using HyperTensionBot.Server.LLM.Strategy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Chat;
using System.Text;

namespace HyperTensionBot.Server.LLM {

    // allow ollama service. It used for comunicate with model in the server 
    public class OllamaService : ILLMService {

        private readonly HttpClient _httpClient = new();
        // URL for LLM
        private readonly string? _llmApiUrl;

        // set names to different model 
        private readonly string MODEL_COMUNICATION = "llama3";
        private readonly string MODEL_REQUEST = "llama3";
        private readonly string MODEL_INSERT = "llama3";

        // Lists contains requests of insertion and data request. They are used for the context at prompt 
        private readonly List<ChatMessage> analysistInsert;
        private readonly List<ChatMessage> analysisRequest;

        private ILogger<LLMService>? _logger;

        // Factory costructor 
        private OllamaService(WebApplicationBuilder builder) {
            _llmApiUrl = ConfigureUrl(builder);

            analysisRequest = Prompt.RequestContext();
            analysistInsert = Prompt.InsertContest();
        }

        private static string ConfigureUrl(WebApplicationBuilder builder) {
            var buildCluster = builder.Configuration.GetSection("Clusters");
            if (!buildCluster.Exists() && buildCluster["UrlLLM"] != null)
                throw new ArgumentException("Configuration Cluster: Url Cluster is not set");
            return buildCluster["UrlLLM"]!;
        }

        public static async Task<OllamaService> CreateAsync(WebApplicationBuilder builder) {

            var llm = new OllamaService(builder);

            await llm.CheckConnection(llm);

            return llm;
        }

        private async Task CheckConnection(OllamaService llm) {

            llm._httpClient.Timeout = TimeSpan.FromSeconds(200);    // over 200 seconds for the request, it can be an error

            // try get request
            var response = await llm._httpClient.GetAsync(llm._llmApiUrl!.Replace("/api/generate", ""));

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Errore di connessione al server");
        }

        public void SetLogger(ILogger<LLMService> logger) { _logger = logger; }

        // connection and interaction with server for request to LLM 
        public async Task<string> AskLLM(TypeConversation t, string message, List<ChatMessage>? comunicationChat = null) {

            if (_llmApiUrl != "") {

                string modelName = "";
                double temp = 0;
                List<ChatMessage> chatContext = new();
                AssignInput(t, ref chatContext, comunicationChat, ref modelName, ref temp);

                // build payload JSON
                var jsonPayload = new {
                    model = modelName,
                    prompt = chatContext!.First().Content + "\n The request is: " + message,
                    messages = chatContext,
                    stream = false,
                    options = new {
                        temperature = temp // value for deterministic or crestive response 
                    },
                };

                var content = new StringContent(JsonConvert.SerializeObject(jsonPayload), Encoding.UTF8, "application/json");

                // send POST request 
                try {
                    var response = await _httpClient.PostAsync(_llmApiUrl, content);

                    if (response.IsSuccessStatusCode) {

                        var jsonResponse = await response.Content.ReadAsStringAsync();

                        // extracting text by LLM response 
                        return ParserResponse(jsonResponse, t);
                    }

                    return "Si è verificato un errore nella generazione del testo.";
                }
                catch (TaskCanceledException) {
                    return "Errore dal server";
                }
            }
            return "Non è possibile rispondere a queste domande. Riprova più tardi. ";
        }

        // Set parameter for each conversation model
        private void AssignInput(TypeConversation t, ref List<ChatMessage> chatContext, List<ChatMessage>? comunication, ref string modelName, ref double temp) {
            switch (t) {
                case TypeConversation.Request:
                    modelName = MODEL_REQUEST;
                    chatContext = analysisRequest;
                    temp = 0.1;
                    break;
                case TypeConversation.Insert:
                    modelName = MODEL_INSERT;
                    chatContext = analysistInsert;
                    temp = 0.2;
                    break;
                default:
                    modelName = MODEL_COMUNICATION;
                    chatContext = comunication!;
                    temp = 0.8;
                    break;
            }
        }

        private string ParserResponse(string response, TypeConversation t) {
            JObject jsonObj = JObject.Parse(response);

            // Estrai il valore della chiave 'response'
            var resp = jsonObj["response"]?.ToString();

            // Rimuovi i caratteri di newline e ritorna la risposta
            resp = resp!.Replace("\\n", "");
            if (t != TypeConversation.Communication && _logger is not null) _logger.LogInformation(resp);
            return resp;
        }
    }
}
