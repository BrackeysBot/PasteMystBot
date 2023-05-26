using DSharpPlus.Entities;
using NLog;
using PasteMystBot.Configuration;
using PasteMystBot.Data;
using PasteMystNet;
using X10D.Text;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which handles the pasting of messages.
/// </summary>
internal sealed class MessagePastingService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configurationService;
    private readonly CodeblockDetectionService _codeblockDetectionService;
    private readonly PasteMystService _pasteMystService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessagePastingService" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="codeblockDetectionService">The codeblock detection service.</param>
    /// <param name="pasteMystService">The PasteMyst service.</param>
    public MessagePastingService(
        HttpClient httpClient,
        ConfigurationService configurationService,
        CodeblockDetectionService codeblockDetectionService,
        PasteMystService pasteMystService
    )
    {
        _httpClient = httpClient;
        _configurationService = configurationService;
        _codeblockDetectionService = codeblockDetectionService;
        _pasteMystService = pasteMystService;
    }

    /// <summary>
    ///     Determines if a channel is exempt from auto-pasting.
    /// </summary>
    /// <param name="channel">The channel whose status to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the channel is exempt from auto-pasting; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsChannelExempt(DiscordChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (channel.Guild is not { } guild)
        {
            return true;
        }

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            return false;
        }

        return Array.IndexOf(configuration.IgnoredChannels, channel.Id) > -1;
    }

    /// <summary>
    ///     Attempts to paste the attachments, codeblocks, or raw content of a message.
    /// </summary>
    /// <param name="message">The message to paste.</param>
    /// <param name="paster">The user who initiated the paste request.</param>
    /// <param name="deleteMessage">
    ///     <see langword="true" /> to delete the source message; otherwise, <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="message" /> or <paramref name="paster" /> is <see langword="null" />.
    /// </exception>
    public async Task<bool> PasteMessageAsync(DiscordMessage message, DiscordMember paster, bool deleteMessage = true)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(paster);

        string content = message.Content;
        bool contentEmpty = string.IsNullOrWhiteSpace(content);

        int attachmentsCount = message.Attachments.Count;
        switch (attachmentsCount)
        {
            case 0 when !contentEmpty && _codeblockDetectionService.IsExclusivelyCodeblocks(content):
                return await PasteMessageCodeblocksAsync(message, deleteMessage, false).ConfigureAwait(false);
            case > 0 when contentEmpty && QualifiesForPasting(message):
                return await PasteMessageAttachmentsAsync(message, deleteMessage, false).ConfigureAwait(false);
        }

        PasteMystPaste? paste = await _pasteMystService.PastePastiesAsync(message.Author, new[]
        {
            new PasteMystPastyForm
            {
                Title = "(untitled)",
                Language = "Autodetect",
                Code = content
            }
        });

        if (paste is null)
        {
            return false;
        }

        if (deleteMessage)
        {
            await message.DeleteAsync("Auto-pasted").ConfigureAwait(false);
        }

        var response = $"{message.Author.Mention}, your message was pasted to {paste.Url}";
        await message.Channel.SendMessageAsync(response).ConfigureAwait(false);

        Logger.Info($"Message by {message.Author} was pasted to {paste.Url} by {paster}");
        return true;
    }

    /// <summary>
    ///     Pastes the attachments of a message.
    /// </summary>
    /// <param name="message">The message whose attachments to paste.</param>
    /// <param name="deleteMessage">
    ///     <see langword="true" /> to delete the source message; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="quoteAutomatically">
    ///     <see langword="true" /> to include the term "automatically" in the response message; otherwise,
    ///     <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public async Task<bool> PasteMessageAttachmentsAsync(
        DiscordMessage message,
        bool deleteMessage = true,
        bool quoteAutomatically = true
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        var pasties = new List<PasteMystPastyForm>();

        foreach (DiscordAttachment attachment in message.Attachments)
        {
            await using Stream stream = await _httpClient.GetStreamAsync(attachment.Url).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync().ConfigureAwait(false);
            string title = attachment.FileName;
            var language = "Autodetect";

            if (title == "message.txt")
            {
                title = "(untitled)";
            }
            else
            {
                string extension = Path.GetExtension(attachment.FileName);
                language = await _pasteMystService.GetLanguageNameByExtensionAsync(extension).ConfigureAwait(false);
            }

            pasties.Add(new PasteMystPastyForm
            {
                Title = title,
                Language = language,
                Code = content
            });
        }

        PasteMystPaste? paste = await _pasteMystService.PastePastiesAsync(message.Author, pasties);
        if (paste is null)
        {
            return false;
        }

        if (deleteMessage)
        {
            await message.DeleteAsync("Auto-pasted").ConfigureAwait(false);
        }

        string phrase = pasties.Count > 1 ? "attachments were" : "attachment was";
        var response =
            $"{message.Author.Mention}, your {phrase + (quoteAutomatically ? " automatically" : "")} pasted to {paste.Url}";
        await message.Channel.SendMessageAsync(response).ConfigureAwait(false);

        Logger.Info(
            $"{pasties.Count}{phrase + (quoteAutomatically ? " automatically" : "")} pasted to {paste.Url} ({message.Author})");
        return true;
    }

    /// <summary>
    ///     Pastes the attachments of a message.
    /// </summary>
    /// <param name="message">The message whose attachments to paste.</param>
    /// <param name="deleteMessage">
    ///     <see langword="true" /> to delete the source message; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="quoteAutomatically">
    ///     <see langword="true" /> to include the term "automatically" in the response message; otherwise,
    ///     <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public async Task<bool> PasteMessageCodeblocksAsync(
        DiscordMessage message,
        bool deleteMessage = true,
        bool quoteAutomatically = true
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyList<string> codeblocks = _codeblockDetectionService.DetectCodeblocks(message.Content);
        if (codeblocks.Count == 0)
        {
            return false;
        }

        var pasties = new List<PasteMystPastyForm>();

        foreach (string content in codeblocks)
        {
            Codeblock codeblock = Codeblock.Parse(content);
            string language = await _pasteMystService.GetLanguageNameAsync(codeblock.Language).ConfigureAwait(false);

            pasties.Add(new PasteMystPastyForm
            {
                Title = "(untitled)",
                Language = language,
                Code = codeblock.Content
            });
        }

        PasteMystPaste? paste = await _pasteMystService.PastePastiesAsync(message.Author, pasties);
        if (paste is null)
        {
            return false;
        }

        if (deleteMessage)
        {
            await message.DeleteAsync("Auto-pasted").ConfigureAwait(false);
        }

        string phrase = pasties.Count > 1 ? "codeblocks were" : "codeblock was";
        string response =
            $"{message.Author.Mention}, your {phrase + (quoteAutomatically ? " automatically" : "")} pasted to {paste.Url}";
        await message.Channel.SendMessageAsync(response).ConfigureAwait(false);

        Logger.Info(
            $"{pasties.Count} {phrase + (quoteAutomatically ? " automatically" : "")} pasted to {paste.Url} ({message.Author})");
        return true;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified message qualifies for auto-pasting.
    /// </summary>
    /// <param name="message">The message whose qualification to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the message qualifies for auto-pasting according to its guild rules; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public bool QualifiesForPasting(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_configurationService.TryGetGuildConfiguration(message.Channel.Guild,
                out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        string content = message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return AttachmentsQualifyForPasting(message);
        }

        if (!_codeblockDetectionService.IsExclusivelyCodeblocks(content) && !configuration.AutoPasteIfText)
        {
            return false;
        }

        IReadOnlyList<string> codeblocks = _codeblockDetectionService.DetectCodeblocks(message.Content);
        int countThreshold = configuration.CountThreshold;
        int lineThreshold = configuration.LineThreshold;

        if (countThreshold > -1 && codeblocks.Count > countThreshold)
        {
            return true;
        }

        if (lineThreshold > -1)
        {
            foreach (string rawCodeblock in codeblocks)
            {
                Codeblock codeblock = Codeblock.Parse(rawCodeblock);
                int lineCount = codeblock.Content.CountSubstring('\n') + 1;
                Console.WriteLine(codeblock.Content);
                Console.WriteLine(lineCount);
                Console.WriteLine(lineThreshold);
                if (lineCount > lineThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool AttachmentsQualifyForPasting(DiscordMessage message)
    {
        DiscordGuild guild = message.Channel.Guild;
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            return false;
        }

        IReadOnlyList<DiscordAttachment> attachments = message.Attachments;
        if (!configuration.PasteAttachments || attachments.Count <= 0)
        {
            return false;
        }

        foreach (DiscordAttachment attachment in attachments)
        {
            string mimeType = attachment.MediaType;
            if (mimeType.Contains(';'))
            {
                mimeType = mimeType[..mimeType.IndexOf(';')];
            }

            if (mimeType != "text/plain")
            {
                return true;
            }
        }

        {
            return true;
        }
    }
}
