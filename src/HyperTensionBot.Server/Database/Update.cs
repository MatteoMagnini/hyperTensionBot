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


using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Database {

    // Manage database at new messages.
    public static class Update {

        internal static void UpdateUser(IMongoCollection<BsonDocument>? User, DateTime date, Intent i, User from, string mex, bool adv) {
            var user = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
            bool insert = i.ToString().Equals("Inserimento");
            if (user is not null) {
                var update = Builders<BsonDocument>.Update.Set("LastConversationUpdate", date)
                                                          .Inc("NumberMessages", 1);

                var doc = User.FindAsync(Memory.GetFilter(from.Id)).Result.FirstOrDefault();
                // update date measurement
                if (insert) {
                    if (!doc.Contains("DateFirstMeasurement")) {
                        update = update.Set("DateFirstMeasurement", date);
                    }
                    update = update.Set("DateLastMeasurement", date);
                }
                // update date of notify
                if (adv) {
                    update = update.Set("DateDeactivate", date);
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
                    {"DateDeactivate", DateTime.MinValue}
                };
                if (dateMeasure is not null) {
                    document.Add("DateFirstMeasurement", dateMeasure);
                    document.Add("DateLastMeasurement", dateMeasure);
                }
                User?.InsertOne(document);
            }
        }

        public static void InsertNewMex(IMongoCollection<BsonDocument>? chat, DateTime date, long id, string i, string mex) {

            var documentMex = new BsonDocument {
                {"id", id},
                {"messages", mex},
                {"type", i},
                {"Date", date},
            };
            chat?.InsertOne(documentMex);
        }
    }
}
