using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which listens for text file attachments to auto-paste.
/// </summary>
internal sealed class FileAttachmentListeningService : BackgroundService
{
    private readonly DiscordClient _discordClient;
    private readonly MessagePastingService _messagePastingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileAttachmentListeningService" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="messagePastingService">The message pasting service.</param>
    public FileAttachmentListeningService(DiscordClient discordClient, MessagePastingService messagePastingService)
    {
        _discordClient = discordClient;
        _messagePastingService = messagePastingService;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.MessageCreated += DiscordClientOnMessageCreated;
        return Task.CompletedTask;
    }

    private Task DiscordClientOnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        if (_messagePastingService.IsChannelExempt(e.Channel))
        {
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(e.Message.Content))
        {
            return Task.CompletedTask;
        }

        if (!_messagePastingService.QualifiesForPasting(e.Message))
        {
            return Task.CompletedTask;
        }

        return _messagePastingService.PasteMessageAttachmentsAsync(e.Message);
    }
}
