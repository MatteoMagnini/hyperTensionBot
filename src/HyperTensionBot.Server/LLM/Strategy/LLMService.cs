using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM.Strategy {
    public class LLMService {

        private readonly ILLMService _llm;

        public LLMService(ILLMService llm) {
            _llm = llm;
        }

        public async Task<string> HandleAskAsync(TypeConversation t, string message, List<ChatMessage>? comunicationChat = null) {
            return await _llm.AskLLM(t, message, comunicationChat);
        }

        public void SetLogger(ILogger<LLMService> logger) {
            _llm.SetLogger(logger);
        }
    }
}
