using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Database {

    // Manage database at new messages. 
    internal static class Update {

        internal static void UpdateUser(IMongoCollection<BsonDocument>? User, Intent i, User from, string mex) {
            var user = User.FindAsync(Memory.GetFilter(from.Id)).Result.First();
            bool insert = i.ToString().StartsWith("inser"); 
            if (user.Any()) {
                var update = Builders<BsonDocument>.Update.Set("LastConversationUpdate", DateTime.UtcNow.ToString())
                                                          .Inc("NumberMessages", 1);
                if (insert) {

                    var doc = User.FindAsync(Memory.GetFilter(from.Id)).Result.First(); 
                    if (!doc["DateFirstMeasurement"].IsValidDateTime) {
                        update.Set("DateFirstMeasurement", DateTime.UtcNow);
                    }
                    update.Set("DateLastMeasurement", DateTime.UtcNow);
                }
                User?.UpdateMany(Memory.GetFilter(from.Id), update);
            }
            var userInfo = new UserInformation(from);

            dynamic? date = insert? DateTime.UtcNow: null;

            var document = new BsonDocument {
                {"id", userInfo.TelegramId },
                {"name", userInfo.FirstName },
                {"surname", userInfo.LastName},
                {"LastConversationUpdate", DateTime.UtcNow},
                {"NumberMessages", 1},
                {"DateFirstMeasurement", date},
                {"DateLastMeasurement", date}
            };
            User?.InsertOne(document);
        }

        internal static void InsertNewMex(IMongoCollection<BsonDocument>? chat, User? from, Intent i, string mex) {

            var documentMex = new BsonDocument {
                {"id", from?.Id},
                {"messages", mex},
                {"type", i.ToString()},
                {"Date", DateTime.Now},
            };
            chat?.InsertOne(documentMex);
        }
    }
}
