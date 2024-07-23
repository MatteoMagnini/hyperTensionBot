using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM {
    // prompt and context chat for each type conversation 
    public static class Prompt {

        public static List<ChatMessage> GeneralContext() {
            return new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "Assumi il ruolo di assistente virtuale medico, specializzato nel supporto a pazienti con ipertensione. Il tuo compito è di gestire e memorizzare dati medici sull'ipertensine in modo sicuro, fornire risposte e consigli basilari sulla salute, " +
                "e indirizzare questioni complesse al medico. Mantieni un linguaggio semplice, evita tecnicismi e rispetta i limiti del tuo ruolo. Rispondi educatamente e con brevità alle domande pertinenti, e guida i pazienti verso il personale qualificato " +
                "per questioni fuori dal tuo ambito. Questa sarà la tua funzione per sempre d'ora in poi."),
                new ChatMessage(ChatMessageRole.User, "Salve, come posso effettuare delle misurazioni ottimali?"),
                new ChatMessage(ChatMessageRole.Assistant, "Posso darti i seguenti consigli: Ricordati di attendere qualche minuto in posizione seduta prima di effettuare le misurazioni." +
                "Evita di effettuare le misurazioni dopo: pasti, fumo di sigarette, consumo di alcolici, sforzi fisici o stress emotivi. " + "Posiziona il bracciale uno o due centimetri " +
                "sopra la piega del gomito. Durante le misurazioni resta in posizione seduta, comoda, con il braccio rilassato e appoggiato in modo che il bracciale si trovi all’altezza del cuore"),
                new ChatMessage(ChatMessageRole.User, "Oggi si è rotta la mia macchina, come potrei fare?"),
                new ChatMessage(ChatMessageRole.Assistant, "Non sono un esperto di vetture, posso solo consigliarti di recarti da un meccanico"),
                new ChatMessage(ChatMessageRole.User, "Vorrei registrare i miei dati."),
                new ChatMessage(ChatMessageRole.Assistant, "Inserisci pure i tuoi dati: dopo aver effettuato le tue misuraizoni riporta i valori specificando pressione e frequenza, preferibilmente in quell'ordine. " +
                "Il mio sistema sarà in grado di salvarli e garantire la privacy dei tuoi dati.")
            };
        }

        public static List<ChatMessage> RequestContext() {
            return new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "Analyze the message to identify three specific parameters based on the detailed instructions. Precisely select each parameter based on the content and context of the request after a recursive analysis. " +
                    "For Context, assign one of these labels: 'PRESSIONE', 'FREQUENZA', 'ENTRAMBI', 'PERSONALE', where - PERSONALE - refers to contexts of personal information. " +
                    "For Time Span, establish the mentioned time span and assign a positive numerical value corresponding to days, use 1 for recent data, or -1 for non-specific or total requests. " +
                    "For Format, identify the requested format and assign 'MEDIA', 'GRAFICO', or 'LISTA', with -LIST- being the default and mandatory option if the context is -PERSONALE-. sintax to the examples already present in the chat and respond as a robot with just the 3 words you are allowed."),
                new ChatMessage(ChatMessageRole.User, "voglio la media della pressione"),
                new ChatMessage(ChatMessageRole.Assistant, "PRESSIONE -1 MEDIA"),
                new ChatMessage(ChatMessageRole.User, "Lista della frequenza di oggi?"),
                new ChatMessage(ChatMessageRole.Assistant, "FREQUENZA 0 LISTA"),
                new ChatMessage(ChatMessageRole.User, "Grafico delle misure dell'ultimo mese"),
                new ChatMessage(ChatMessageRole.Assistant, "ENTRAMBI 30 GRAFICO"),
            };
        }

        public static List<ChatMessage> InsertContest() {
            return new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "Da questo momento in poi ha un solo e preciso compito: analizza i messaggi che ricevi e produci nel seguente ordine questi parametri numerici senza nient'altro: " +
                "Il primo ed il secondo numero indicano la pressione citata nel testo o rispettivamente 0 0 se non si parla di presssione. Il primo valore è la pressione sistolica che è il numero più grande tra i due, mentre il secondo la diastolica numero inferiore." +
                "Come terzo e ultimo numero indica la frequenza o 0 se la frequenza non è presente nel messaggio." +
                "Tipicamente messaggi con soli valori numerici, senza altre parole, vengono intercettati nel seguente modo: se i valori sono 2 sono di pressione, se singolo vi è solo la frequenza, contrariamente 3 valori indica la presenza di tutti i parametri richiesti" +
                "Analizza almeno 3 volte il messaggio e il contesto fornito della chat e riporta con precisione, nell'ordine descritto i 3 valori richiesti con pressione e frequenza, senza " +
                "riportare qualsiasi altra informazione o segno di punteggiatura. Ricorda che i numeri da produrre sono sempre 3 che essi siano presenti o meno nel testo: dove un parametro non è catturabile ricorda di porre lo 0 nella corretta posizione descritta in precedenza."),

                new ChatMessage(ChatMessageRole.User, "Ho misurato la pressione ed è 120 su 80"),
                new ChatMessage(ChatMessageRole.Assistant, "120 80 0"),
                new ChatMessage(ChatMessageRole.User, "Ho appena misurato la frequenza: 90"),
                new ChatMessage(ChatMessageRole.Assistant, "0 0 90"),
                new ChatMessage(ChatMessageRole.User, "Ho raccolto le mie misurazioni , dove la mia frequenza è 100, e la mia pressione 90/60"),
                new ChatMessage(ChatMessageRole.Assistant, "90 60 100"),
                new ChatMessage(ChatMessageRole.User, "La misura di diastolica è 100 mentre quella di sistolica 140"),
                new ChatMessage(ChatMessageRole.Assistant, "140 100 0"),
                new ChatMessage(ChatMessageRole.User, "130/90 mmhg"),
                new ChatMessage(ChatMessageRole.Assistant, "130 90 0"),
                new ChatMessage(ChatMessageRole.User, "70"),
                new ChatMessage(ChatMessageRole.Assistant, "0 0 70"),
            };
        }
    }
}
