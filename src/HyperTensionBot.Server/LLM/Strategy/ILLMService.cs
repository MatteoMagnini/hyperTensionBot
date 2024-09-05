using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM.Strategy {
    public interface ILLMService {

        Task<string> AskLLM(TypeConversation t, List<ChatMessage>? comunicationChat = null, List<ChatMessage>? context = null);

        void SetLogger(ILogger<LLMService> logger);

    }
}
