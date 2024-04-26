using HyperTensionBot.Server.LLM;
using Telegram.Bot.Types;
using LLMChat = HyperTensionBot.Server.LLM.LLMChat;

namespace HyperTensionBot.Server.Bot {
    public class UserInformation {
        public UserInformation(User from, DateTime date) {
            TelegramId = from.Id;
            LastConversationUpdate = date;
            FirstName = from.FirstName;
            LastName = from.LastName;
            Measurements = new();
            GeneralInfo = new();
            ChatComunication = Prompt.GeneralContext();
        }

        public long TelegramId { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? FullName {
            get {
                return string.Join(" ", new string?[] { FirstName, LastName }.Where(s => s != null));
            }
        }

        public List<Measurement> Measurements { get; init; }

        public Measurement? LastMeasurement {
            get {
                if (Measurements.Count == 0) {
                    return null;
                }

                return Measurements[Measurements.Count - 1];
            }
        }

        public Measurement? FirstMeasurement {
            get {
                if (Measurements.Count == 0) {
                    return null;
                }

                return Measurements[0];
            }
        }

        public DateTime LastConversationUpdate { get; set; }

        public List<string> GeneralInfo { get; set; }

        public override bool Equals(object? obj) {
            if (obj is UserInformation userInformation) {
                return TelegramId == userInformation.TelegramId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramId.GetHashCode();
        }

        public List<LLMChat> ChatComunication { get;}
    }
}
