/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


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
