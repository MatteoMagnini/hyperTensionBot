using HyperTensionBot.Server.Database;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Plottable.AxisManagers;
using ScottPlot.Renderable;
using ScottPlot.Styles;
using ScottPlot;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static ScottPlot.Plottable.PopulationPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HyperTensionBot.Server.Bot.Extensions {
    // Static class to send messages for variety of scenes in the chat
    public static class SendMessagesExtension {

        // Start Message
        public static async Task SendStartMessage(TelegramBotClient bot, long id) {
            await bot.SendTextMessageAsync(id,
                @"
👋 Benvenuto al nostro Bot per l'Ipertensione e la Frequenza Cardiaca!
Sono qui per aiutarti a gestire la tua salute cardiovascolare. 🩺

📌 Misurazioni di Pressione Arteriosa:
- Prima di effettuare le misurazioni, attendi qualche minuto in posizione seduta.
- Evita di farlo subito dopo i pasti, il fumo di sigarette, il consumo di alcolici o lo sforzo fisico.
- Posiziona il bracciale a uno o due centimetri sopra la piega del gomito.
- Durante la misurazione, mantieni il braccio rilassato e appoggiato in modo che il bracciale si trovi all'altezza del cuore.

🔍 Come Funziono:
- Sono in grado di rispondere alle tue domande generali e specifiche sull'ipertensione.
- Posso acquisire e memorizzare le tue misurazioni di pressione arteriosa e frequenza cardiaca.
- Fornisco visualizzazioni dei dati raccolti, sia in formato lista che grafico.
- Calcolo anche le medie dei dati per te.

🤖 Attenzione:
- Comprendo ciò che mi dici, ma potrei commettere errori. Verifica sempre le analisi che ti fornisco.
- Per ottenere risultati precisi, specifica il tipo di dato (pressione, frequenza o dati personali), la finestra temporale in giorni e il formato desiderato (lista, grafico o media) durante le richieste, o per gli inserimenti specifica se si tratta di pressione o frequenza.

Se hai domande o hai bisogno di assistenza, non esitare a chiedere! 🌟");
        }


        // manage button 
        public static async Task SendButton(TelegramBotClient bot, string text, long id, string[] s) {
            // Check that array is not null 
            if (s != null && s.Length % 2 == 0) {

                // List of bot button
                List<InlineKeyboardButton> buttons = new();

                // add button 
                for (int i = 0; i < s.Length; i += 2) {
                    // A Button is added if theres a pair (key, value) to send at user 
                    if (i + 1 < s.Length) {
                        buttons.Add(new InlineKeyboardButton(s[i]) { CallbackData = s[i + 1] });
                    }
                }

                var inlineKeyboard = new InlineKeyboardMarkup(buttons.ToArray());

                // Send messages with buttons 
                await bot.SendTextMessageAsync(id, text, replyMarkup: inlineKeyboard);
            }
            else
                throw new ArgumentException();
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

        public static async Task HandleRefuseRegisterMeasurement(Chat chat, TelegramBotClient bot) {

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
            StringBuilder sb = new("--- Visionare i dati ");

            if (parameters[0] == "ENTRAMBI")
                sb.Append("di pressione arteriosa e frequenza cardiaca🤓");
            else if (parameters[0] == "PRESSIONE")
                sb.Append("di pressione arteriosa🤓");
            else if (parameters[0] == "FREQUENZA")
                sb.Append("di frequenza cardiaca🤓");
            else if (parameters[0] == "PERSONALE")
                sb.Append("personali indicati al dottore🤓");

            if (parameters[1] == "-1")
                sb.Append("\n--- dal tuo primo inserimento📅");
            else {
                sb.Append("\n--- con riferimento ");
                if (parameters[1] == "1" || parameters[1] == "0")
                    sb.Append("l'ultimo giorno📅");
                else
                    sb.Append($"gli ultimi {parameters[1]} giorni📅");
            }


            if (parameters[2] == "LISTA")
                sb.Append($"\n--- tramite un elenco📉");
            else if (parameters[2] == "GRAFICO")
                sb.Append($"\n--- tramite una rappresentazione grafica📉");
            else
                sb.Append("\n--- tramite la loro media📉");

            return sb.ToString();
        }
        public static async Task SendChoiceRequest(TelegramBotClient bot, Memory memory, long id, string[] choice, ConversationInformation.RequestState state, string text) {
            await SendButton(bot, $"Scegli {text} desiderato..", id, choice);
            memory.SetRequestState(id, state);
        }
    }
}
