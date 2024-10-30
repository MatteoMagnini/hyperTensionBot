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


namespace HyperTensionBot.Server.Bot {
    // Contains information of user during the session
    public class ConversationInformation {
        public ConversationInformation(long telegramChatId, DateTime? date = null, RequestState state = RequestState.ChoiceContext) {
            TelegramChatId = telegramChatId;
            LastConversationUpdate = date;
            State = state;
        }

        public long TelegramChatId { get; init; }

        public DateTime? LastConversationUpdate { get; set; }

        public Measurement? TemporaryMeasurement { get; set; }

        public enum RequestState {
            ChoiceContext, ChoiceTimeSpan, ChoiceFormat
        }

        public RequestState State { get; set; }

        public string[]? Requestparameters { get; set; }

        public override bool Equals(object? obj) {
            if (obj is ConversationInformation conversationInformation) {
                return TelegramChatId == conversationInformation.TelegramChatId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramChatId.GetHashCode();
        }
    }
}
