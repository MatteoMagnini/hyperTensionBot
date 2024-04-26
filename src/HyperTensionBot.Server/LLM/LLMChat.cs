namespace HyperTensionBot.Server.LLM {
    public class LLMChat {

        public LLMChat(string role, string content) {
            Role = role;
            Content = content;
        }
        public string Role { get; set; }

        public string Content { get; set; }
    }
}
