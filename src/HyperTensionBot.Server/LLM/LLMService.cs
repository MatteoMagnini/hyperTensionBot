using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using OpenAI_API;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Concurrent;

namespace HyperTensionBot.Server.LLM {
    public class LLMService {

        private readonly HttpClient _httpClient = new HttpClient();
        // URL dell'API del LLM
        private string? _llmApiUrl;
        // set names to different model 
        private readonly string MODEL_COMUNICATION = "nous-hermes2-mixtral";
        private readonly string MODEL_ANALYSIS = "nous-hermes2-mixtral";

        private List<ChatMessage> analysisChat = new();

        private ILogger<LLMService>? _logger; 


        // Build option for client 
        public LLMService(WebApplicationBuilder builder) {
            ConfigureUrl(builder);
            _httpClient.Timeout = TimeSpan.FromSeconds(200);

            analysisChat.Add(new ChatMessage("assistant", BaseRequestPrompt()));
        }
        public void SetLogger(ILogger<LLMService> logger) { _logger = logger; }
        private string BaseComunicationPrompt() {
            return "Comportati come un dottore specializzato sull'ipertensione, rivolgendoti in modo semplice e breve come negli esempi che seguiranno. " +
                "Puoi far inserire, memorizzare ed elaborare i dati medici garantendo sicurezza, quindi quando richiesto comportati come un assistente nei confronti del paziente.\" " +
                "\"Puoi rispondere a domande relative all'ipertensione o fornire semplicissimi consigli generali sulla salute, " +
                "sul benessere su argomenti medici in PERSONALE, mantenendo un alta astrazione ai tecnicismi che non ti competono e che sarà il dottore a prenderne atto. \" +\n                    " +
                "\"Infatti, non sei in grado di fornire consigli articolati sul campo medico diversi dal contesto ipertensione o rispondere a domande fuori contesto medico.\" +\n " +
                "\"Usa un tono educato, rispondi in maniera chiara e semplice se gli input sono inerenti al ruolo descritto altrimenti se fuori contesto sii generico e responsabilizza il paziente verso chi di dovere.\"" +
                "Prendi in considerazioni le seguenti domande con le corrette risposte dopo -> esclusivamente come contesto di base. Non riprendere tali esempi nelle tue risposte se non strettamente correlate ad essi: " +
                "Salve, come posso effettuare delle misurazioni ottimali? -> Posso darti i seguenti consigli: Ricordati di attendere qualche minuto in posizione seduta \" +\n                    " +
                "\"prima di effettuare le misurazioni. Evita di effettuare le misurazioni dopo: pasti, fumo di sigarette, consumo di alcolici, sforzi fisici o stress emotivi. \" +\n                    " +
                "\"Posiziona il bracciale uno o due centimetri sopra la piega del gomito. Durante le misurazioni resta in posizione seduta, comoda, con il braccio rilassato e appoggiato in modo che \" +\n                    " +
                "\"il bracciale si trovi all’altezza del cuore" +
                "Oggi si è rotta la mia macchina, come potrei fare? -> Non sono un esperto di vetture, posso solo consigliarti di recarti da un meccanico" +
                "Vorrei registrare i miei dati. -> Inserisci pure i tuoi dati. Il mio sistema sarà in grado di salvarli e garantire la privacy dei tuoi dati." +
                "Sulla base del tuo ruolo rispondi esclusivamente al seguente messaggio esclusivamente in lingua italiana: ";
        }

        private string BaseRequestPrompt() {
            return "devi analizzare e produrre esclusivamente con 3 etichette (Mostrate fra '..' ma dovrai riportare solo la parola senza nient'altro). La prima etichetta descrive il contesto richiesta" +
                "'PRESSIONE', 'FREQUENZA', 'ENTRAMBI' (quando la richiesta indica sia pressione che frequenza oppure è generica), 'PERSONALE' (per richieste che intendono le informazioni personali). " +
                "Il secondo parametro è l'arco temporale espresso in giorni sempre positivi: eccezione fanno i dati recenti con risposta 1, e la totalità dei dati o richieste non specifiche con -1. " +
                "Il terzo parametro indica il formato che potrà essere 'MEDIA' (si vuole la media dei dati), 'GRAFICO' (con richieste di rappresentazioni e andamenti), 'LISTA' (In tutti gli altri casi si fornisce sempre la lista. Inoltre se il primo parametro è PERSONALE questo è sempre lista)" +
                "Il tuo output da questo momento in poi deve essere con le sole 3 etichette senza virgole punti o altro." +
                "Sii conciso, rispondi esattamente solo con i tre valori. " +
                "Esempi: \n      \"voglio la media della pressione di ieri\" -> \"PRESSIONE 1 MEDIA\".\n      " +
                "\"Lista della frequenza di oggi?\" -> \"FREQUENZA 0 LISTA\".\n      " +
                "\"Grafico delle misure dell'ultimo mese\" -> \"ENTRAMBI 30 GRAFICO\".\n      " +
                "\"Tutti i dati della pressione\" -> \"PRESSIONE -1 LISTA\".\n      " +
                "\"Voglio sapere la frequenza degli ultimi 6 mesi\" -> \"FREQUENZA 180 LISTA\".\n      " +
                "\"Volgio visualizzare pressione e frequenza delle ultime due settimane\" -> \"ENTRAMBI 14 GRAFICO\".\n      " +
                "\"Riassunto di tutte le informazioni che ho fornito finora\" -> \"PERSONALE -1 LISTA\".\n    " +
                "Puoi rispondere solo con i tre valori in ordine separati da spazio." +
                "Ora estrai i parametri dal seguente messagio: ";
        }

        private void ConfigureUrl(WebApplicationBuilder builder) {
            var buildCluster = builder.Configuration.GetSection("Clusters");
            if (!buildCluster.Exists() && buildCluster["UrlLLM"] != null)
                throw new ArgumentException("Configuration Cluster: Url Cluster is not set");
            _llmApiUrl = buildCluster["UrlLLM"];
        }

        // connection and interaction with server for request to LLM 
        public async Task<string> AskLlm(TypeConversation t, string message, List<ChatMessage>? comunicationChat = null) {

            if (_llmApiUrl != "") {
                string modelName;
                if (t == TypeConversation.Communication) {
                    modelName = MODEL_COMUNICATION;
                }
                else {
                    modelName = MODEL_ANALYSIS;
                    analysisChat.Add(new ChatMessage("user", message));
                }

                // build payload JSON
                var jsonPayload = new {
                    model = modelName,
                    prompt = ((t == TypeConversation.Communication) ? BaseComunicationPrompt() : analysisChat.First().Content) + message,
                    messages = (t == TypeConversation.Communication) ? comunicationChat : analysisChat,
                    stream = false,
                    message = "fdsf"
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

        private string ParserResponse(string response, TypeConversation t) {
            JObject jsonObj = JObject.Parse(response);

            // Estrai il valore della chiave 'response'
            var resp = jsonObj["response"]?.ToString();

            // Rimuovi i caratteri di newline e ritorna la risposta
            resp = resp!.Replace("\\n", "");
            if (t == TypeConversation.Analysis && _logger is not null) _logger.LogDebug(resp); 
            return resp;
        }
    }
}
