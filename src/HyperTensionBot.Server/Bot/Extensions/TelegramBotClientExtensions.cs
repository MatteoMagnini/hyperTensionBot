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


using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HyperTensionBot.Server.Bot.Extensions {
    // Configure Telegram bot
    public static class TelegramBotClientExtensions {
        public static WebApplicationBuilder ConfigureTelegramBot(this WebApplicationBuilder builder) {

            var confBot = builder.Configuration.GetSection("Bot");
            var botToken = confBot["TelegramToken"] ?? throw new ArgumentException("Configuration element Bot:TelegramToken is not set");

            //var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new ArgumentException("Configuration element Bot:TelegramToken is not set");
            builder.Services.AddTransient(provider => new TelegramBotClient(botToken));

            return builder;
        }

        public static WebApplication SetupTelegramBot(this WebApplication app) {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var confBot = app.Configuration.GetSection("Bot");
            var botWebhookUrl = confBot["WebhookUrlBase"];
            if (botWebhookUrl == null) {
                logger.LogInformation("Configuration element Bot:WebhookUrlBase not set, not setting webhook");
            }
            else {
                var client = app.Services.GetRequiredService<TelegramBotClient>();
                var finalUrl = botWebhookUrl + "/webhook";
                logger.LogInformation("Setting Telegram bot webhook to URL {0}", finalUrl);

                client.SetWebhookAsync(finalUrl, allowedUpdates: new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery }).Wait();
            }

            return app;
        }
    }
}
