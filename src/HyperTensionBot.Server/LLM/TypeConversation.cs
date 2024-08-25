namespace HyperTensionBot.Server.LLM {
    // define tyoe conversation. For each intent, the user message is forwarded to the correct chat 
    public enum TypeConversation {
        Request,
        Communication,
        Insert,
        Advice
    }
}
