using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HyperTensionBot.Server.Bot.Extensions {
    public static class TelegramBotClientExtensions {
        public static WebApplicationBuilder ConfigureTelegramBot(this WebApplicationBuilder builder) {
            var confBot = builder.Configuration.GetSection("Bot");
            var botToken = confBot["TelegramToken"] ?? throw new ArgumentException("Configuration element Bot:TelegramToken is not set");

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
