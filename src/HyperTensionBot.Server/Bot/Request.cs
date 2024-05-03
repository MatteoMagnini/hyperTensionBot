using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using ScottPlot;
using ScottPlot.Renderable;
using System;
using System.Drawing;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Bot {
    public static class Request {

        private static int days;
        private static List<Measurement> measurementsFormatted = new();

        // manage request

        // because there is a obsolete method in GetFirstMeasurement().. 
        private static string SettingsValue(Memory m, long id, ref bool pressure, ref bool frequence, string x, int d) {
            days = d;

            // info di control days è la data della prima misurazione
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
            // 0 => contesto, 1 => giorni, 2 => formato
            try {
                bool pressure = false;
                bool frequence = false;

                var text = SettingsValue(mem, id, ref pressure, ref frequence, parameters[0], int.Parse(parameters[1]));

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
                await bot.SendTextMessageAsync(id, "Vorrei fornirti le tue misurazioni ma non sono ancora state registrate, ricordati di farlo quotidianamente.😢\n\n" +
                    "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
            catch (ArgumentException) {
                await bot.SendTextMessageAsync(id, "Non sono riuscito a comprendere il tuo messaggio. \nRiscrivi la richiesta in maniera differente, così potrò aiutarti😁");
            }
            catch (ExceptionExtensions.InsufficientData) {
                await bot.SendTextMessageAsync(id, "Per poterti generare il grafico necessito di almeno due misurazioni, ricordati di fornirmi giornalmente i tuoi dati.😢\n\n" +
                    "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
        }

        private static void ControlDays(DateTime? date) {
            if (!date.HasValue) { throw new ArgumentNullException(); }
            if (days == -1) {
                days = DateTime.Now.Subtract((DateTime)date).Days;
            }
        }

        private static List<Measurement> ProcessesRequest(Memory m, long id, Predicate<Measurement> p) {
            var result = m.GetAllMeasurements(id).FindAll(p);
            if (result is null || result.Count == 0) {
                throw new ArgumentNullException();
            }
            return result;
        }

        private static Plot CreatePlot(bool includePress, bool includeFreq) {
            var plot = new Plot(600, 400);

            if (includePress) {
                double[] datePressure = measurementsFormatted.Where(m => m.SystolicPressure != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] systolic = measurementsFormatted.Select(m => m.SystolicPressure).Where(x => x != null).ToArray();
                double?[] diastolic = measurementsFormatted.Select(m => m.DiastolicPressure).Where(x => x != null).ToArray();
                if (systolic.Length > 1 && diastolic.Length > 1) {
                    plot.AddScatterLines(datePressure, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Chocolate, 1, LineStyle.Solid, "Pressione Sistolica");
                    plot.AddScatterPoints(datePressure, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                    plot.AddScatterLines(datePressure, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
                    plot.AddScatterPoints(datePressure, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                }
                else throw new ExceptionExtensions.InsufficientData();
            }
            if (includeFreq) {
                double[] dateFrequence = measurementsFormatted.Where(m => m.HeartRate != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] frequence = measurementsFormatted.Select(m => m.HeartRate).Where(x => x != null).ToArray();
                if (frequence.Length > 1) {
                    plot.AddScatterLines(dateFrequence, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Red, 1, LineStyle.Solid, "Frequenza cardiaca");
                    plot.AddScatterPoints(dateFrequence, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                }
                else
                    throw new ExceptionExtensions.InsufficientData();
            }

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Pressione (mmHg) / Frequenza (bpm)");
            plot.XLabel("Data");

            return plot;
        }

        private static async Task SendPlot(TelegramBotClient bot, long id, Plot plot) {
            Bitmap im = plot.Render();
            Bitmap leg = plot.RenderLegend();
            Bitmap b = new Bitmap(im.Width + leg.Width, im.Height);
            using Graphics g = Graphics.FromImage(b);
            g.Clear(System.Drawing.Color.White);
            g.DrawImage(im, 0, 0);
            g.DrawImage(leg, im.Width, (b.Height - b.Height) / 2);
            using (var ms = new MemoryStream()) {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                await bot.SendPhotoAsync(id, new InputFileStream(ms));
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

            StringBuilder sb = new StringBuilder();

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

        public static async Task AskConfirmParameters(LLMService llm, TelegramBotClient bot, Memory memory, string message, long id) {
            string outLLM = await llm.HandleAskAsync(TypeConversation.Request, message);
            var parameters = RegexExtensions.ExtractParameters(outLLM); 
            memory.SetTemporaryParametersRequest(id, parameters);
            await SendMessagesExtension.SendButton(bot, $"Mi stai chiedendo di:\\{SendMessagesExtension.DefineRequestText(parameters)}",
                    id, new string[] { "Sì, esatto!", "yesReq", "No", "noReq" }); 
        }

        internal static async Task ValuteRequest(string resp, long id, TelegramBotClient bot, Memory memory, LLMService llm) {
            // if LLM correctly extract the parameters by requrest continue, else take it manually by user 
            if (resp == "yesReq") {
                await ManageRequest(memory, id, bot, llm, memory.GetParameters(id)); 
            }
            else if (resp == "noReq") {
                await ModifyParameters(bot, id); 
            }
        }

        // build parameters 
        private static async Task ModifyParameters(TelegramBotClient bot, long id) {
            await bot.SendTextMessageAsync(id, "Qui ci saranno le inline Keybord"); 
        }
    }
}
