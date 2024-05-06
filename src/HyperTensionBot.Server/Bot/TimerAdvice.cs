using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using ScottPlot.Drawing.Colormaps;
using Telegram.Bot;

namespace HyperTensionBot.Server.Bot {
    public class TimerAdvice {
        private readonly System.Timers.Timer _timer;

        public TimerAdvice(Memory m, TelegramBotClient bot) {
            _timer = new System.Timers.Timer();
            TimerStart(m, bot);
        }

        // the timer runs an event every days
        private void TimerStart(Memory m, TelegramBotClient bot) {
            _timer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            _timer.Elapsed += async (e, o) => await AdvicePatients(m, bot);
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        // check on the date of last insertion 
        private async Task AdvicePatients(Memory m, TelegramBotClient bot) {
            var patients = m.GetAllPatients();
            foreach (var p in patients) {
                var timePassed = DateTime.Now - Time.Convert((DateTime)p["DateLastMeasurement"]);
                if (timePassed.Hours > TimeSpan.FromDays(2).Hours) {
                    await bot.SendTextMessageAsync((long)p["id"],
                        $"Salve {p["name"]}, sono passate circa {timePassed.Days} giorni dalla tua ultima misurazione\n\n" +
                        $"Facciamo un altro check sulla pressione e sulla frequenza assieme...\n" +
                        $"Il dottore non vede l'ora di valutare i nuovi dati🧑🏽‍⚕️");
                }
            }
        }
    }
}
