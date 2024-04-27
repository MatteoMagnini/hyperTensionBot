using HyperTensionBot.Server.Bot;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HyperTensionBot.Server.Database {

    // manage pressure and frequence's datas for the database
    internal static class ManageMeasurement {

        internal static List<BsonDocument> GetAllDocuments(IMongoCollection<BsonDocument>? measurements, long id) {
            return measurements.FindAsync(Memory.GetFilter(id)).Result.ToList();
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

        public static Measurement LastMeasurement(IMongoCollection<BsonDocument>? measurements, IMongoCollection<BsonDocument>? user, long id) {

            var date = DateLastMeasurement(user, id); 
            var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("id", id),
                    Builders<BsonDocument>.Filter.Eq("Date", date)
                );

            var measure = measurements.FindAsync(filter).Result.FirstOrDefault(); 
            return new Measurement((double?)measure["Systolic"], (double?)measure["Diastolic"],
                    (double?)measure["HeartRate"], (DateTime)measure["Date"]); 
        }

        private static DateTime? DateLastMeasurement(IMongoCollection<BsonDocument>? user, long id) {
            var doc = user.FindAsync(Memory.GetFilter(id)).Result.FirstOrDefault();
            return (DateTime)doc["DateLastMeasurement"]; 
        }
    }
}
