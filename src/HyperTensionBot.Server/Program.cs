using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();
builder.Services.AddSingleton<Memory>();

// add model and GPT 
builder.Services.AddSingleton(new ClassificationModel(builder));
builder.Services.AddSingleton(new GPTService(builder));

var app = builder.Build();

// configuring the bot and timer to alert patients 
app.SetupTelegramBot();

TimerAdvice timer = new(app.Services.GetRequiredService<Memory>(), app.Services.GetRequiredService<TelegramBotClient>());

// handle update 
app.MapPost("/webhook", async (HttpContext context, TelegramBotClient bot, Memory memory, ILogger<Program> logger, ClassificationModel model, GPTService gpt) => {
    if (!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }

    using var sr = new StreamReader(context.Request.Body);
    var update = JsonConvert.DeserializeObject<Update>(await sr.ReadToEndAsync()) ?? throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    User? from = update.Message?.From ?? update.CallbackQuery?.From;
    Chat chat = update.Message?.Chat ?? update.CallbackQuery?.Message?.Chat ?? throw new Exception("Unable to detect chat ID");
    memory.HandleUpdate(from, chat);

    logger.LogInformation("Chat {0} incoming {1}", chat.Id, update.Type switch {
        UpdateType.Message => $"message with text: {update.Message?.Text}",
        UpdateType.CallbackQuery => $"callback with data: {update.CallbackQuery?.Data}",
        _ => "update of unhandled type"
    });
    if (update.Message?.Text is not null) {
        var messageText = update.Message?.Text;
        if (messageText != null) {
            // add message to model input and predict intent
            var input = new ModelInput { Sentence = messageText };
            var result = model.Predict(input);

            logger.LogInformation("Incoming message matches intent {0}", result);

            // manage operations
            await Context.ControlFlow(bot, gpt, memory, result, messageText, chat, update.Message!.Date.ToLocalTime());
        }
        
    }
    else if (update.CallbackQuery?.Data != null && update.CallbackQuery?.Message?.Chat != null) {
        await Context.ValuteMeasurement(update.CallbackQuery.Data, update.CallbackQuery.From, update.CallbackQuery.Message.Chat, bot, memory);
        } else return Results.NotFound();

    return Results.Ok();
});

app.Run();
