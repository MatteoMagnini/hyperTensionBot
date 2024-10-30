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


namespace HyperTensionBot.Server.Bot.Extensions {
    public static class Time {

        // conversion UTC time to local time
        // Default ita, it can be changed to any time zone
        public static DateTime Convert(DateTime date, string timeZoneId = "Europe/Rome") {

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(date, timeZone);
        }
    }
}
