using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;
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

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste Whole (KEEP)", false)]
    [SlashRequireGuild]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public async Task UploadMessageKeepMessageAsync(ContextMenuContext context)
    {
        await ForcePasteMessageAsync(context, false);
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste Whole (DELETE)", false)]
    [SlashRequireGuild]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public async Task UploadMessageDeleteMessageAsync(ContextMenuContext context)
    {
        await ForcePasteMessageAsync(context, true);
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste Qualifying (KEEP)", false)]
    [SlashRequireGuild]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public async Task UploadPasteKeepMessageAsync(ContextMenuContext context)
    {
        await PasteMessageAsync(context, false);
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Paste Qualifying (DELETE)", false)]
    [SlashRequireGuild]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public async Task UploadPasteDeleteMessageAsync(ContextMenuContext context)
    {
        await PasteMessageAsync(context, true);
    }

    private async Task ForcePasteMessageAsync(ContextMenuContext context, bool deleteMessage)
    {
        DiscordMessage message = context.TargetMessage;

        await context.DeferAsync(true);
        await _messagePastingService.ForcePasteMessageAsync(message, context.Member, deleteMessage);

        var builder = new DiscordWebhookBuilder();
        builder.WithContent("Message was pasted");
        await context.EditResponseAsync(builder);
    }

    private async Task PasteMessageAsync(ContextMenuContext context, bool deleteMessage)
    {
        DiscordMessage message = context.TargetMessage;

        await context.DeferAsync(true);
        int forms = await _messagePastingService.PasteMessageAsync(message, context.Member, deleteMessage, true);

        var builder = new DiscordWebhookBuilder();
        if (forms > 0)
        {
            builder.WithContent($"{"qualifying element".ToQuantity(forms)} {(forms > 1 ? "were" : "was")} pasted");
        }
        else
        {
            builder.WithContent("No qualifying elements detected. Did you mean to **Paste Whole** instead?");
        }

        await context.EditResponseAsync(builder);
    }
}
