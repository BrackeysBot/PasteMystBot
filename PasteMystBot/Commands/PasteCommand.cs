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

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste (Keep Message)", false)]
    [SlashRequireGuild]
    public async Task UploadPasteKeepMessageAsync(ContextMenuContext context)
    {
        await PasteMessageAsync(context, false);
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste (Delete Message)", false)]
    [SlashRequireGuild]
    public async Task UploadPasteDeleteMessageAsync(ContextMenuContext context)
    {
        await PasteMessageAsync(context, true);
    }

    private async Task PasteMessageAsync(ContextMenuContext context, bool deleteMessage)
    {
        DiscordMessage message = context.TargetMessage;

        await context.DeferAsync(true);
        await _messagePastingService.PasteMessageAsync(message, context.Member, deleteMessage);

        var builder = new DiscordWebhookBuilder();
        builder.WithContent("Message pasted");
        await context.EditResponseAsync(builder);
    }
}
