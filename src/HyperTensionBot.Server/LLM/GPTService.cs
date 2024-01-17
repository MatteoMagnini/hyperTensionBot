using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace HyperTensionBot.Server.LLM {
    public class GPTService {
        private OpenAIAPI? api;
        private string? gptKey;
        private List<ChatMessage> calculateDays = new();


        public GPTService(WebApplicationBuilder builder) {
            ConfigureKey(builder);
            AnalysisTime();
        }

        private void ConfigureKey(WebApplicationBuilder builder) {
            var confGpt = builder.Configuration.GetSection("OpenAI");
            if (!confGpt.Exists() && confGpt["OpenKey"] != null)
                throw new ArgumentException("Configuration Gpt: OpenAi Key is not set");
            this.gptKey = confGpt["Openkey"];
            this.api = new OpenAIAPI(gptKey);
        }

        private void AnalysisTime() {
            this.calculateDays = new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "devi analizzare e produrre esclusivamente con 3 etichette (Mostrate fra '..' ma dovrai riportare solo la parola senza nient'altro). La prima etichetta descrive il contesto richiesta" +
                    "'PRESSIONE', 'FREQUENZA', 'ENTRAMBI' (quando la richiesta indica sia pressione che frequenza o è generico), 'GENERALE' (per richieste sui dati personali diverse da misurazioni e medie). " +
                    "Il secondo parametro è l'arco temporale espresso in giorni sempre positivi: eccezione fanno i dati recenti con risposta 1, e la totalità dei dati o richieste non specifiche con -1. " +
                    "Il terzo parametro indica il formato che potrà essere 'MEDIA' 'GRAFICO' (esplicitamente o implicitamente con richieste di rappresentazioni e andamenti), 'LISTA' (Per l'etichetta 1 GENERALE è sempre lista)" +
                    "Il tuo output da questo momento in poi deve essere con le sole 3 etichette senza virgole punti o altro."),
                new ChatMessage(ChatMessageRole.Assistant, "Certo."),
                new ChatMessage(ChatMessageRole.User, "Dammi i dati"),
                new ChatMessage(ChatMessageRole.Assistant, "ENTRAMBI -1 LISTA"),
                new ChatMessage(ChatMessageRole.User, "Mostra l'andamento dei dati sulla frequenza più recenti "),
                new ChatMessage(ChatMessageRole.Assistant, "FREQUENZA 1 GRAFICO"),
                new ChatMessage(ChatMessageRole.User, "Riportami il valore medio sulla pressione dell'ultimo mese"),
                new ChatMessage(ChatMessageRole.Assistant, "PRESSIONE 30 MEDIA"),
                new ChatMessage(ChatMessageRole.User, "voglio ricontrollare i dati. Dammi una rappresentazione"),
                new ChatMessage(ChatMessageRole.Assistant, "ENTRAMBI -1 GRAFICO"),
                new ChatMessage(ChatMessageRole.User, "Dammi le mie informazioni personali"),
                new ChatMessage(ChatMessageRole.Assistant, "GENERALE -1 LISTA"),
            };
        }

        public async Task<string> CallGpt(TypeConversation t, string userMessage = "", List<ChatMessage>? conversation = null) {
            if (api is not null) {
                if (t == TypeConversation.Communication)
                    conversation!.Add(new ChatMessage(ChatMessageRole.User, userMessage));
                else
                    calculateDays.Add(new ChatMessage(ChatMessageRole.User, userMessage));

                var response = await api.Chat.CreateChatCompletionAsync(
                    model: Model.ChatGPTTurbo,
                    messages: (t == TypeConversation.Communication) ? conversation : calculateDays,
                    max_tokens: 200);
                return response.ToString();
            }
            return "Error Service";
        }
    }
}
