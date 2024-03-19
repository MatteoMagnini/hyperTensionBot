using HyperTensionBot.Server.LLM;
using OpenAI_API.Chat;

namespace HyperTensionBot.Server.Bot {
    public class UserInformation {
        public UserInformation(long telegramId) {
            TelegramId = telegramId;
            LastConversationUpdate = DateTime.UtcNow;
            Measurements = new();
            GeneralInfo = new();
            ChatMessages = new();
        }

        public long TelegramId { get; init; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

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

        public List<string> ChatMessages { get; set; }

        public override bool Equals(object? obj) {
            if (obj is UserInformation userInformation) {
                return TelegramId == userInformation.TelegramId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramId.GetHashCode();
        }
    }
}
