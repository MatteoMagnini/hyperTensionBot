using HyperTensionBot.Server.Services;
using Telegram.Bot;

namespace HyperTensionBot.Server.Bot {
    public class TimerAdvice {
        private System.Timers.Timer _timer;

        public TimerAdvice(Memory m, TelegramBotClient bot) {
            _timer = new System.Timers.Timer();
            TimerStart(m, bot);
        }

        private void TimerStart(Memory m, TelegramBotClient bot) {
            // the timer runs an event every hour
            _timer.Interval = TimeSpan.FromHours(3).TotalMilliseconds;
            _timer.Elapsed += async (e, o) => await AdvicePatients(m, bot);
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async Task AdvicePatients(Memory m, TelegramBotClient bot) {
            var patients = m.UserMemory.Where(pait => pait.Value.LastMeasurement is not null);
            foreach (var p in patients) {
                var timePassed = DateTime.Now - p.Value.LastMeasurement!.Date;
                if (timePassed.Hours > TimeSpan.FromDays(2).Hours) {
                    await bot.SendTextMessageAsync(p.Key,
                        $"Salve {p.Value.FullName} è passato circa un giorno dalla tua ultima misurazione🕰️\n\n" +
                        $"Facciamo un altro check sulla pressione e sulla frequenza assieme...\n" +
                        $"Il dottore non vede l'ora di valutare i nuovi dati🧑🏽‍⚕️");
                }
            }
        }
    }
}
