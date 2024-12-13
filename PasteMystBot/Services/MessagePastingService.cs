using System.Buffers;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MessagePastingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configurationService;
    private readonly CodeblockDetectionService _codeblockDetectionService;
    private readonly PasteMystService _pasteMystService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessagePastingService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="codeblockDetectionService">The codeblock detection service.</param>
    /// <param name="pasteMystService">The PasteMyst service.</param>
    public MessagePastingService(
        ILogger<MessagePastingService> logger,
        HttpClient httpClient,
        ConfigurationService configurationService,
        CodeblockDetectionService codeblockDetectionService,
        PasteMystService pasteMystService
    )
    {
        _logger = logger;
        _httpClient = httpClient;
        _configurationService = configurationService;
        _codeblockDetectionService = codeblockDetectionService;
        _pasteMystService = pasteMystService;
    }

    /// <summary>
    ///     Creates a collection of <see cref="PasteMystPastyForm" /> objects from the given set of attachments.
    /// </summary>
    /// <param name="attachments">The attachments from which the pasties will be created.</param>
    /// <returns>A read-only view of the pasty forms generated from the attachments.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="attachments" /> is <see langword="null" />.</exception>
    public async Task<IReadOnlyList<PasteMystPastyForm>> CreatePastyFormsAsync(IEnumerable<DiscordAttachment> attachments)
    {
        if (attachments is null)
        {
            throw new ArgumentNullException(nameof(attachments));
        }

        var pasties = new List<PasteMystPastyForm>();

        foreach (DiscordAttachment attachment in attachments)
        {
            await using var stream = await _httpClient.GetStreamAsync(attachment.Url);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();

            var form = new PasteMystPastyForm
            {
                Content = text,
                Language = "Autodetect",
                Title = "(untitled)"
            };

            if (attachment.FileName != "message.txt")
            {
                string extension = Path.GetExtension(attachment.FileName);
                form.Language = await _pasteMystService.GetLanguageNameByExtensionAsync(extension);
                form.Title = attachment.FileName;
            }

            pasties.Add(form);
        }

        return pasties.AsReadOnly();
    }

    /// <summary>
    ///     Creates a collection of <see cref="PasteMystPastyForm" /> objects from the given set of codeblocks.
    /// </summary>
    /// <param name="codeblocks">The codeblocks from which the pasties will be created.</param>
    /// <returns>A read-only view of the pasty forms generated from the attachments.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="codeblocks" /> is <see langword="null" />.</exception>
    public async Task<IReadOnlyList<PasteMystPastyForm>> CreatePastyFormsAsync(IEnumerable<Codeblock> codeblocks)
    {
        if (codeblocks is null)
        {
            throw new ArgumentNullException(nameof(codeblocks));
        }

        var pasties = new List<PasteMystPastyForm>();

        foreach (Codeblock codeblock in codeblocks)
        {
            var form = new PasteMystPastyForm
            {
                Title = "(untitled)",
                Language = await _pasteMystService.GetLanguageNameAsync(codeblock.Language),
                Content = codeblock.Content
            };

            _logger.LogDebug("Created pasty form for {Language} codeblock", form.Language);
            pasties.Add(form);
        }

        return pasties.AsReadOnly();
    }

    /// <summary>
    ///     Force-pastes a message.
    /// </summary>
    /// <param name="message">The message to force-paste.</param>
    /// <param name="user">The user who initiated the force.</param>
    /// <param name="deleteMessage">
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Value</term>
    ///             <description>Meaning</description>
    ///         </listheader>
    ///
    ///         <item>
    ///             <term><see langword="true" /></term>
    ///             <description>The message will be deleted.</description>
    ///         </item>
    ///
    ///         <item>
    ///             <term><see langword="false" /></term>
    ///             <description>The message will not be deleted.</description>
    ///         </item>
    ///
    ///         <item>
    ///             <term><see langword="null" /></term>
    ///             <description>Automatic deletion rules will apply.</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task ForcePasteMessageAsync(DiscordMessage message, DiscordUser user, bool deleteMessage)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        _logger.LogInformation("Message was force-pasted by {User}", user);
        Codeblock codeblock = new Codeblock(message.Content, "Markdown");

        IReadOnlyList<PasteMystPastyForm> forms = await CreatePastyFormsAsync([codeblock]);
        await CreatePasteAsync(message, forms, [], deleteMessage, false);
    }

    /// <summary>
    ///     Determines which attachments, if any, qualify for pasting on the specified message.
    /// </summary>
    /// <param name="message">The message whose attachments to verify.</param>
    /// <param name="force"><see langword="true" /> to bypass configuration rules; otherwise, <see langword="false" />.</param>
    /// <returns>A read-only view of the attachments which qualify for pasting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public IReadOnlyList<DiscordAttachment> GetQualifyingAttachments(DiscordMessage message, bool force = false)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Attachments.Count == 0)
        {
            _logger.LogDebug("{Message} has no attachments", message);
            return ArraySegment<DiscordAttachment>.Empty;
        }

        if (message.Channel.Guild is not { } guild)
        {
            _logger.LogDebug("{Message} is not a guild message", message);
            return ArraySegment<DiscordAttachment>.Empty;
        }

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration) && !force)
        {
            _logger.LogWarning("{Guild} is not configured", guild);
            return ArraySegment<DiscordAttachment>.Empty;
        }

        if (!force && !configuration!.PasteAttachments)
        {
            _logger.LogDebug("{Guild} has disabled attachment pasting", guild);
            return ArraySegment<DiscordAttachment>.Empty;
        }

        var attachments = new List<DiscordAttachment>();

        foreach (DiscordAttachment attachment in message.Attachments)
        {
            if (attachment.MediaType.StartsWith("text/plain") || attachment.FileName == "message.txt")
            {
                attachments.Add(attachment);
            }
        }

        _logger.LogDebug("{Mesage} has {Quantity}", message, "qualifying attachment".ToQuantity(attachments.Count));
        return attachments.AsReadOnly();
    }

    /// <summary>
    ///     Determines which attachments, if any, qualify for pasting on the specified message.
    /// </summary>
    /// <param name="message">The message whose attachments to verify.</param>
    /// <param name="force"><see langword="true" /> to bypass configuration rules; otherwise, <see langword="false" />.</param>
    /// <returns>A read-only view of the attachments which qualify for pasting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Codeblock> GetQualifyingCodeblocks(DiscordMessage message, bool force = false)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (!_configurationService.TryGetGuildConfiguration(message.Channel.Guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        string content = message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("{Message} has no qualifying codeblocks: empty content", message);
            return ArraySegment<Codeblock>.Empty;
        }

        IReadOnlyList<Codeblock> codeblocks = _codeblockDetectionService.DetectCodeblocks(message.Content);
        int countThreshold = configuration.CountThreshold;
        int lineThreshold = configuration.LineThreshold;

        if (!force && countThreshold > -1 && codeblocks.Count > 1 && codeblocks.Count < countThreshold)
        {
            _logger.LogDebug("{Message} has no qualifying codeblocks: not enough codeblocks", message);
            return ArraySegment<Codeblock>.Empty;
        }

        if (!force && lineThreshold <= -1)
        {
            _logger.LogDebug("{Message} has no qualifying codeblocks: line threshold is undefined", message);
            return ArraySegment<Codeblock>.Empty;
        }

        var qualifyingCodeblocks = new List<Codeblock>();

        foreach (Codeblock codeblock in codeblocks)
        {
            int lineCount = codeblock.Content.CountSubstring('\n') + 1;
            if (force || lineCount > lineThreshold)
            {
                _logger.LogDebug("Codeblock in {Message} qualifies for pasting: codeblock exceeds line threshold", message);
                qualifyingCodeblocks.Add(codeblock);
            }
        }

        _logger.LogDebug("Found {Count} qualifying codeblocks in message {Id}", qualifyingCodeblocks.Count, message.Id);
        return qualifyingCodeblocks.AsReadOnly();
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
        if (channel is null)
        {
            throw new ArgumentNullException(nameof(channel));
        }

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
    ///     Gets a value indicating whether the specified message qualifies for deletion.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns><see langword="true" /> if the message qualifies for deletion; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public bool QualifiesForDeletion(DiscordMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            if (!_codeblockDetectionService.IsExclusivelyCodeblocks(message.Content))
            {
                return false;
            }

            int actualCodeblocks = _codeblockDetectionService.DetectCodeblocks(message.Content).Count;
            int qualifyingCodeblocks = GetQualifyingCodeblocks(message).Count;

            if (actualCodeblocks > qualifyingCodeblocks)
            {
                return false;
            }
        }

        foreach (DiscordAttachment attachment in message.Attachments)
        {
            _logger.LogDebug("Found attachment {Type}", attachment.MediaType);
            if (!attachment.MediaType.StartsWith("text/plain"))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Pastes the specified message.
    /// </summary>
    /// <param name="message">The message to paste.</param>
    /// <param name="user">The user who initiated the pasting, if any.</param>
    /// <param name="force"><see langword="true" /> to bypass configuration rules; otherwise, <see langword="false" />.</param>
    /// <param name="deleteMessage">
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Value</term>
    ///             <description>Meaning</description>
    ///         </listheader>
    ///
    ///         <item>
    ///             <term><see langword="true" /></term>
    ///             <description>The message will be deleted.</description>
    ///         </item>
    ///
    ///         <item>
    ///             <term><see langword="false" /></term>
    ///             <description>The message will not be deleted.</description>
    ///         </item>
    ///
    ///         <item>
    ///             <term><see langword="null" /></term>
    ///             <description>Automatic deletion rules will apply.</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public async Task<int> PasteMessageAsync(DiscordMessage message, DiscordUser? user = null, bool? deleteMessage = null,
        bool force = false)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (user is not null)
        {
            _logger.LogInformation("Message was force-pasted by {User}", user);
        }

        IReadOnlyList<DiscordAttachment> attachments = GetQualifyingAttachments(message, force);
        IReadOnlyList<PasteMystPastyForm> attachmentForms = await CreatePastyFormsAsync(attachments);

        IReadOnlyList<Codeblock> codeblocks = GetQualifyingCodeblocks(message, force);
        IReadOnlyList<PasteMystPastyForm> codeblockForms = await CreatePastyFormsAsync(codeblocks);

        return await CreatePasteAsync(message, codeblockForms, attachmentForms, deleteMessage, user is null);
    }

    private async Task<int> CreatePasteAsync(DiscordMessage message,
        IReadOnlyList<PasteMystPastyForm> codeblockForms,
        IReadOnlyList<PasteMystPastyForm> attachmentForms,
        bool? deleteMessage,
        bool automatic)
    {
        int codeblockCount = codeblockForms.Count;
        int attachmentCount = attachmentForms.Count;
        int formCount = attachmentCount + codeblockCount;
        if (formCount == 0)
        {
            _logger.LogDebug("No qualifying paste forms found");
            return 0;
        }

        PasteMystPastyForm[] forms = ArrayPool<PasteMystPastyForm>.Shared.Rent(formCount);
        _logger.LogDebug("Rented {Count} elements from array pool", forms.Length);

        int offset = 0;
        for (int i = 0; i < attachmentForms.Count; i++)
        {
            forms[offset + i] = attachmentForms[i];
        }

        offset += attachmentForms.Count;

        for (int i = 0; i < codeblockForms.Count; i++, offset++)
        {
            forms[offset + i] = codeblockForms[i];
        }

        _logger.LogInformation(
            "Detected {TotalCount} forms from message {Message}. {AttachmentCount} from attachments, {CodeblockCount} from codeblocks",
            formCount, message.Id, attachmentCount, codeblockCount);

        try
        {
            DiscordUser author = message.Author;

            _logger.LogDebug("Creating paste for {Author}", author);
            PasteMystPaste paste = await _pasteMystService.CreatePasteAsync(author,
                // ArrayPool<PasteMystPastyForm>.Shared.Rent may have returned a block larger than we need 
                forms.Length > formCount ? forms.AsSpan()[..formCount].ToArray() : forms);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (paste is null)
            {
                _logger.LogCritical("Paste is null! This might be an API error");
                ArrayPool<PasteMystPastyForm>.Shared.Return(forms);
                return 0;
            }

            string method = automatic ? " automatically" : "";
            string verb = formCount > 1 ? $"were{method}" : $"was{method}";

            _logger.LogInformation("{Count} {Verb} pasted to {Url} ({Author})",
                "form".ToQuantity(formCount),
                verb,
                paste.Url,
                author
            );

            if (deleteMessage is true || (deleteMessage is not false && QualifiesForDeletion(message)))
            {
                _logger.LogInformation("{Message} qualifies for deletion", message);
                await message.DeleteAsync("Auto-pasted");
            }

            string term = formCount switch
            {
                > 0 when attachmentCount > 0 && codeblockCount == 0 =>
                    "attachment".ToQuantity(attachmentCount, ShowQuantityAs.None),
                > 0 when attachmentCount == 0 && codeblockCount > 0 =>
                    "codeblock".ToQuantity(codeblockCount, ShowQuantityAs.None),
                _ =>
                    "message"
            };

            if (term == "message")
            {
                // we love edge cases
                verb = "was";
            }

            string response = $"{author.Mention}, your {term} {verb} pasted to {paste.Url}";
            await message.Channel.SendMessageAsync(response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not create paste from pasty forms");
        }
        finally
        {
            _logger.LogDebug("Returning rented memory to array pool");
            ArrayPool<PasteMystPastyForm>.Shared.Return(forms);
        }

        return formCount;
    }
}
