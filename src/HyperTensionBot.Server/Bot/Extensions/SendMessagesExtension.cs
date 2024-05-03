using HyperTensionBot.Server.Database;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HyperTensionBot.Server.Bot.Extensions {
    public static class SendMessagesExtension {

        // manage button 
        public static async Task SendButton(TelegramBotClient bot, string text, long id, string[] s) {
            await bot.SendTextMessageAsync(id, text,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                new InlineKeyboardButton(s[0]) { CallbackData = s[1] },
                new InlineKeyboardButton(s[2]) { CallbackData = s[3] },
                })
            );
        }

        // messages for confermed and refused insert measurement 
        public static async Task HandleConfirmRegisterMeasurement(User from, Chat chat, TelegramBotClient bot, Memory memory) {
            memory.PersistMeasurement(chat.Id);

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

        public static async Task HandleRefuseRegisterMeasurement(Chat chat, TelegramBotClient bot, Memory memory) {

            await bot.SendTextMessageAsync(chat.Id,
                new string[] {
                    "No? Mandami pure i dati corretti allora\\.\nInvia le misure rilevate in un *unico messaggio di testo\\.",
                    "Devo aver capito male, puoi ripetere i dati della misurazione?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                    "Forse ho capito male, puoi ripetere?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                        }.PickRandom(),
                        parseMode: ParseMode.MarkdownV2
            );
        }

        // messages to inform for the wait
        public static async Task<int> Waiting(long id, TelegramBotClient bot) {
            var message = await bot.SendTextMessageAsync(id, "🔄⏳ Sto elaborando... 🔄⏳");
            return message.MessageId;
        }

        public static async Task Delete(TelegramBotClient bot, long idChat, int idMessage) {
            await bot.DeleteMessageAsync(idChat, idMessage);
        }

        internal static string DefineRequestText(string[] parameters) {
            StringBuilder sb = new("- Visionare i dati ");

            if (parameters[0] == "ENTRAMBI")
                sb.Append("di pressione arteriosa e frequenza cardiaca");
            else if(parameters[0] == "PRESSIONE")
                sb.Append("di pressione arteriosa");
            else if (parameters[0] == "FREQUENZA")
                sb.Append("di frequenza cardiaca");
            else if (parameters[0] == "PERSONALE")
                sb.Append("personali indicati al dottore");

            if (parameters[1] == "-1")
                sb.Append("\n- dal tuo primo inserimento");
            else {
                sb.Append("\n- con riferimento ");
                if (parameters[1] == "1" || parameters[0] == "0")
                    sb.Append("l'ultimo giorno");
                else
                    sb.Append($"gli ultimi {parameters[0]} giorni");
            }
                

            if (parameters[2] == "LISTA")
                sb.Append($"\n- tramite un elenco");
            else if (parameters[2] == "GRAFICO")
                sb.Append($"\n- tramite una rappresentazione grafica");
            else
                sb.Append("\n- tramite la loro media");

            return sb.ToString(); 
        }
    }
}
