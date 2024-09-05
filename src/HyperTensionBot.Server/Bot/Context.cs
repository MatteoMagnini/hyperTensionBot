using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using HyperTensionBot.Server.ModelML;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Bot {
    // Manage all predicted intent of ML model. After prediction this class is responsable to workflow
    public static class Context {

        public static async Task ControlFlow(TelegramBotClient bot, LLMService llm, Memory memory, Intent context, string message, Chat chat, DateTime date) {
            try {

                int idMessage;

                switch (context) {

                    case Intent.Richiesta:
                        idMessage = await SendMessagesExtension.Waiting(chat.Id, bot);
                        var contRe = ManageChat.GetContext(chat.Id, memory.Chat, message);
                        await Request.AskConfirmParameters(llm, bot, memory, chat.Id, contRe);
                        await SendMessagesExtension.Delete(bot, chat.Id, idMessage);
                        break;

                    // ask conferme and storage data 
                    case Intent.PersonalInfo:
                        idMessage = await SendMessagesExtension.Waiting(chat.Id, bot);
                        await StorageGeneralData(bot, message, chat, memory);
                        await SendMessagesExtension.Delete(bot, chat.Id, idMessage);
                        break;

                    case Intent.Inserimento:
                        var cont = ManageChat.GetContext(chat.Id, memory.Chat, message);
                        var result = await llm.HandleAskAsync(TypeConversation.Insert, context: cont);
                        await StorageData(bot, result, chat, memory, date);
                        break;

                    case Intent.Umore:
                        idMessage = await SendMessagesExtension.Waiting(chat.Id, bot);
                        await bot.SendTextMessageAsync(
                            chat.Id, await llm.HandleAskAsync(TypeConversation.Communication,
                                comunicationChat: memory.AddMessageLLM(chat, message)));
                        if (memory.GetFirstMeasurement(chat.Id).HasValue)
                            await CheckAverage(Request.AverageData(memory, chat.Id, 30, true, false), bot, chat.Id);

                        await SendMessagesExtension.Delete(bot, chat.Id, idMessage);
                        break;

                    case Intent.Generale:
                        idMessage = await SendMessagesExtension.Waiting(chat.Id, bot);
                        await bot.SendTextMessageAsync(
                            chat.Id, await llm.HandleAskAsync(TypeConversation.Communication, memory.AddMessageLLM(chat, message)));
                        await SendMessagesExtension.Delete(bot, chat.Id, idMessage);
                        break;
                }
            }
            catch (ArgumentNullException) {
                await bot.SendTextMessageAsync(chat.Id, "Non sono presenti dati per soddsfare la tua richiesta\n" +
                    "Inizia a inserire i tuoi dati, dopodichè sarà possibile accedere alle statistiche presenti!.");
            }
            catch (ArgumentException) {
                await bot.SendTextMessageAsync(chat.Id, "Non ho compreso i dati. Prova a riscrivere il messaggio inserendo prima la pressione e poi la frequenza.");
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

        // Storage datas. The seguent methods manage insert intent
        private static async Task StorageGeneralData(TelegramBotClient bot, string message, Chat chat, Memory memory) {
            memory.GetGeneralInfo(chat.Id).Add(message);
            await bot.SendTextMessageAsync(chat.Id, "Queste informazioni sono preziose per il dottore: più dati fornisci migliore sarà l'analisi!💪");
        }

        // manage meuserment
        private static async Task StorageData(TelegramBotClient bot, string result, Chat chat, Memory memory, DateTime date) {
            // Match values
            var measurement = RegexExtensions.ExtractMeasurement(result);
            // send message and button
            memory.SetTemporaryMeasurement(chat, new Measurement(
                systolicPressure: measurement[0],
                diastolicPressure: measurement[1],
                heartRate: measurement[2],
                date: date
            ), date);

            StringBuilder text = new("Ho ricevuto la misurazione.\n\n");
            if (measurement[0] is not null)
                text.AppendLine($"🔺 Pressione sistolica: {(int)measurement[0]!} mmHg\n🔻 Pressione diastolica: {(int)measurement[1]!} mmHg");
            if (measurement[2] is not null)
                text.AppendLine($"❤️ Frequenza: {(int)measurement[2]!} bpm\n\nHo capito bene?");

            await SendMessagesExtension.SendButton(bot, text.ToString(), chat.Id, new string[] { "Sì, registra!", "yesIns", "No", "noIns" });
        }

        // manage button
        public static async Task ManageButton(string resp, User from, Chat chat, TelegramBotClient bot, Memory memory, LLMService llm) {
            switch (resp) {
                case "yesIns":
                case "noIns":
                    await ValuteMeasurement(resp, from, chat, bot, memory);
                    break;
                case "yesReq":
                case "noReq":
                    await Request.ValuteRequest(resp, chat.Id, bot, memory, llm);
                    break;
            }
        }

        private static async Task ValuteMeasurement(string resp, User from, Chat chat, TelegramBotClient bot, Memory memory) {
            if (resp == "yesIns") {
                await SendMessagesExtension.HandleConfirmRegisterMeasurement(from, chat, bot, memory);
                // if it is inserts a measure of pressure then check
                if (memory.IsPressureLastMeasurement(chat.Id)) {
                    int?[] average = Request.AverageData(memory, chat.Id, 30, true, false);
                    await CheckAverage(average, bot, chat.Id);
                }
            }
            else if (resp == "noIns") {
                await SendMessagesExtension.HandleRefuseRegisterMeasurement(chat, bot);
            }
        }

        public static async Task CheckAverage(int?[] average, TelegramBotClient bot, long id) {
            // check avarage after new insertion on last month's datas 
            StringBuilder sb = new();
            sb.Append("Ho analizzato le nuove medie registrate:\n");
            if (average[0] <= 140 && average[1] <= 90)
                await bot.SendTextMessageAsync(id, $"La media sulla pressione è {average[0]}/{average[1]} che rientra nei parametri ottimali di salute. Molto bene!");
            else
                await bot.SendTextMessageAsync(id, $"La media sulla pressione è {average[0]}/{average[1]}. Le consiglio di consultare il medico per un'analisi più accurata");
        }
    }
}
