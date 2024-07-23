using MongoDB.Bson;
using MongoDB.Driver;

namespace HyperTensionBot.Server.Database {
    internal static class ManageChat {

        // find all messages that match with specific id and type messages (insertion, request, ecc..)
        internal static List<BsonDocument> GetMessages(long id, IMongoCollection<BsonDocument>? chat, string type) {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("id", id),
                Builders<BsonDocument>.Filter.Eq("type", type)
            );
            return chat.FindAsync(filter).Result.ToList();
        }
    }
}
