using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace HyperTensionBot.Server.LLM {
    public class LLMService {

        private readonly HttpClient _httpClient = new HttpClient();
        // URL dell'API del LLM
        private string? _llmApiUrl;

        // set names to different model 
        private readonly string MODEL_COMUNICATION = "nous-hermes2-mixtral";
        private readonly string MODEL_REQUEST = "llama2:70b";
        private readonly string MODEL_INSERT = "llama2:70b";

        private List<LLMChat> analysistInsert; 
        private List<LLMChat> analysisRequest;

        private ILogger<LLMService>? _logger;

        // Factory costructor 
        private LLMService(WebApplicationBuilder builder) {
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

        public static async Task<LLMService> CreateAsync(WebApplicationBuilder builder) {
            
            var llm = new LLMService(builder); 
            
            await llm.CheckConnection(llm);

            return llm;
        }

        private async Task CheckConnection(LLMService llm) {

            llm._httpClient.Timeout = TimeSpan.FromSeconds(200);    // over 200 seconds for the request, it can be an error

            // try get request
            var response = await llm._httpClient.GetAsync(llm._llmApiUrl!.Replace("/api/generate", ""));

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Errore di connessione al server"); 
        }

        public void SetLogger(ILogger<LLMService> logger) { _logger = logger; }

        // connection and interaction with server for request to LLM 
        public async Task<string> AskLlm(TypeConversation t, string message, List<LLMChat>? comunicationChat = null) {

            if (_llmApiUrl != "") {

                string modelName = "";
                List<LLMChat> chatContext = new();
                AssignInput(t, ref chatContext, comunicationChat, ref modelName); 

                //modelName = (t == TypeConversation.Communication)? MODEL_COMUNICATION: MODEL_REQUEST;

                // build payload JSON
                var jsonPayload = new {
                    model = modelName,
                    prompt = chatContext!.First().Content + "\n The request is: " + message,
                    messages = chatContext,
                    stream = false,
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

        private void AssignInput(TypeConversation t, ref List<LLMChat> chatContext, List<LLMChat>? comunication, ref string modelName) {
            switch(t) {
                case TypeConversation.Request:
                    modelName = MODEL_REQUEST;
                    chatContext = analysisRequest;
                break;
                case TypeConversation.Insert:
                    modelName = MODEL_INSERT;
                    chatContext = analysistInsert;
                break;
                default:
                    modelName = MODEL_COMUNICATION;
                    chatContext = comunication!;
                break;
            }
        }

        private string ParserResponse(string response, TypeConversation t) {
            JObject jsonObj = JObject.Parse(response);

            // Estrai il valore della chiave 'response'
            var resp = jsonObj["response"]?.ToString();

            // Rimuovi i caratteri di newline e ritorna la risposta
            resp = resp!.Replace("\\n", "");
            if (t != TypeConversation.Communication && _logger is not null) _logger.LogDebug(resp); 
            return resp;
        }
    }
}
