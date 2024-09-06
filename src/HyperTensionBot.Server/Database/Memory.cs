using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using OpenAI_API.Chat;
using System.Collections.Concurrent;
using Telegram.Bot.Types;
using static HyperTensionBot.Server.Bot.ConversationInformation;

namespace HyperTensionBot.Server.Database {

    // Service memory for chatbot. It's defined DB and their collections 
    public class Memory {

        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IMongoCollection<BsonDocument>? User { get; set; }

        public IMongoCollection<BsonDocument>? Misuration { get; set; }
        public IMongoCollection<BsonDocument>? Chat { get; set; }

        // to speed access 
        private readonly ConcurrentDictionary<long, ConversationInformation> _chatMemory = new();

        private readonly ILogger<Memory> _logger;

        public Memory(
            ILogger<Memory> logger, ConfigurationManager conf
        ) {
            _logger = logger;
            Client = new MongoClient(GetStringConnection(conf));

            // create Scheme for DB
            Database = Client.GetDatabase("HyperTension");

            CreateScheme(Database);
        }

        // Find the url connection for DB
        private string GetStringConnection(ConfigurationManager conf) {
            var section = conf.GetSection("MongoDB");
            if (!section.Exists() || section["connection"] is null)
                throw new ArgumentException("Connection DB: string connection MongoDB is not set");
            return section["connection"]!;

        }

        // Create the scheme if it not defined, else acquire it. So equals with index 
        private void CreateScheme(IMongoDatabase db) {

            // information as idTelegram, name, surname, last update 
            User = db.GetCollection<BsonDocument>("User");

            // all misuration of pressure and frequence 
            Misuration = db.GetCollection<BsonDocument>("Misuration");

            // save all mexages in the chat
            Chat = db.GetCollection<BsonDocument>("Chat");

            AddIndexToCollection();

        }

        // allow for greater search speed
        private void AddIndexToCollection() {
            // create index and option of that. 
            var index = new CreateIndexOptions();
            var option = Builders<BsonDocument>.IndexKeys.Ascending("id");

            // Add indexes to all my collections 
            User?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
            Misuration?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
            Chat?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
        }

        // Define standard filter for research on id Telegram
        public static FilterDefinition<BsonDocument> GetFilter(long id) {
            return Builders<BsonDocument>.Filter.Eq("id", id);
        }

        // save new information for user 
        public void HandleUpdate(User? from, DateTime date, Intent i, string mex) {

            // update info of User as name,..., Time and number of messages 
            if (from != null) {
                Update.UpdateUser(User, date, i, from, mex, false);
                _logger.LogTrace("Updated user memory");
            }

            Update.InsertNewMex(Chat, date, from!.Id, i.ToString(), mex);
        }

        // use Ram and not Db for measurements before confirm
        public void SetTemporaryMeasurement(Chat chat, Measurement measurement, DateTime date) {
            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id, date) { TemporaryMeasurement = measurement }, (_, existing) => {
                existing.TemporaryMeasurement = measurement;
                return existing;
            });
            _logger.LogTrace("Stored temporary measurement for chat {0}", chat.Id);
        }

        // get all patients
        public List<BsonDocument> GetAllPatients() {
            return User.FindAsync(new BsonDocument()).Result.ToList();
        }

        // saved measurement in DB 
        public void PersistMeasurement(long id) {
            if (!_chatMemory.TryGetValue(id, out var chatInformation)) {
                throw new Exception($"Tried persisting measurement but no information available about chat {id}");
            }
            if (chatInformation.TemporaryMeasurement == null) {
                throw new Exception($"Tried persisting measurement but no temporary measurement was recorded for chat {id}");
            }

            var newMeasurement = chatInformation.TemporaryMeasurement;
            ManageMeasurement.InsertMeasurement(Misuration, id, newMeasurement);

            _chatMemory.AddOrUpdate(id, new ConversationInformation(id), (_, existing) => {
                existing.TemporaryMeasurement = null;
                return existing;
            });
        }

        // getter personal information and measurements
        public List<string> GetGeneralInfo(long id) {
            // get all messages with type = personal messages 
            var messages = ManageChat.GetMessages(id, Chat, Intent.PersonalInfo.ToString());
            return messages!.Select(x => x["messages"].AsString).ToList();
        }

        public List<Measurement> GetAllMeasurements(long id) {

            var bsonMeasurements = ManageMeasurement.GetAllDocuments(Misuration, id);
            List<Measurement> measurements = new();
            foreach (var measure in bsonMeasurements) {
                measurements.Add(new Measurement((double?)measure["Systolic"], (double?)measure["Diastolic"],
                    (double?)measure["HeartRate"], Time.Convert((DateTime)measure["Date"])));
            }
            return measurements;

        }

        // insert message into collection and return all messages for that user
        public List<ChatMessage> AddMessageLLM(Chat chat) {

            // get all messages with
            var messages = ManageChat.GetMessages(chat.Id, Chat);
            var chatToLLM = Prompt.GeneralContext();

            // select last 6 message for specific id chat. Not last. 
            if (messages.Count >= 5) {
                var selection = messages.GetRange(messages.Count - 6, 5);
                foreach (var mex in selection) {
                    if (mex["type"] == "Risposta")
                        chatToLLM.Add(new ChatMessage(ChatMessageRole.Assistant, mex["messages"].ToString()));
                    else
                        chatToLLM.Add(new ChatMessage(ChatMessageRole.User, mex["messages"].ToString()));
                }
            }
            return chatToLLM;
        }

        internal DateTime? GetFirstMeasurement(long id) {
            var document = User.FindAsync(GetFilter(id)).Result.FirstOrDefault();
            if (document is not null) {
                return Time.Convert((DateTime)document["DateFirstMeasurement"]);
            }
            else { throw new ArgumentNullException(); }
        }

        public bool IsPressureLastMeasurement(long id) {

            return ManageMeasurement.LastMeasurement(Misuration, User, id).DiastolicPressure.HasValue;
        }
        internal void SetTemporaryParametersRequest(long id, string[] parameters) {
            _chatMemory.AddOrUpdate(id, new ConversationInformation(id) { Requestparameters = parameters }, (_, existing) => {
                existing.Requestparameters = parameters;
                return existing;
            });
        }

        internal string[] GetParameters(long id) {
            if (!_chatMemory.TryGetValue(id, out var chatInformation)) {
                throw new Exception($"Tried parameters but no information available about chat {id}");
            }
            if (chatInformation.Requestparameters == null) {
                throw new Exception($"There are not parameters for chat {id}");
            }
            return chatInformation.Requestparameters!;
        }

        public RequestState GetRequestState(long id) {
            if (!_chatMemory.TryGetValue(id, out var chatInformation)) {
                throw new Exception($"Tried state but no information available about chat {id}");
            }
            if (chatInformation.Requestparameters == null) {
                throw new Exception($"There are not request state for chat {id}");
            }
            return chatInformation.State;
        }

        public void SetRequestState(long id, RequestState state) {
            if (!_chatMemory.TryGetValue(id, out var chatInformation)) {
                throw new Exception($"Tried state but no information available about chat {id}");
            }
            chatInformation.State = state;
        }
    }
}
