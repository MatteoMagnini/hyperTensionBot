using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenAI_API.Chat;
using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Update = HyperTensionBot.Server.Database.Update;

namespace HyperTensionBot.Server.Services {
    public class Memory {

        // prendere tutti gli UserMemory metodi Get e toglierli dal codice formando invece delle query di interazione
        // con il database in questa sezione apposita. -- Quindi ripescare tutto e comprendere quali sono le query che servono a seconda di...
        // public ConcurrentDictionary<long, UserInformation> UserMemory { get; } = new();

        public MongoClient Client { get; }
        public IMongoDatabase Database { get; }
        public IMongoCollection<BsonDocument>? User { get; set; }

        public IMongoCollection<BsonDocument>? Misuration { get; set; }
        public IMongoCollection<BsonDocument>? Chat { get; set; }

        // to speed access 
        private readonly ConcurrentDictionary<long, ConversationInformation> _chatMemory = new();

        private readonly ILogger<Memory> _logger; 

        public Memory(
            ILogger<Memory> logger, WebApplicationBuilder builder
        ) {
            _logger = logger;
            Client = new MongoClient(GetStringConnection(builder));

            // create Scheme for DB
            Database = Client.GetDatabase("HyperTension");

            CreateScheme(Database); 
        }

        private string GetStringConnection(WebApplicationBuilder builder) {
            var section = builder.Configuration.GetSection("MongoDB");
            if (!section.Exists() || section["connection"] is null)
                throw new ArgumentException("Connection DB: string connection MongoDB is not set");
            return section["connection"]!;

        }

        private void CreateScheme(IMongoDatabase db) {

            // information as idTelegram, name, surname, last update 
            User = db.GetCollection<BsonDocument>("User");

            // all misuration of pressure and frequence 
            Misuration = db.GetCollection<BsonDocument>("Misuration");

            // save all mexages in the chat
            Chat = db.GetCollection<BsonDocument>("Chat");

            AddIndexToCollection();

        }

        private void AddIndexToCollection() {
            // create index and option of that. 
            var index = new CreateIndexOptions();
            var option = Builders<BsonDocument>.IndexKeys.Ascending("id");

            // Add indexes to all my collections 
            User?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
            Misuration?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
            Chat?.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(option, index));
        }

        public static FilterDefinition<BsonDocument> GetFilter(long id) {
            return Builders<BsonDocument>.Filter.Eq("id", id); 
        }

        // metodo che deve prendere le informazioni di un nuovo utente e le deve salvare
        // chat information - posso integrare l'unica informazione necessaria che è la data dell'ultimo messaggio. 
        public void HandleUpdate(User? from, Intent i, string mex) {

            // update info of User as name,..., Time and number of messages 
            if (from != null) {
                Update.UpdateUser(User, i, from, mex);
                _logger.LogTrace("Updated user memory");
            }

            Update.InsertNewMex(Chat, from, i, mex); 
        }

        // non interviene database, è qualcosa di temporaneo in Ram 
        public void SetTemporaryMeasurement(Chat chat, Measurement measurement) {
            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id) { TemporaryMeasurement = measurement }, (_, existing) => {
                existing.TemporaryMeasurement = measurement;
                return existing;
            });
            _logger.LogTrace("Stored temporary measurement for chat {0}", chat.Id);
        }

        // get all patients
        public List<BsonDocument> GetAllPatients() {
            return User.FindAsync(new BsonDocument()).Result.ToList();
        }
        // gisce collection delle misurazioni
        public void PersistMeasurement(User from, Chat chat) {
            if(!_chatMemory.TryGetValue(chat.Id, out var chatInformation)) {
                throw new Exception($"Tried persisting measurement but no information available about chat {chat.Id}");
            }
            if(chatInformation.TemporaryMeasurement == null) {
                throw new Exception($"Tried persisting measurement but no temporary measurement was recorded for chat {chat.Id}");
            }

            var newMeasurement = chatInformation.TemporaryMeasurement;
            ManageMeasurement.InsertMeasurement(Misuration, chat.Id, newMeasurement); 

            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id), (_, existing) => {
                existing.TemporaryMeasurement = null;
                return existing;
            });
        }

        // informazioni personali - occorre splittare nei metodi di inserimento in database e una che invece resta di ritorno Lista per la stampa. 
        public List<string> GetGeneralInfo(Chat chat) {
            // get all messages with type = personal messages 
            var messages = ManageChat.GetMessages(chat.Id, Chat, Intent.PersonalInfo.ToString()); 
            return messages!.Select(x => x["messages"].AsString).ToList(); 
        }

        public List<Measurement> GetAllMeasurements(long id) {

            var bsonMeasurements = ManageMeasurement.GetAllDocuments(Misuration, id);
            List<Measurement> measurements = new(); 
            foreach (var measure in bsonMeasurements) {
                measurements.Add(new Measurement((double?)measure["Systolic"], (double?)measure["Diastolic"],
                    (double?)measure["HeartRate"], (DateTime)measure["Date"])); 
            }
            return measurements;

        }

        // inserimento del messaggio in Messaggi collection e restituisco la lista dei messaggi di quell'id Telegram come richiesto 
        public List<LLMChat> AddMessageLLM(Chat chat, string message) {

            // get all messages with type = General 
            var messages = ManageChat.GetMessages(chat.Id, Chat, Intent.Generale.ToString());

            var chatToLLM = Prompt.GeneralContext();
            foreach (var mex in messages) {
                chatToLLM.Add(new LLMChat("user", mex.ToString()));
            }
            return chatToLLM; 
        }

        [Obsolete] // AsDateTime is obsolete method, but work !! :) 
        internal DateTime? GetFirstMeasurement(long id) {
            var document = User.FindAsync(GetFilter(id)).Result.First();
            return document["DateFirstMeasurement"].AsDateTime; 
        }

        public bool IsPressureLastMeasurement(long id) {

            return ManageMeasurement.LastMeasurement(Misuration, User, id).DiastolicPressure.HasValue; 

        }
    }
}
