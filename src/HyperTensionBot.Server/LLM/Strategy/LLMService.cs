using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM.Strategy {
    // Allow use of gpt or ollama template used the same interface
    public class LLMService {

        private readonly ILLMService _llm;

        public LLMService(ILLMService llm) {
            _llm = llm;
        }

        public async Task<string> HandleAskAsync(TypeConversation t, string message, List<ChatMessage>? comunicationChat = null, List<ChatMessage>? context = null) {
            return await _llm.AskLLM(t, message, comunicationChat, context);
        }

        public void SetLogger(ILogger<LLMService> logger) {
            _llm.SetLogger(logger);
        }
    }
}
