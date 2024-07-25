using HyperTensionBot.Server.Bot;
using Microsoft.VisualBasic;
using OpenAI_API.Chat;
using ScottPlot.Palettes;
using static ScottPlot.Plottable.PopulationPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System;
using Telegram.Bot.Types;

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
                    "For Format, identify the requested format and assign 'MEDIA', 'GRAFICO', or 'LISTA', with -LIST- being the default and mandatory option if the context is -PERSONALE-. " +
                    "The syntax to use is shown in the prompt. The only words in capital letters are the 3 words. Reply like a robot with only the 3 words you are allowed without adding anything else."),
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
                new ChatMessage(ChatMessageRole.User, "From now on, you have one precise task: analyze the messages you receive and produce the following numerical parameters in the specified order, without anything else: " +
                "The first and second numbers indicate the mentioned blood pressure in the text or 0 if there is no blood pressure. The first value represents the systolic pressure(the larger number), while the second " +
                "represents the diastolic pressure(the smaller number). The third and final number indicates the heart rate or 0 if the heart rate is not present in the message. Typically, messages with only numeric values, " +
                "without other words, are intercepted as follows: if there are 2 values, they refer to blood pressure; if there’s a single value, it represents the heart rate; conversely, 3 values indicate the presence " +
                "of all the required parameters.Analyze the message and context provided in the chat at least 3 times, and accurately report the 3 requested values for blood pressure and heart rate, without including any other " +
                "information or punctuation marks.Remember that you should always produce 3 numbers, whether they are present in the text or not.If a parameter cannot be captured, replace it with 0 in the correct position as described above."),
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
