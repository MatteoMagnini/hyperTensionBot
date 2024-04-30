using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM.Strategy {
    public interface ILLMService {

        Task<string> AskLLM(TypeConversation t, string message, List<ChatMessage>? comunicationChat = null);

        void SetLogger(ILogger<LLMService> logger);

    }
}
