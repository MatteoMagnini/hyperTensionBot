using MongoDB.Bson;
using MongoDB.Driver;
using OpenAI_API.Chat;

namespace HyperTensionBot.Server.Database {
    internal static class ManageChat {

        // find all messages that match with specific id and type messages (insertion, request, ecc..)
        internal static List<BsonDocument> GetMessages(long id, IMongoCollection<BsonDocument>? chat, string type = "") {
            FilterDefinition<BsonDocument> filter;
            if (type == "Generale") {
                filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("id", id),
                    Builders<BsonDocument>.Filter.Eq("type", type)
                );
            }
            else
                filter = Memory.GetFilter(id);
            return chat.FindAsync(filter).Result.ToList();
        }

        internal static List<ChatMessage> GetContext(long id, IMongoCollection<BsonDocument>? chat, string mex) {
            List<ChatMessage> context = new();
            var contT = GetMessages(id, chat);
            if (contT.Count > 2) {
                context.AddRange(new List<ChatMessage> {
                    new ChatMessage(ChatMessageRole.User, contT[contT.Count-3]["messages"].ToString()),
                    new ChatMessage(ChatMessageRole.User, contT[contT.Count-2]["messages"].ToString()),
                    new ChatMessage(ChatMessageRole.User, mex),
                });
            }

            return context;
        }
    }
}
