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
        private readonly string MODEL_COMUNICATION = "mixtral";
        private readonly string MODEL_REQUEST = "mixtral";
        private readonly string MODEL_INSERT = "mixtral";

        // Lists contains requests of insertion and data request. They are used for the context at prompt
        private readonly List<ChatMessage> analysistInsert;
        private readonly List<ChatMessage> analysisRequest;
        private readonly List<ChatMessage> advice;

        private ILogger<LLMService>? _logger;

        // Factory costructor
        private OllamaService(WebApplicationBuilder builder) {
            _llmApiUrl = ConfigureUrl(builder);

            analysisRequest = Prompt.RequestContext();
            analysistInsert = Prompt.InsertContest();
            advice = Prompt.AdviceContest();
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
            var response = await llm._httpClient.GetAsync(llm._llmApiUrl!.Replace("/api/chat", ""));

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
                AssignInput(t, ref chatContext, comunicationChat, ref modelName, ref temp, message);

                var context = chatContext.Select(msg => new {
                    role = msg.Role.ToString().ToLower(), // 'user' o 'assistant'
                    content = msg.Content
                }).ToList();
                context.Add(new { role = "user", content = message });

                // build payload JSON
                var jsonPayload = new {
                    model = modelName,
                    messages = context,
                    stream = false,
                    options = new {
                        temperature = temp // value for deterministic or creative response
                    },
                };
                string jsonString = JsonConvert.SerializeObject(jsonPayload);
                await Console.Out.WriteLineAsync($"Payload JSON: {jsonString}");
                var content = new StringContent(JsonConvert.SerializeObject(jsonPayload), Encoding.UTF8, "application/json");

                // send POST request
                try {
                    var response = await _httpClient.PostAsync(_llmApiUrl, content);
                    await Console.Out.WriteLineAsync("Richiesta inviata");
                    if (response.IsSuccessStatusCode) {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        await Console.Out.WriteLineAsync($"Risposta LLM JSON: {jsonResponse}");

                        // Controllo e log della risposta prima di processarla
                        string parsedResponse = ParserResponse(jsonResponse, t);
                        await Console.Out.WriteLineAsync($"Risposta elaborata: {parsedResponse}");

                        if (!string.IsNullOrWhiteSpace(parsedResponse)) {
                            return parsedResponse;
                        }
                        else {
                            await Console.Out.WriteLineAsync("La risposta elaborata è vuota.");
                            return "Errore: la risposta dell'LLM è vuota.";
                        }
                    }
                    else {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        await Console.Out.WriteLineAsync($"Errore API: {response.StatusCode}, Contenuto: {errorResponse}");
                        return "Si è verificato un errore nella generazione del testo.";
                    }
                }
                catch (TaskCanceledException) {
                    return "Errore dal server";
                }
                catch (Exception ex) {
                    // Another log
                    await Console.Out.WriteLineAsync($"Eccezione durante la richiesta: {ex.Message}");
                    return "Errore durante la richiesta all'LLM.";
                }
            }
            return "Non è possibile rispondere a queste domande. Riprova più tardi.";
        }

        // Set parameter for each conversation model
        // The chatContext is the list of messages that will be sent to LLM: the examples in the Prompt section and the last 2 messages of the user chat.
        // The context contains the last 2 user messages that will be added to the chat context
        private void AssignInput(TypeConversation t, ref List<ChatMessage> chatContext, List<ChatMessage>? comunication, ref string modelName, ref double temp, string? message) {
            switch (t) {
                case TypeConversation.Request:
                    modelName = MODEL_REQUEST;
                    chatContext = analysisRequest;
                    temp = 0.1;
                    break;
                case TypeConversation.Insert:
                    modelName = MODEL_INSERT;
                    chatContext = analysistInsert;
                    temp = 0.1;
                    break;
                case TypeConversation.Communication:
                    modelName = MODEL_COMUNICATION;
                    chatContext = comunication!;
                    temp = 0.8;
                    break;
                case TypeConversation.Advice:
                    modelName = MODEL_COMUNICATION;
                    chatContext = advice;
                    temp = 0.7;
                    break;
            }
        }

        private string ParserResponse(string response, TypeConversation t) {
            JObject jsonObj = JObject.Parse(response);

            // Estrai il valore della chiave 'response'
            var resp = jsonObj["message"]!["content"]!.ToString();

            // Rimuovi i caratteri di newline e ritorna la risposta
            resp = resp!.Replace("\\n", "");
            if (t != TypeConversation.Communication && _logger is not null) _logger.LogInformation(resp);
            return resp;
        }
    }
}
