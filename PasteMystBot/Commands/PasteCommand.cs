using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using PasteMystBot.Services;

namespace PasteMystBot.Commands;

/// <summary>
///     Represents a class which implements the <c>paste</c> command and the <c>Upload Paste</c> context menu.
/// </summary>
internal sealed class PasteCommand : ApplicationCommandModule
{
    private readonly MessagePastingService _messagePastingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PasteCommand" /> class.
    /// </summary>
    /// <param name="messagePastingService">The message pasting service.</param>
    public PasteCommand(MessagePastingService messagePastingService)
    {
        _messagePastingService = messagePastingService;
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Upload Paste", false)]
    [SlashRequireGuild]
    public async Task UploadPasteAsync(ContextMenuContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        await _messagePastingService.PasteMessageAsync(context.TargetMessage, context.Member).ConfigureAwait(false);
        await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Message pasted")).ConfigureAwait(false);
    }
}
