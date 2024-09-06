using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Bot {
    // timer for advice users when they not send new insertions 
    public class TimerAdvice {
        private readonly System.Timers.Timer _timer;
        private readonly LLMService _llm;
        private readonly TelegramBotClient _bot;
        private readonly Memory _mem;

        public TimerAdvice(Memory mem, TelegramBotClient bot, LLMService llm) {
            _timer = new System.Timers.Timer();
            _llm = llm;
            _mem = mem;
            _bot = bot;
            TimerStart();
        }

        // the timer runs an event every days
        private void TimerStart() {
            _timer.Interval = TimeSpan.FromDays(2).TotalMilliseconds;
            _timer.Elapsed += async (e, o) => await AdvicePatients();
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        // check on the date of last insertion 
        private async Task AdvicePatients() {
            var patients = _mem.GetAllPatients();
            foreach (var p in patients) {
                // if DateDeactivate don't exists, add min date value to DB
                if (!p.Contains("DateDeactivate")) {
                    var update = Builders<BsonDocument>.Update.Set("DateDeactivate", DateTime.MinValue);
                    _mem.User!.UpdateOne(Memory.GetFilter(p["id"].AsInt64), update);
                }
                // update patient after update field
                var pUp = await _mem.User!.Find(Memory.GetFilter(p["id"].AsInt64)).FirstOrDefaultAsync();
                var timeSilence = DateTime.Now - Time.Convert((DateTime)pUp["DateDeactivate"]);
                if (timeSilence > TimeSpan.FromDays(7)) {
                    // Check time passed and send mex 
                    var timePassed = DateTime.Now - Time.Convert((DateTime)pUp["DateLastMeasurement"]);
                    if (timePassed > TimeSpan.FromDays(2)) {
                        await SendMessagesExtension.SendButton(_bot, await _llm.HandleAskAsync(TypeConversation.Advice, "Genera il messaggio di avviso per il paziente iperteso"),
                            pUp["id"].AsInt64, new string[] { "Silenzia per una settimana", "sil", "Continua a ricevere avvisi", "adv" });
                    }
                }
            }
        }

        // deactivate notify
        public void DeactivateNotify(DateTime date, User from, string choice) {
            if (choice == "sil")
                Database.Update.UpdateUser(_mem.User, date, Intent.Generale, from, "", true);
        }
    }
}
