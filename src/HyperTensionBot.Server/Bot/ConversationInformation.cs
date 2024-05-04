namespace HyperTensionBot.Server.Bot {
    public class ConversationInformation {
        public ConversationInformation(long telegramChatId, DateTime? date = null, RequestState state = RequestState.ChoiceContext) {
            TelegramChatId = telegramChatId;
            LastConversationUpdate = date;
            State = state;
        }

        public long TelegramChatId { get; init; }

        public DateTime? LastConversationUpdate { get; set; }

        public Measurement? TemporaryMeasurement { get; set; }

        public enum RequestState {
            ChoiceContext, ChoiceTimeSpan, ChoiceFormat
        }

        public RequestState State { get; set; }

        public string[]? Requestparameters { get; set; }

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
