using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using Microsoft.ML.Transforms.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();

builder.Services.AddSingleton<Memory>();

// add model and llm 
builder.Services.AddSingleton(new ClassificationModel(builder));
builder.Services.AddSingleton(new LLMService(builder));

bool internalPOST = false; // flag: exclude some POST request from LLM server 
var app = builder.Build();

// configuring the bot and timer to alert patients 
app.SetupTelegramBot();
app.Services.GetRequiredService<LLMService>().SetLogger(app.Services.GetRequiredService<ILogger<LLMService>>());

TimerAdvice timer = new(app.Services.GetRequiredService<Memory>(), app.Services.GetRequiredService<TelegramBotClient>());

// handle update 
app.MapPost("/webhook", async (HttpContext context, TelegramBotClient bot, Memory memory, ILogger<Program> logger, ClassificationModel model, LLMService llm) => {
    if (!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }
    if (internalPOST)
        return Results.Ok();

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
    internalPOST = true; // possible POST calls for request to the LLM server 
    if (update.Message?.Text is not null) {
        var messageText = update.Message?.Text;
        if (messageText != null) {
            // add message to model input and predict intent
            var input = new ModelInput { Sentence = messageText };
            var result = model.Predict(input);

            logger.LogInformation("Incoming message matches intent {0}", result);

            // manage operations
            Stopwatch stopwatch = Stopwatch.StartNew();
            await Context.ControlFlow(bot, llm, memory, result, messageText, chat, update.Message!.Date.ToLocalTime());
            stopwatch.Stop();
            logger.LogInformation($"Tempo di elaborazione impiegato: {stopwatch.ElapsedMilliseconds / 1000} s");
        }  
    }
    else if (update.CallbackQuery?.Data != null && update.CallbackQuery?.Message?.Chat != null) {
        await Context.ValuteMeasurement(update.CallbackQuery.Data, update.CallbackQuery.From, update.CallbackQuery.Message.Chat, bot, memory);
        // removing inline keybord
        await bot.EditMessageReplyMarkupAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId, replyMarkup: null); 
    } else
        return Results.NotFound();

    internalPOST = false; // after request, reset flag

    return Results.Ok();
});

app.Run();
