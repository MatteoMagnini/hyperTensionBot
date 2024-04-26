using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace HyperTensionBot.Server.LLM {
    // questa classe è stata sostituita da LLMService ed è in fase di cancellazione 
    public class GPTService {
        private OpenAIAPI? api;
        private string? gptKey;
        private List<LLMChat> calculateDays = new();


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
            this.calculateDays = new List<LLMChat> {
                new LLMChat(ChatMessageRole.User, "devi analizzare e produrre esclusivamente con 3 etichette (Mostrate fra '..' ma dovrai riportare solo la parola senza nient'altro). La prima etichetta descrive il contesto richiesta" +
                    "'PRESSIONE', 'FREQUENZA', 'ENTRAMBI' (quando la richiesta indica sia pressione che frequenza o è generico), 'GENERALE' (per richieste sui dati personali diverse da misurazioni e medie). " +
                    "Il secondo parametro è l'arco temporale espresso in giorni sempre positivi: eccezione fanno i dati recenti con risposta 1, e la totalità dei dati o richieste non specifiche con -1. " +
                    "Il terzo parametro indica il formato che potrà essere 'MEDIA' 'GRAFICO' (esplicitamente o implicitamente con richieste di rappresentazioni e andamenti), 'LISTA' (Per l'etichetta 1 GENERALE è sempre lista)" +
                    "Il tuo output da questo momento in poi deve essere con le sole 3 etichette senza virgole punti o altro."),
                new LLMChat(ChatMessageRole.Assistant, "Certo."),
                new LLMChat(ChatMessageRole.User, "Dammi i dati"),
                new LLMChat(ChatMessageRole.Assistant, "ENTRAMBI -1 LISTA"),
                new LLMChat(ChatMessageRole.User, "Mostra l'andamento dei dati sulla frequenza più recenti "),
                new LLMChat(ChatMessageRole.Assistant, "FREQUENZA 1 GRAFICO"),
                new LLMChat(ChatMessageRole.User, "Riportami il valore medio sulla pressione dell'ultimo mese"),
                new LLMChat(ChatMessageRole.Assistant, "PRESSIONE 30 MEDIA"),
                new LLMChat(ChatMessageRole.User, "voglio ricontrollare i dati. Dammi una rappresentazione"),
                new LLMChat(ChatMessageRole.Assistant, "ENTRAMBI -1 GRAFICO"),
                new LLMChat(ChatMessageRole.User, "Dammi le mie informazioni personali"),
                new LLMChat(ChatMessageRole.Assistant, "GENERALE -1 LISTA"),
            };
        }

        public async Task<string> CallGpt(TypeConversation t, string userMessage = "", List<LLMChat>? conversation = null) {
            if (api is not null) {
                if (t == TypeConversation.Communication)
                    conversation!.Add(new LLMChat(ChatMessageRole.User, userMessage));
                else
                    calculateDays.Add(new LLMChat(ChatMessageRole.User, userMessage));

                var response = await api.Chat.CreateChatCompletionAsync(
                    model: Model.ChatGPTTurbo,
                    messages: (IList<OpenAI_API.Chat.ChatMessage>?)((t == TypeConversation.Communication) ? conversation : calculateDays),
                    max_tokens: 200);
                return response.ToString();
            }
            return "Error Service";
        }
    }
}
