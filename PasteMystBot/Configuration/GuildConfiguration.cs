namespace PasteMystBot.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets a value indicating whether messages should be auto-pasted if they contain non-codeblock text.
    /// </summary>
    /// <value>
    ///     <see langword="true" /> if messages containing non-codeblock text should be auto-pasted; otherwise,
    ///     <see langword="false" />.
    /// </value>
    public bool AutoPasteIfText { get; set; } = false;

    /// <summary>
    ///     Gets or sets the count threshold for the amount of codeblocks to be pasted to PasteMyst.
    /// </summary>
    /// <value>The codeblock count threshold.</value>
    /// <remarks>
    ///     If there are more than this number of exclusive codeblocks in a message, regardless of their length, the message will
    ///     be pasted.
    /// </remarks>
    public int CountThreshold { get; set; } = -1;

    /// <summary>
    ///     Gets or sets the array of exempt channels for auto-pasting.
    /// </summary>
    /// <value>An array of channel IDs to ignore when auto-pasting a message.</value>
    public ulong[] IgnoredChannels { get; set; } = Array.Empty<ulong>();

    /// <summary>
    ///     Gets or sets the line threshold for pasting a codeblock to PasteMyst.
    /// </summary>
    /// <value>The line threshold.</value>
    /// <remarks>If any of the codeblocks have more than this many lines, the message will be pasted.</remarks>
    public int LineThreshold { get; set; } = -1;

    /// <summary>
    ///     Gets or sets a value indicating whether to paste messages containing only *.cs and/or message.txt attachments.
    /// </summary>
    /// <value><see langword="true" /> to paste attachments; otherwise, <see langword="false" />.</value>
    public bool PasteAttachments { get; set; } = true;
}
