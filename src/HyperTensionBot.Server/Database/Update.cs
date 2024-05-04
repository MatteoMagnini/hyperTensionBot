using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Database {

    // Manage database at new messages. 
    internal static class Update {

        internal static void UpdateUser(IMongoCollection<BsonDocument>? User, DateTime date, Intent i, User from, string mex) {
            var user = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
            bool insert = i.ToString().Equals("Inserimento");
            if (user is not null) {
                var update = Builders<BsonDocument>.Update.Set("LastConversationUpdate", date)
                                                          .Inc("NumberMessages", 1);
                if (insert) {

                    var doc = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
                    if (!doc.Contains("DateFirstMeasurement")) {
                        update = update.Set("DateFirstMeasurement", date);
                    }
                    update = update.Set("DateLastMeasurement", date);
                }
                User?.UpdateManyAsync(Memory.GetFilter(from.Id), update);
            }
            else {
                var userInfo = new UserInformation(from, date);

                dynamic? dateMeasure = insert ? date : null;

                var document = new BsonDocument {
                    {"id", userInfo.TelegramId },
                    {"name", userInfo.FullName },
                    {"LastConversationUpdate", date},
                    {"NumberMessages", 1},
                };
                if (dateMeasure is not null) {
                    document.Add("DateFirstMeasurement", dateMeasure);
                    document.Add("DateLastMeasurement", dateMeasure);
                }
                User?.InsertOne(document);
            }
        }

        internal static void InsertNewMex(IMongoCollection<BsonDocument>? chat, DateTime date, User? from, Intent i, string mex) {

            var documentMex = new BsonDocument {
                {"id", from?.Id},
                {"messages", mex},
                {"type", i.ToString()},
                {"Date", date},
            };
            chat?.InsertOne(documentMex);
        }
    }
}
