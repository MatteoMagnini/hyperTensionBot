using HyperTensionBot.Server.LLM;
using Telegram.Bot.Types;
using Telegram.Bot;
using HyperTensionBot.Server.Services;
using System.Drawing;
using ScottPlot;
using System.Text;
using HyperTensionBot.Server.Bot.Extensions;

namespace HyperTensionBot.Server.Bot {
    public static class Request {

        private static int days;
        private static List<Measurement> measurements = new();

        // manage request

        private static string  SettingsValue(ref bool pressure,ref bool frequence, string x, int d, UserInformation info) {
            days = d;
            ControlDays(info);
            if (x == "PRESSIONE") {
                pressure = true;
                frequence = false;
                measurements = ProcessesRequest(info, x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
            }
                else if (x == "FREQUENZA") {
                    pressure = false;
                    frequence = true;
                    measurements = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                }
                else if (x == "ENTRAMBI"){
                    pressure = frequence = true;
                    measurements = ProcessesRequest(info, x => (x.SystolicPressure != null && x.DiastolicPressure != null ||
                                x.HeartRate != null) && x.Date >= DateTime.Now.Date.AddDays(-days));
                }
            if (d != -1)
                return $"negli ultimi {d} giorni";
            else
                return "";
        }

        public static async Task ManageRequest(string message, Memory mem, Chat chat, TelegramBotClient bot, GPTService gpt) { 
            // 0 => contesto, 1 => giorni, 2 => formato
            try {
                var outGPT = await gpt.CallGpt(TypeConversation.Analysis, message);
                var parameters = RegexExtensions.ExtractParameters(outGPT);
                await bot.SendTextMessageAsync(chat.Id, outGPT);
                bool pressure = false;
                bool frequence = false;

                mem.UserMemory.TryGetValue(chat.Id, out var info);
                var text = SettingsValue(ref pressure, ref frequence, parameters[0], int.Parse(parameters[1]), info!);

                if (parameters[0] != "GENERALE") {
                    switch (parameters[2]) {
                        case "GRAFICO":
                            await bot.SendTextMessageAsync(chat.Id, $"Ecco il grafico richiesto {text}");
                            await SendPlot(bot, chat, CreatePlot(pressure, frequence));
                            break;
                        case "LISTA":
                            await SendDataList(bot, chat, pressure, frequence,
                                $"Ecco la lista richiesta sulle misurazioni {text}");
                            break;
                        case "MEDIA":
                            List<int?> average = AverageData(mem, chat, int.Parse(parameters[1]), pressure, frequence);
                            if (frequence)
                                await bot.SendTextMessageAsync(chat.Id, $"Ecco le media sulla frequenza cardiaca {text}:\n" +
                                    $"❤️ Frequenza cardiaca media: {average[2]} bpm\n");
                            if (pressure)
                                await bot.SendTextMessageAsync(chat.Id, $"Ecco le media sulla pressione arteriosa {text}:\n" +
                                    $"🔻 Pressione arteriosa media: {average[0]}/{average[1]} mmHg");
                            await Context.CheckAverage(average, bot, chat);
                            break;
                    }
                } else
                    await SendGeneralInfo(bot, mem, chat);

            }
            catch (ArgumentNullException) {
                await bot.SendTextMessageAsync(chat.Id, "Vorrei fornirti le tue misurazioni ma non sono ancora state registrate, ricordati di farlo quotidianamente.😢\n\n" +
                    "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
            catch (ExceptionExtensions.InsufficientData) {
                await bot.SendTextMessageAsync(chat.Id, "Per poterti generare il grafico necessito di almeno due misurazioni, ricordati di fornirmi giornalmente i tuoi dati.😢\n\n" +
                    "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
            }
        }

        private static void ControlDays(UserInformation? info) {
            if (info?.FirstMeasurement is null) { throw new ArgumentNullException(); }
            if (days == -1) {
                days = DateTime.Now.Subtract(info.FirstMeasurement!.Date).Days;
            }
        }

        private static List<Measurement> ProcessesRequest(UserInformation? i, Predicate<Measurement> p) {
            var result =  i?.Measurements.FindAll(p);
            if (result is null || result.Count == 0) {
                throw new ArgumentNullException();
            }
            return result;
        }

        private static Plot CreatePlot(bool includePress, bool includeFreq) {
            var plot = new Plot(600, 400);

            if (includePress) {
                double[] datePressure = measurements.Where(m => m.SystolicPressure != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] systolic = measurements.Select(m => m.SystolicPressure).Where(x => x != null).ToArray();
                double?[] diastolic = measurements.Select(m => m.DiastolicPressure).Where(x => x != null).ToArray();
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
                double[] dateFrequence = measurements.Where(m => m.HeartRate != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] frequence = measurements.Select(m => m.HeartRate).Where(x => x != null).ToArray();
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

        private static async Task SendPlot(TelegramBotClient bot, Chat chat, Plot plot) {
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
                await bot.SendPhotoAsync(chat.Id, new InputFileStream(ms));
            }
        }

        private static async Task SendDataList(TelegramBotClient bot, Chat chat, bool press, bool freq, string mex) {
            var sbPress = new StringBuilder();
            var sbFreq = new StringBuilder();
            foreach (var m in measurements) {

                if (press && m.SystolicPressure != null && m.DiastolicPressure != null) 
                    sbPress.AppendLine($"🔻 Pressione {m.SystolicPressure}/{m.DiastolicPressure} mmgh misurata il {m.Date}");
                if (freq && m.HeartRate != null)
                    sbFreq.AppendLine($"❤️ Frequenza {m.HeartRate} bpm misurata il {m.Date}");                    
            }
            await bot.SendTextMessageAsync(chat.Id, $"{mex}\n\n{sbPress}\n\n{sbFreq}");
        }

        private static async Task SendGeneralInfo(TelegramBotClient bot, Memory memory, Chat chat) {

            StringBuilder sb = new StringBuilder();
            
            foreach(var s in memory.GetGeneralInfo(chat)) {
                sb.Append(s + "\n");
            }
            if (sb.Length > 0) 
                await bot.SendTextMessageAsync(chat.Id, "Ecco un elenco di tutte le informazioni generali registrate finora!!🗒️\n\n" + sb.ToString());
            else
                await bot.SendTextMessageAsync(chat.Id, "Non sono presenti dati personali nel tuo storico. Queste informazioni sono molto importanti perchè offrono al dottore" +
                    "una panoramica più ampia della tua situazione. Ogni informazione può essere preziosa🗒️");
        }

        public static List<int?> AverageData(Memory memory, Chat chat, int d, bool pressure, bool frequence) {
            List<int?> average = new();
            days = d;
            memory.UserMemory.TryGetValue(chat.Id, out var info);
            if (info?.FirstMeasurement == null)
                throw new ArgumentNullException();
            ControlDays(info);

            if (pressure) {
                var press = ProcessesRequest(info,
                    x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                average.Add(((int?)press.Select(m => m.SystolicPressure).Where(x => x != null).Average()));
                average.Add((int?)press.Select(m => m.DiastolicPressure).Where(x => x != null).Average());
            }
            if (frequence) {
                var freq = ProcessesRequest(info,
                    x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));

                average.Add((int?)freq.Select(x => x.HeartRate).Where(x => x != null).Average());
            }

            return average;
        }
    }
}
