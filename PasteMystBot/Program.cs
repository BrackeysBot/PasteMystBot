using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PasteMystBot.Services;
using PasteMystNet;
using X10D.Hosting.DependencyInjection;

Directory.CreateDirectory("data");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("data/config.json", true, true);

builder.Logging.ClearProviders();
builder.Logging.AddNLog();

builder.Services.AddSingleton(new PasteMystClient());

builder.Services.AddSingleton(new PasteMystClient());
builder.Services.AddSingleton(new DiscordClient(new DiscordConfiguration
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
    LoggerFactory = new NLogLoggerFactory(),
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents
}));

builder.Services.AddHostedSingleton<LoggingService>();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<CodeblockDetectionService>();
builder.Services.AddSingleton<MessagePastingService>();
builder.Services.AddSingleton<PasteMystService>();

builder.Services.AddHostedSingleton<FileAttachmentListeningService>();
builder.Services.AddHostedSingleton<MessageListeningService>();

builder.Services.AddHostedSingleton<BotService>();

IHost app = builder.Build();
await app.RunAsync();
