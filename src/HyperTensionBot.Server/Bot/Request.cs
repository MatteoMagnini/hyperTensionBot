using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.ImageSharp;
using OxyPlot.Legends;
using OxyPlot.Series;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Bot {
    // Manage data requests from User
    public static class Request {

        // set days parameter to search data
        private static int days;

        // The datas in DB are preleved and inserted in list 
        private static List<Measurement> measurementsFormatted = new();

        // take measurements
        private static string SettingsValue(Memory m, long id, ref bool pressure, ref bool frequence, string x, int d) {
            days = d;

            // first measure and date 
            var firstMeasurement = m.GetFirstMeasurement(id);
            ControlDays(firstMeasurement);

            // info di process Request è la lista di tutte le misurzioni per un utente 
            if (x == "PRESSIONE") {
                pressure = true;
                frequence = false;
                measurementsFormatted = ProcessesRequest(m, id, x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
            }
            else if (x == "FREQUENZA") {
                pressure = false;
                frequence = true;
                measurementsFormatted = ProcessesRequest(m, id, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
            }
            else if (x == "ENTRAMBI") {
                pressure = frequence = true;
                measurementsFormatted = ProcessesRequest(m, id, x => (x.SystolicPressure != null && x.DiastolicPressure != null ||
                            x.HeartRate != null) && x.Date >= DateTime.Now.Date.AddDays(-days));
            }
            if (d != -1)
                return $"negli ultimi {d} giorni";
            else
                return "";
        }


        public static async Task ManageRequest(Memory mem, long id, TelegramBotClient bot, LLMService llm, string[] parameters) {
            // 0 => context, 1 => days, 2 => format
            try {
                bool pressure = false;
                bool frequence = false;

                var text = SettingsValue(mem, id, ref pressure, ref frequence, parameters[0], int.Parse(parameters[1]));

                // selection right choice with parameters 
                if (parameters[0] != "PERSONALE") {
                    switch (parameters[2]) {
                        case "GRAFICO":
                            await bot.SendTextMessageAsync(id, $"Ecco il grafico richiesto {text}");
                            await SendPlot(bot, id, CreatePlot(pressure, frequence));
                            break;
                        case "LISTA":
                            await SendDataList(bot, id, pressure, frequence,
                                $"Ecco la lista richiesta sulle misurazioni {text}");
                            break;
                        case "MEDIA":
                            int?[] average = AverageData(mem, id, int.Parse(parameters[1]), pressure, frequence);
                            if (frequence)
                                await bot.SendTextMessageAsync(id, $"Ecco le media sulla frequenza cardiaca {text}:\n" +
                                    $"❤️ Frequenza cardiaca media: {average[2]} bpm\n");
                            if (pressure)
                                await bot.SendTextMessageAsync(id, $"Ecco le media sulla pressione arteriosa {text}:\n" +
                                    $"🔻 Pressione arteriosa media: {average[0]}/{average[1]} mmHg");
                            if (pressure)
                                await Context.CheckAverage(average, bot, id);
                            break;
                    }
                }
                else
                    await SendGeneralInfo(bot, mem, id);
            }
            catch (ArgumentNullException) {
                await bot.SendTextMessageAsync(id, "Vorrei fornirti le tue misurazioni ma non sono ancora state registrate, ricordati di farlo quotidianamente.\n\n" +
                    "Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
            catch (ArgumentException) {
                await bot.SendTextMessageAsync(id, "Non sono riuscito a comprendere il tuo messaggio. \nRiscrivi la richiesta in maniera differente, così potrò aiutarti");
            }
            catch (ExceptionExtensions.InsufficientData) {
                await bot.SendTextMessageAsync(id, "Per poterti generare il grafico necessito di almeno due misurazioni, ricordati di fornirmi giornalmente i tuoi dati.\n\n" +
                    "Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
        }

        private static void ControlDays(DateTime? date) {
            if (!date.HasValue) { throw new ArgumentNullException(); }
            if (days == -1) {
                days = DateTime.Now.Subtract((DateTime)date).Days;
            }
        }

        // find all measurements when predicate is true
        private static List<Measurement> ProcessesRequest(Memory m, long id, Predicate<Measurement> p) {
            var result = m.GetAllMeasurements(id).FindAll(p);
            if (result is null || result.Count == 0) {
                throw new ArgumentNullException();
            }
            return result;
        }

        private static PlotModel CreatePlot(bool includePress, bool includeFreq) {
            string title = "Misurazioni della Pressione e Frequenza Cardiaca";
            if (includePress && includeFreq) {
                title = "Grafico Pressione e Frequenza Cardiaca";
            }
            else if (includePress) {
                title = "Grafico solo Pressione";
            }
            else if (includeFreq) {
                title = "Grafico solo Frequenza Cardiaca";
            }

            var plotModel = new PlotModel {
                Title = title,
                DefaultFont = "DejaVu Sans",
                IsLegendVisible = true
            };

            plotModel.Axes.Add(new DateTimeAxis {
                Position = AxisPosition.Bottom,
                StringFormat = "dd/MM/yyyy",
                Title = "Data",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TitleFontWeight = FontWeights.Bold
            });

            string yAxisTitle;
            if (includePress && includeFreq) {
                yAxisTitle = "Pressione (mmHg) / Frequenza (bpm)";
            }
            else if (includePress) {
                yAxisTitle = "Pressione (mmHg)";
            }
            else if (includeFreq) {
                yAxisTitle = "Frequenza (bpm)";
            }
            else {
                yAxisTitle = "Valori";
            }

            plotModel.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = yAxisTitle,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TitleFontWeight = FontWeights.Bold
            });

            if (includePress) {
                var systolicSeries = new LineSeries {
                    Title = "Pressione Sistolica",
                    Color = OxyColors.Chocolate,
                    LineStyle = LineStyle.Solid,
                };
                var diastolicSeries = new LineSeries {
                    Title = "Pressione Diastolica",
                    Color = OxyColors.Blue,
                    LineStyle = LineStyle.Solid,
                };

                var systolicPoints = measurementsFormatted
                    .Where(m => m.SystolicPressure != null)
                    .Select(m => new DataPoint(DateTimeAxis.ToDouble(m.Date), m.SystolicPressure!.Value))
                    .ToList();

                var diastolicPoints = measurementsFormatted
                    .Where(m => m.DiastolicPressure != null)
                    .Select(m => new DataPoint(DateTimeAxis.ToDouble(m.Date), m.DiastolicPressure!.Value))
                    .ToList();

                if (systolicPoints.Count > 1 && diastolicPoints.Count > 1) {
                    systolicSeries.Points.AddRange(systolicPoints);
                    diastolicSeries.Points.AddRange(diastolicPoints);

                    plotModel.Series.Add(systolicSeries);
                    plotModel.Series.Add(diastolicSeries);
                }
                else {
                    throw new ExceptionExtensions.InsufficientData();
                }
            }

            if (includeFreq) {
                var heartRateSeries = new LineSeries {
                    Title = "Frequenza Cardiaca",
                    Color = OxyColors.Red,
                    LineStyle = LineStyle.Solid,
                    MarkerType = MarkerType.None // Rimuove i markers
                };

                var heartRatePoints = measurementsFormatted
                    .Where(m => m.HeartRate != null)
                    .Select(m => new DataPoint(DateTimeAxis.ToDouble(m.Date), m.HeartRate!.Value))
                    .ToList();

                if (heartRatePoints.Count > 1) {
                    heartRateSeries.Points.AddRange(heartRatePoints);
                    plotModel.Series.Add(heartRateSeries);
                }
                else {
                    throw new ExceptionExtensions.InsufficientData();
                }
            }

            // Configure legends 
            plotModel.Legends.Add(new Legend {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.RightTop,
                LegendOrientation = LegendOrientation.Vertical,
                LegendSymbolPlacement = LegendSymbolPlacement.Left,
            });

            return plotModel;
        }

        // Send plot to chat 
        private static async Task SendPlot(TelegramBotClient bot, long id, PlotModel plotModel) {
            using (var stream = new MemoryStream()) {
                var pngExporter = new PngExporter(width: 800, height: 500);
                pngExporter.Export(plotModel, stream);
                stream.Position = 0;

                await bot.SendPhotoAsync(id, new InputFileStream(stream));
            }
        }


        private static async Task SendDataList(TelegramBotClient bot, long id, bool press, bool freq, string mex) {
            var sbPress = new StringBuilder("\n\n");
            var sbFreq = new StringBuilder("\n");
            foreach (var m in measurementsFormatted) {

                if (press && m.SystolicPressure != null && m.DiastolicPressure != null)
                    sbPress.AppendLine($"🔻 Pressione {m.SystolicPressure}/{m.DiastolicPressure} mmgh misurata il {m.Date}");
                if (freq && m.HeartRate != null)
                    sbFreq.AppendLine($"❤️ Frequenza {m.HeartRate} bpm misurata il {m.Date}");
            }
            await bot.SendTextMessageAsync(id, $"{mex}{sbPress}{sbFreq}");
        }

        private static async Task SendGeneralInfo(TelegramBotClient bot, Memory memory, long id) {

            StringBuilder sb = new();

            foreach (var s in memory.GetGeneralInfo(id)) {
                sb.Append(s + "\n");
            }
            if (sb.Length > 0)
                await bot.SendTextMessageAsync(id, "Ecco un elenco di tutte le informazioni generali registrate finora!!🗒️\n\n" + sb.ToString());
            else
                await bot.SendTextMessageAsync(id, "Non sono presenti dati personali nel tuo storico. Queste informazioni sono molto importanti perchè offrono al dottore " +
                    "una panoramica più ampia della tua situazione. Ogni informazione può essere preziosa🗒️");
        }


        public static int?[] AverageData(Memory memory, long id, int d, bool pressure, bool frequence) {
            int?[] average = new int?[3];
            days = d;
            var firstMeasurement = memory.GetFirstMeasurement(id);
            if (!firstMeasurement.HasValue)
                throw new ArgumentNullException();
            ControlDays(firstMeasurement);

            if (pressure) {
                var press = ProcessesRequest(memory, id, x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                average[0] = (((int?)press.Select(m => m.SystolicPressure).Where(x => x != null).Average()));
                average[1] = ((int?)press.Select(m => m.DiastolicPressure).Where(x => x != null).Average());
            }
            if (frequence) {
                var freq = ProcessesRequest(memory, id, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));

                average[2] = ((int?)freq.Select(x => x.HeartRate).Where(x => x != null).Average());
            }
            return average;
        }

        // Ask confirm to extracted parameters. They are extracted by LLM. 
        public static async Task AskConfirmParameters(LLMService llm, TelegramBotClient bot, Memory memory, string message, long id) {
            string outLLM = await llm.HandleAskAsync(TypeConversation.Request, message);
            var parameters = RegexExtensions.ExtractParameters(outLLM);
            memory.SetTemporaryParametersRequest(id, parameters);
            await SendMessagesExtension.SendButton(bot, $"Stai facendo richiesta per:\n{SendMessagesExtension.DefineRequestText(parameters)}",
                    id, new string[] { "Sì, esatto!", "yesReq", "No", "noReq" });
        }

        internal static async Task ValuteRequest(string resp, long id, TelegramBotClient bot, Memory memory, LLMService llm) {
            // if LLM correctly extract the parameters by requrest continue, else take it manually by user 
            if (resp == "yesReq") {
                await ManageRequest(memory, id, bot, llm, memory.GetParameters(id));
            }
            else if (resp == "noReq") {
                // Set state to context and send button 
                await SendMessagesExtension.SendChoiceRequest(bot, memory, id,
                    new string[] {
                            "Pressione", "PRESSIONE", "Frequenza", "FREQUENZA",
                            "Entrambi", "ENTRAMBI", "Dati personali", "PERSONALE"
                        }, ConversationInformation.RequestState.ChoiceContext, "il dato");
            }
        }

        // build parameters if LLM has wrong
        public static async Task ModifyParameters(TelegramBotClient bot, long id, Memory memory, string resp, int idMessage, LLMService llm) {
            switch (memory.GetRequestState(id)) {
                case ConversationInformation.RequestState.ChoiceContext:
                    await TemporaryChoice(bot, memory, idMessage, id, resp, ConversationInformation.RequestState.ChoiceTimeSpan, 0,
                        new string[] {
                            "Giorno", "1", "Settimana", "7",
                            "Mese", "30", "Tutti", "-1"}, "l'arco temporale");
                    break;

                case ConversationInformation.RequestState.ChoiceTimeSpan:
                    await TemporaryChoice(bot, memory, idMessage, id, resp, ConversationInformation.RequestState.ChoiceFormat, 1,
                        new string[] { "Elenco", "LISTA", "Grafico", "GRAFICO", "Media", "MEDIA" },
                            "il formato");
                    break;

                case ConversationInformation.RequestState.ChoiceFormat:
                    var param = memory.GetParameters(id);
                    param[2] = resp;
                    memory.SetTemporaryParametersRequest(id, param);

                    await ManageRequest(memory, id, bot, llm, memory.GetParameters(id));
                    break;
            }

        }
        private static async Task TemporaryChoice(TelegramBotClient bot, Memory memory, int idMessage, long id, string resp, ConversationInformation.RequestState state, int index, string[] choice, string text) {

            var param = memory.GetParameters(id);
            param[index] = resp;
            memory.SetTemporaryParametersRequest(id, param);

            await SendMessagesExtension.SendChoiceRequest(bot, memory, id, choice, state, text);
        }

    }
}
