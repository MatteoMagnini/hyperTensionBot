using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using HyperTensionBot.Server.Database;
using System;
using Telegram.Bot.Requests;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace HyperTensionBot.Server.Bot.Extensions {
    public static class SendMessagesExtension {

        // manage button 
        public static async Task SendButton(TelegramBotClient bot, string text, Chat chat, string[] s) {
            await bot.SendTextMessageAsync(chat.Id, text,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                new InlineKeyboardButton(s[0]) { CallbackData = s[1] },
                new InlineKeyboardButton(s[2]) { CallbackData = s[3] },
                })
            );
        }

        // messages for confermed and refused insert measurement 
        public static async Task HandleConfirmRegisterMeasurement(User from, Chat chat, TelegramBotClient bot, Memory memory) {
            memory.PersistMeasurement(from, chat.Id);

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
    }
}
