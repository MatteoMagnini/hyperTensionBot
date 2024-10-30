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


using OpenAI_API.Chat;

namespace HyperTensionBot.Server.LLM.Strategy {
    // Allow use of gpt or ollama template used the same interface
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
