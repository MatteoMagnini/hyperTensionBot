using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using HyperTensionBot.Server.ModelML;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramUpdate = Telegram.Bot.Types.Update;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();

builder.Services.AddSingleton<ConfigurationManager>(builder.Configuration);
builder.Services.AddSingleton<Memory>();

// add model and llm
builder.Services.AddSingleton(new ClassificationModel());

// Change the strategy - LLM - with class Ollama or gpt
builder.Services.AddSingleton(new LLMService(await OllamaService.CreateAsync(builder)));

var app = builder.Build();

// Configure the bot and timer to alert patients
app.SetupTelegramBot();
app.Services.GetRequiredService<LLMService>().SetLogger(app.Services.GetRequiredService<ILogger<LLMService>>());

// Create a Telegram bot client
var botClient = app.Services.GetRequiredService<TelegramBotClient>();

await botClient.DeleteWebhookAsync();

// Configure Timer Advice
TimerAdvice timer = new(app.Services.GetRequiredService<Memory>(), botClient, app.Services.GetRequiredService<LLMService>());


// Configure the receiver options for polling
var receiverOptions = new ReceiverOptions {
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

var cts = new CancellationTokenSource();

// Start receiving updates using polling
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("Bot is listening for updates..."));
app.Lifetime.ApplicationStopping.Register(() => cts.Cancel());

// Ensure the application doesn't exit immediately
await app.RunAsync();

async Task HandleUpdateAsync(ITelegramBotClient botClient, TelegramUpdate update, CancellationToken cancellationToken) {
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var memory = app.Services.GetRequiredService<Memory>();
    var model = app.Services.GetRequiredService<ClassificationModel>();
    var llm = app.Services.GetRequiredService<LLMService>();
    TelegramBotClient localBotClient = (TelegramBotClient)botClient;

    try {
        if (update.Type == UpdateType.Message && update.Message?.Text != null) {
            var messageText = update.Message.Text;
            var chatId = update.Message.Chat.Id;
            var from = update.Message.From;
            var date = Time.Convert(update.Message.Date);

            // Add message to model input and predict intent
            if (messageText == "/start")
                await SendMessagesExtension.SendStartMessage(localBotClient, chatId);
            else {
                ModelInput input = new() { Sentence = messageText };
                var result = model.Predict(input);

                memory.HandleUpdate(from, date, result, messageText);
                logger.LogInformation("Chat {0} incoming {1}", chatId, update.Type switch {
                    UpdateType.Message => $"message with text: {messageText}",
                    UpdateType.CallbackQuery => $"callback with data: {update.CallbackQuery?.Data}",
                    _ => "update of unhandled type"
                });
                logger.LogInformation("Incoming message matches intent {0}", result);

                // Manage operations
                Stopwatch stopwatch = Stopwatch.StartNew();
                await Context.ControlFlow(localBotClient, llm, memory, result, messageText, update.Message.Chat, date);
                stopwatch.Stop();
                logger.LogInformation($"Tempo di elaborazione impiegato: {stopwatch.ElapsedMilliseconds / 1000} s");
            }
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data != null) {
            var chatId = update.CallbackQuery.Message!.Chat.Id;
            var from = update.CallbackQuery.From;

            // buttons for requests and insertions 
            if (update.CallbackQuery.Data.StartsWith("yes") || update.CallbackQuery.Data.StartsWith("no")) {
                await Context.ManageButton(update.CallbackQuery.Data, update.CallbackQuery.From, update.CallbackQuery.Message.Chat, localBotClient, memory, llm);
            }
            else if (update.CallbackQuery.Data == "sil" || update.CallbackQuery.Data == "adv") {
                // buttons for advice
                timer.DeactivateNotify(update.CallbackQuery.Message.Date, from, update.CallbackQuery.Data);
            }
            else {
                await Request.ModifyParameters(localBotClient, chatId, memory, update.CallbackQuery.Data, update.CallbackQuery.Message.MessageId, llm);
            }
            // Removing inline keyboard
            await localBotClient.DeleteMessageAsync(chatId, update.CallbackQuery.Message.MessageId);
        }
        else
            return;
    }
    catch (Exception e) {
        logger.LogError(e, "Error handling update");
    }
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(exception, "Error occurred");
    return Task.CompletedTask;
}
