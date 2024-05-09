using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.Bot.Extensions;
using HyperTensionBot.Server.Database;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.LLM.Strategy;
using HyperTensionBot.Server.ModelML;
using Newtonsoft.Json;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();

builder.Services.AddSingleton<ConfigurationManager>(builder.Configuration);
builder.Services.AddSingleton<Memory>();

// add model and llm 
builder.Services.AddSingleton(new ClassificationModel());

// change the strategy - LLM - with class Ollama o gpt
builder.Services.AddSingleton(new LLMService(await OllamaService.CreateAsync(builder))); // new GPTService(builder))

bool internalPOST = false; // flag: exclude some POST request from LLM server 
var app = builder.Build();

// configuring the bot and timer to alert patients 
app.SetupTelegramBot();
app.Services.GetRequiredService<LLMService>().SetLogger(app.Services.GetRequiredService<ILogger<LLMService>>());

TimerAdvice timer = new(app.Services.GetRequiredService<Memory>(), app.Services.GetRequiredService<TelegramBotClient>());

// handle update 
app.MapPost("/webhook", async (HttpContext context, TelegramBotClient bot, Memory memory, ILogger<Program> logger, ClassificationModel model, LLMService llm) => {
    try {
        if (!context.Request.HasJsonContentType()) {
            throw new BadHttpRequestException("HTTP request must be of type application/json");
        }
        if (internalPOST)
            return Results.Ok();

        using var sr = new StreamReader(context.Request.Body);
        var update = JsonConvert.DeserializeObject<Telegram.Bot.Types.Update>(await sr.ReadToEndAsync()) ?? throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
        logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

        User? from = update.Message?.From ?? update.CallbackQuery?.From;
        Chat chat = update.Message?.Chat ?? update.CallbackQuery?.Message?.Chat ?? throw new Exception("Unable to detect chat ID");

        internalPOST = true; // possible POST calls for request to the LLM server 
        if (update.Message?.Text is not null) {
            var messageText = update.Message?.Text;
            if (messageText != null) {
                var date = Time.Convert(update!.Message!.Date);
                // add message to model input and predict intent
                var input = new ModelInput { Sentence = messageText };
                var result = model.Predict(input);

                memory.HandleUpdate(from, date, result, messageText);
                logger.LogInformation("Chat {0} incoming {1}", chat.Id, update.Type switch {
                    UpdateType.Message => $"message with text: {update.Message?.Text}",
                    UpdateType.CallbackQuery => $"callback with data: {update.CallbackQuery?.Data}",
                    _ => "update of unhandled type"
                });
                logger.LogInformation("Incoming message matches intent {0}", result);

                // manage operations
                Stopwatch stopwatch = Stopwatch.StartNew();
                await Context.ControlFlow(bot, llm, memory, result, messageText, chat, date);
                stopwatch.Stop();
                logger.LogInformation($"Tempo di elaborazione impiegato: {stopwatch.ElapsedMilliseconds / 1000} s");
            }
        }
        else if (update.CallbackQuery?.Data != null && update.CallbackQuery?.Message?.Chat != null) {
            await Context.ManageButton(update.CallbackQuery.Data, update.CallbackQuery.From, update.CallbackQuery.Message.Chat, bot, memory, llm);
            if (!update.CallbackQuery.Data.StartsWith("yes") && !update.CallbackQuery.Data.StartsWith("no"))
                await Request.ModifyParameters(bot, chat.Id, memory, update.CallbackQuery.Data, update.CallbackQuery.Message.MessageId, llm);
            // removing inline keybord
            await bot.DeleteMessageAsync(chat.Id, update.CallbackQuery.Message.MessageId);
        }
        else
            return Results.NotFound();

        internalPOST = false; // after request, reset flag
    }
    catch (Exception e) {
        logger.LogDebug(e.Message);
        return Results.Ok(); // system always online 
    }
    return Results.Ok();
});

app.Run();
