namespace HyperTensionBot.Server.Bot {
    public class ConversationInformation {
        public ConversationInformation(long telegramChatId) {
            TelegramChatId = telegramChatId;
            LastConversationUpdate = DateTime.UtcNow;
        }

        public long TelegramChatId { get; init; }

        public DateTime LastConversationUpdate { get; set; }

        public Measurement? TemporaryMeasurement { get; set; }

        public override bool Equals(object? obj) {
            if (obj is ConversationInformation conversationInformation) {
                return TelegramChatId == conversationInformation.TelegramChatId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramChatId.GetHashCode();
        }
    }
}
