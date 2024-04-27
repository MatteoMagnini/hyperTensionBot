using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Database {

    // Manage database at new messages. 
    internal static class Update {

        internal static void UpdateUser(IMongoCollection<BsonDocument>? User, Telegram.Bot.Types.Update? u, Intent i, User from, string mex) {
            var user = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
            bool insert = i.ToString().Equals("Inserimento");
            if (user is not null) {
                var update = Builders<BsonDocument>.Update.Set("LastConversationUpdate", u?.Message?.Date)
                                                          .Inc("NumberMessages", 1);
                if (insert) {

                    var doc = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
                    if (!doc.Contains("DateFirstMeasurement")) {
                        update = update.Set("DateFirstMeasurement", u?.Message?.Date);
                    }
                    update = update.Set("DateLastMeasurement", u?.Message?.Date);
                }
                User?.UpdateManyAsync(Memory.GetFilter(from.Id), update);
            }
            else {
                var userInfo = new UserInformation(from, u!.Message!.Date);

                dynamic? date = insert ? u.Message?.Date : null;

                var document = new BsonDocument {
                    {"id", userInfo.TelegramId },
                    {"name", userInfo.FullName },
                    {"LastConversationUpdate", u.Message?.Date},
                    {"NumberMessages", 1},
                };
                if (date is not null) {
                    document.Add("DateFirstMeasurement", date);
                    document.Add("DateLastMeasurement", date);
                }
                User?.InsertOne(document);
            }
        }

        internal static void InsertNewMex(IMongoCollection<BsonDocument>? chat, Telegram.Bot.Types.Update? u, User? from, Intent i, string mex) {

            var documentMex = new BsonDocument {
                {"id", from?.Id},
                {"messages", mex},
                {"type", i.ToString()},
                {"Date", u?.Message?.Date},
            };
            chat?.InsertOne(documentMex);
        }
    }
}
