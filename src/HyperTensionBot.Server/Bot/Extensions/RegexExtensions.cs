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


using System.Text.RegularExpressions;

namespace HyperTensionBot.Server.Bot.Extensions {
    public static class RegexExtensions {
        public static int GetIntMatch(this Match match, string groupName) {
            return match.GetOptionalIntMatch(groupName) ?? throw new ArgumentException($"Group {groupName} not matched or not convertible to integer");
        }

        public static int? GetOptionalIntMatch(this Match match, string groupName) {
            var g = match.Groups[groupName] ?? throw new ArgumentException($"Group {groupName} not found");
            if (!g.Success) {
                return null;
            }

            if (!int.TryParse(g.ValueSpan, out var result)) {
                return null;
            }

            return result;
        }

        // When the ML model predict a new insertion, the text passed to LLM for a first check and this method use the regex to extract
        public static double?[] ExtractMeasurement(string message) {
            var match = Regex.Match(message, @"(?<v1>\d{2,3})(?:\D+(?<v2>\d{2,3}))?(?:\D+(?<v3>\d{2,3}))?");

            if (!match.Success) {
                throw new ArgumentException("Il messaggio non contiene numeri decimali.");
            }
            double? frequence, sistolyc, diastolic;
            if (string.IsNullOrEmpty(match.Groups["v2"].Value)) {
                frequence = double.Parse(match.Groups["v1"].Value);
                sistolyc = diastolic = null;
            }
            else {
                sistolyc = (!string.IsNullOrEmpty(match.Groups["v1"].Value)) ? Math.MaxMagnitude(double.Parse(match.Groups["v1"].Value), double.Parse(match.Groups["v2"].Value)) : null;
                diastolic = (!string.IsNullOrEmpty(match.Groups["v1"].Value)) ? Math.MinMagnitude(double.Parse(match.Groups["v1"].Value), double.Parse(match.Groups["v2"].Value)) : null;
                frequence = (!string.IsNullOrEmpty(match.Groups["v3"].Value)) ? double.Parse(match.Groups["v3"].Value) : null;
            }

            return new[] { sistolyc, diastolic, frequence };
        }

        public static string[] ExtractParameters(string outLLM, string message) {
            var match = Regex.Match(outLLM, @"(?<v1>[A-Z]{2,})[\s\S]*?(?<v2>\d+)[\s\S]*?(?<v3>[A-Z]{2,})");

            if (!match.Success) {
                throw new ArgumentException("L'output non contiene tre parametri.");
            }
            string v1 = Regex.Replace(match.Groups["v1"].Value, "[^A-Z]+", "");
            string v2 = match.Groups["v2"].Value;
            string v3 = Regex.Replace(match.Groups["v3"].Value, "[^A-Z]+", "");

            if (v1 == "ENTRAMBI") {
                if (message.Contains("pressione") && !message.Contains("frequenza"))
                    v1 = "PRESSIONE";
                else if (!message.Contains("pressione") && message.Contains("frequenza"))
                    v1 = "FREQUENZA";
            }

            return new[] { v1, v2, v3 };
        }
    }
}
