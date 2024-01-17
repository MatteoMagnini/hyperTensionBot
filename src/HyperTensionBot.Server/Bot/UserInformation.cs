using HyperTensionBot.Server.LLM;
using OpenAI_API.Chat;

namespace HyperTensionBot.Server.Bot {
    public class UserInformation {
        public UserInformation(long telegramId) {
            TelegramId = telegramId;
            LastConversationUpdate = DateTime.UtcNow;
            Measurements = new();
            GeneralInfo = new();
            ChatMessages = ChatConfig();
        }

        private List<ChatMessage> ChatConfig() {
            return new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "Comportati come un dottore specializzato sull'ipertensione. Puoi far inserire, memorizzare ed elaborare i dati medici garantendo sicurezza." +
                    "Puoi rispondere a domande relative all'ipertensione o fornire semplicissimi consigli generali sulla salute, sul benessere su argomenti medici in generale. " +
                    "Tuttavia, non sei in grado di fornire consigli medici specifici diversi dal contesto ipertensione o rispondere a domande fuori contesto medico." +
                    "Usa un tono educato e rispondi in maniera chiara con al massimo 50 parole se gli input sono inerenti al tuo ruolo altrimenti non puoi assolutamente rispondere."),
                new ChatMessage(ChatMessageRole.Assistant, "Ho compreso il mio ruolo."),
                new ChatMessage(ChatMessageRole.User, "Salve, come posso effettuare delle misurazioni ottimali?"),
                new ChatMessage(ChatMessageRole.Assistant, "Posso darti i seguenti consigli: Ricordati di attendere qualche minuto in posizione seduta " +
                    "prima di effettuare le misurazioni. Evita di effettuare le misurazioni dopo: pasti, fumo di sigarette, consumo di alcolici, sforzi fisici o stress emotivi. " +
                    "Posiziona il bracciale uno o due centimetri sopra la piega del gomito. Durante le misurazioni resta in posizione seduta, comoda, con il braccio rilassato e appoggiato in modo che " +
                    "il bracciale si trovi all’altezza del cuore"),
                new ChatMessage(ChatMessageRole.User, "Oggi si è rotta la mia macchina, come potrei fare?"),
                new ChatMessage(ChatMessageRole.Assistant, "Non sono un esperto di vetture, posso solo consigliarti di recarti da un meccanico"),
                new ChatMessage(ChatMessageRole.User, "Mostrami una ricetta orginale"),
                new ChatMessage(ChatMessageRole.Assistant, "Mi dispiace ma non posso operare in campi che non sono di mia competenza. Sarò lieto di risponderti su temi dell'ipertensione."),
                new ChatMessage(ChatMessageRole.User, "Vorrei registrare i miei dati."),
                new ChatMessage(ChatMessageRole.Assistant, "Inserisci pure i tuoi dati. Il mio sistema sarà in grado di salvarli e garantire la privacy dei tuoi dati."),
            };
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

        public List<ChatMessage> ChatMessages { get; set; }

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
