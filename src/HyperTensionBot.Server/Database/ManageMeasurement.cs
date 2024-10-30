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
using HyperTensionBot.Server.Bot.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HyperTensionBot.Server.Database {

    // manage pressure and frequence's datas for the database
    internal static class ManageMeasurement {

        internal static List<BsonDocument> GetAllDocuments(IMongoCollection<BsonDocument>? measurements, long id) {
            return measurements.Find(Memory.GetFilter(id)).SortBy(doc => doc["Date"]).ToList();
        }

        internal static void InsertMeasurement(IMongoCollection<BsonDocument>? measurements, long id, Measurement newMeasurement) {
            // Add persistent measurement
            var document = new BsonDocument {
                {"id", id},
                {"HeartRate", newMeasurement.HeartRate},
                {"Systolic", newMeasurement.SystolicPressure},
                {"Diastolic", newMeasurement.DiastolicPressure},
                {"Date", newMeasurement.Date},
            };

            measurements?.InsertOne(document);
        }

        // Take last measurement
        public static Measurement LastMeasurement(IMongoCollection<BsonDocument>? measurements, IMongoCollection<BsonDocument>? user, long id) {

            var date = DateLastMeasurement(user, id);
            var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("id", id),
                    Builders<BsonDocument>.Filter.Eq("Date", date)
                );

            var measure = measurements.FindAsync(filter).Result.FirstOrDefault();
            return new Measurement((double?)measure["Systolic"], (double?)measure["Diastolic"],
                    (double?)measure["HeartRate"], Time.Convert((DateTime)measure["Date"]));
        }

        // take last data measurement
        private static BsonValue? DateLastMeasurement(IMongoCollection<BsonDocument>? user, long id) {
            var doc = user.FindAsync(Memory.GetFilter(id)).Result.FirstOrDefault();
            var date = doc["DateLastMeasurement"];
            if (!date.IsBsonNull)
                return date;
            else
                return null;
        }
    }
}
