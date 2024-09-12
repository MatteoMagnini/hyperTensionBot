using MongoDB.Bson;
using MongoDB.Driver;

namespace HyperTensionBot.Server.Database {
    public static class ManageChat {

        // find all messages that match with specific id and type messages (insertion, request, ecc..)
        public static List<BsonDocument> GetMessages(long id, IMongoCollection<BsonDocument>? chat, string type = "") {
            FilterDefinition<BsonDocument> filter;
            if (type != "") {
                filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("id", id),
                    Builders<BsonDocument>.Filter.Eq("type", type)
                );
            }
            else
                filter = Memory.GetFilter(id);
            return chat.FindAsync(filter).Result.ToList();
        }
    }
}
