using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HyperTensionBot.Server.Bot {
    public static class Context { 
        public static async Task ControlFlow(TelegramBotClient bot, GPTService gpt, Memory memory, Intent context, string message, Chat chat, DateTime date) {
            try {
                switch (context) {
                    case Intent.Richiesta:
                        await Request.ManageRequest(message, memory, chat, bot, gpt);
                        break;

                    // ask conferme and storage data 
                    case Intent.inserDatiGener:
                        await StorageGeneralData(bot, message, chat, memory);
                        break;
                    case Intent.inserDatiPress:
                        await StorageDataPress(bot, message, chat, memory, date);
                        break;
                    case Intent.inserDatiFreq:
                        await StorageDataFreq(bot, message, chat, memory, date);
                        break;
                    case Intent.inserDatiTot:
                        await StorageDataTot(bot, message, chat, memory, date);
                        break;

                    case Intent.Umore:
                        await bot.SendTextMessageAsync(
                            chat.Id, await gpt.CallGpt(TypeConversation.Communication,
                                conversation: memory.AddMessageLLM(chat, "Rispondi a questo messaggio con poche parole: " + message)));
                        memory.UserMemory.TryGetValue(chat.Id, out var info);
                        if (info?.FirstMeasurement != null)
                            await CheckAverage(Request.AverageData(memory, chat, 30, true, false), bot, chat);
                        break;

                    // gpt 
                    case Intent.Generale:
                        await bot.SendTextMessageAsync(
                            chat.Id, await gpt.CallGpt(TypeConversation.Communication, conversation: memory.AddMessageLLM(chat, message)));
                        break;
                }
            }
            catch (ArgumentNullException) {
                await bot.SendTextMessageAsync(chat.Id, "Non sono presenti dati per soddsfare la tua richiesta❌\n" +
                    "Inizia a inserire i tuoi dati, dopodichè sarà possibile accedere alle statistiche inerenti!.");
            }
            catch (ArgumentException) {
                await bot.SendTextMessageAsync(chat.Id, "Non ho compreso i dati. Prova a riscrivere il messaggio in un altro modo😊\nProva inserendo prima i valori di pressione e poi la frequenza🤞.");
            }
            
            catch (ExceptionExtensions.ImpossibleSystolic) {
                await bot.SendTextMessageAsync(chat.Id, "La pressione sistolica potrebbe essere errata. Ripeti la misurazione e se i dati si ripetono contatta subito il dottore.");
            }
            catch (ExceptionExtensions.ImpossibleDiastolic) {
                await bot.SendTextMessageAsync(chat.Id, "La pressione diastolica potrebbe essere errata. Ripeti la misurazione e se i dati si ripetono contatta subito il dottore.");
            }
            catch (ExceptionExtensions.ImpossibleHeartRate) {
                await bot.SendTextMessageAsync(chat.Id, "La frequenza cardiaca potrebbe essere errata. Ripeti la misurazione e se i dati si ripetono contatta subito il dottore.");
            }
        }

        private static async Task StorageGeneralData(TelegramBotClient bot, string message, Chat chat, Memory memory) {
            memory.GetGeneralInfo(chat).Add(message);
            await bot.SendTextMessageAsync(chat.Id, "Queste informazioni sono preziose per il dottore: più dati fornisci migliore sarà l'analisi!💪");
        }

        // manage meuserment
        private static async Task StorageDataTot(TelegramBotClient bot, string message, Chat chat, Memory memory, DateTime date) {
            // Match values
            var measurement = RegexExtensions.ExtractMeasurement(message);
            // send message and button
            memory.SetTemporaryMeasurement(chat, new Measurement(
                systolicPressure: measurement[0],
                diastolicPressure: measurement[1],
                heartRate: measurement[2],
                date: date
            ));

            string text = $"Grazie per avermi inviato pressione e frequenza.\n\n🔺 Pressione sistolica: {measurement[0].ToString("F0")} mmHg\n🔻 Pressione diastolica: {measurement[1].ToString("F0")} mmHg\n" +
                $"❤️ Frequenza: {measurement[2].ToString("F0")} bpm\n\nHo capito bene?";

            await SendButton(bot, text, chat, new string[] { "Sì, registra!", "yes", "No", "no" });

        }

        private static async Task StorageDataPress(TelegramBotClient bot, string message, Chat chat, Memory memory, DateTime date) {
            // Match values
            var pressure = RegexExtensions.ExtractPressure(message);
            // send message and button
            memory.SetTemporaryMeasurement(chat, new Measurement(
                systolicPressure: pressure[0],
                diastolicPressure: pressure[1],
                heartRate: null,
                date: date
            ));

            string text = $"Grazie per avermi inviato la tua pressione.\n\n🔺 Pressione sistolica: {pressure[0].ToString("F0")} mmHg\n🔻 Pressione diastolica: {pressure[1].ToString("F0")} mmHg\n" +
                $"Ho capito bene?";

            await SendButton(bot, text, chat, new string[] { "Sì, registra!", "yes", "No", "no" });
        }

        private static async Task StorageDataFreq(TelegramBotClient bot, string message, Chat chat, Memory memory, DateTime date) {
            // Match values
            var freq = RegexExtensions.ExtractFreq(message);

            // send message and button
            memory.SetTemporaryMeasurement(chat, new Measurement(
                systolicPressure: null,
                diastolicPressure: null,
                heartRate: freq,
                date: date
            ));

            string text = $"Grazie per avermi inviato la tua frequenza.\n\n❤️ Frequenza: {freq.ToString()} bpm\nHo capito bene?";

            await SendButton(bot, text, chat, new string[] { "Sì, registra!", "yes", "No", "no" });

        }

        public static async Task SendButton(TelegramBotClient bot, string text, Chat chat, string[] s) {
            await bot.SendTextMessageAsync(chat.Id, text,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                new InlineKeyboardButton(s[0]) { CallbackData = s[1] },
                new InlineKeyboardButton(s[2]) { CallbackData = s[3] },
                })
            );
        }

        // manage button
        public static async Task ValuteMeasurement(string resp, User from, Chat chat, TelegramBotClient bot, Memory memory) {
            if (resp == "yes") {
                await HandleConfirmRegisterMeasurement(from, chat, bot, memory);
                memory.UserMemory.TryGetValue(chat.Id, out var info);
                if (info?.LastMeasurement?.DiastolicPressure != null) {
                    List<int?> average = Request.AverageData(memory, chat, 30, true, false);
                    await CheckAverage(average, bot, chat);
                }
            }
            else if (resp == "no") {
                await HandleRefuseRegisterMeasurement(chat, bot, memory);
            }
        }

        public static async Task CheckAverage(List<int?> average, TelegramBotClient bot, Chat chat) {
            // check sulla media nell'ultimo mese dopo un inserimento delle nuove misure
            StringBuilder sb = new();
            sb.Append("Ho analizzato le nuove medie registrate:\n");
            if (average[0] < 135 && average[1] < 85) 
                await bot.SendTextMessageAsync(chat.Id, $"La media sulla pressione è {average[0]}/{average[1]} che rientra sotto i parametri ottimali di salute😁");
            else
                await bot.SendTextMessageAsync(chat.Id, $"La media sulla pressione è {average[0]}/{average[1]}, e le consiglio di consultare il medico per analizzare la situazione in maneira più approfondita🧑🏽‍⚕️");
        }

        private static async Task HandleConfirmRegisterMeasurement(User from, Chat chat, TelegramBotClient bot, Memory memory) {
            memory.PersistMeasurement(from, chat);

            await bot.SendTextMessageAsync(chat.Id,
                new string[] {
                    "Perfetto, tutto chiaro\\! Inserisco subito i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "Il dottore sarà impaziente di vedere i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "I dati sono stati inseriti, spero solo che il dottore capisca la mia calligrafia\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "Perfetto, grazie\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚"
                        }.PickRandom(),
                        parseMode: ParseMode.MarkdownV2
            );
        }

        private static async Task HandleRefuseRegisterMeasurement(Chat chat, TelegramBotClient bot, Memory memory) {

            await bot.SendTextMessageAsync(chat.Id,
                new string[] {
                    "No? Mandami pure i dati corretti allora\\.\nInvia le misure rilevate in un *unico messaggio di testo.\\.",
                    "Devo aver capito male, puoi ripetere i dati della misurazione?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                    "Forse ho capito male, puoi ripetere?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                        }.PickRandom(),
                        parseMode: ParseMode.MarkdownV2
            );
        }
    }
}
