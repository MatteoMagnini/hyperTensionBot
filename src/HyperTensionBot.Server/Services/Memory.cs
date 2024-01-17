using HyperTensionBot.Server.Bot;
using OpenAI_API.Chat;
using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Services {
    public class Memory {

        public ConcurrentDictionary<long, UserInformation> UserMemory { get; } = new();
        private readonly ConcurrentDictionary<long, ConversationInformation> _chatMemory = new();

        private readonly ILogger<Memory> _logger;

        public Memory(
            ILogger<Memory> logger
        ) {
            _logger = logger;
        }

        public void HandleUpdate(User? from, Chat chat) {
            if (from != null) {
                if (!UserMemory.TryGetValue(from.Id, out var userInformation)) {
                    userInformation = new UserInformation(from.Id);
                }
                userInformation.FirstName = from.FirstName;
                userInformation.LastName = from.LastName;
                userInformation.LastConversationUpdate = DateTime.UtcNow;
                UserMemory.AddOrUpdate(from.Id, userInformation, (_, _) => userInformation);
                _logger.LogTrace("Updated user memory");
            }

            if (!_chatMemory.TryGetValue(chat.Id, out var chatInformation)) {
                chatInformation = new ConversationInformation(chat.Id);
            }
            chatInformation.LastConversationUpdate = DateTime.UtcNow;
            _chatMemory.AddOrUpdate(chat.Id, chatInformation, (_, _) => chatInformation);
            _logger.LogTrace("Updated chat memory");
        }

        public void SetTemporaryMeasurement(Chat chat, Measurement measurement) {
            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id) { TemporaryMeasurement = measurement }, (_, existing) => {
                existing.TemporaryMeasurement = measurement;
                return existing;
            });
            _logger.LogTrace("Stored temporary measurement for chat {0}", chat.Id);
        }

        public void PersistMeasurement(User from, Chat chat) {
            if(!_chatMemory.TryGetValue(chat.Id, out var chatInformation)) {
                throw new Exception($"Tried persisting measurement but no information available about chat {chat.Id}");
            }
            if(chatInformation.TemporaryMeasurement == null) {
                throw new Exception($"Tried persisting measurement but no temporary measurement was recorded for chat {chat.Id}");
            }

            var newValue = new UserInformation(from.Id);
            newValue.Measurements.Add(chatInformation.TemporaryMeasurement);
            UserMemory.AddOrUpdate(from.Id, newValue, (_, existing) => {
                existing.Measurements.Add(chatInformation.TemporaryMeasurement);
                return existing;
            });

            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id), (_, existing) => {
                existing.TemporaryMeasurement = null;
                return existing;
            });
        }

        public List<string> GetGeneralInfo(Chat chat) {
            UserMemory.TryGetValue(chat.Id, out var chatInformation);
            return chatInformation!.GeneralInfo;
        }

        public List<ChatMessage> AddMessageLLM(Chat chat, string message) {
            UserMemory.TryGetValue(chat.Id, out var chatInformation);
            chatInformation!.ChatMessages.Add(new ChatMessage(ChatMessageRole.User, message));
            return chatInformation!.ChatMessages;
        }
    }
}
