using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using PasteMystBot.Extensions;
using PasteMystNet;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which abstracts connection to PasteMyst.
/// </summary>
internal sealed class PasteMystService
{
    private readonly ILogger<PasteMystService> _logger;
    private readonly PasteMystClient _pasteMystClient;
    private const string AutodetectLanguage = "Autodetect";

    /// <summary>
    ///     Initializes a new instance of the <see cref="PasteMystService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pasteMystClient">The PasteMyst client.</param>
    public PasteMystService(ILogger<PasteMystService> logger, PasteMystClient pasteMystClient)
    {
        _logger = logger;
        _pasteMystClient = pasteMystClient;
    }

    public async Task<PasteMystPaste> CreatePasteAsync(DiscordUser user, PasteMystPastyForm[] forms)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (forms is null)
        {
            throw new ArgumentNullException(nameof(forms));
        }

        if (forms.Length == 0)
        {
            throw new ArgumentException("Cannot submit empty form set.", nameof(forms));
        }

        var form = new PasteMystPasteForm
        {
            Title = $"Automatic paste by {user.GetUsernameWithDiscriminator()}",
            Pasties = forms
        };

        _logger.LogInformation("Creating paste from form with {Count} pasties", form.Pasties.Count);
        return await _pasteMystClient.CreatePasteAsync(form);
    }

    /// <summary>
    ///     Gets the name of a language by its name.
    /// </summary>
    /// <param name="nameOrExtension">The name or extension of the language to match.</param>
    /// <returns>The recognized name of the language, or <c>Autodetect</c> if the language failed to be detected.</returns>
    public async Task<string> GetLanguageNameAsync(string? nameOrExtension)
    {
        nameOrExtension = await GetLanguageNameByExtensionAsync(nameOrExtension);
        if (nameOrExtension == AutodetectLanguage)
        {
            nameOrExtension = await GetLanguageNameByNameAsync(nameOrExtension);
        }

        return nameOrExtension;
    }

    /// <summary>
    ///     Gets the name of a language by its extension.
    /// </summary>
    /// <param name="extension">The file extension, which may or may not be preceded with a period (.).</param>
    /// <returns>The name of the language, or <c>Autodetect</c> if the language failed to be detected.</returns>
    public async Task<string> GetLanguageNameByExtensionAsync(string? extension)
    {
        if (extension?.Length > 0 && extension[0] == '.')
        {
            extension = extension[1..];
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            return AutodetectLanguage;
        }

        try
        {
            PasteMystLanguage language = await _pasteMystClient.GetLanguageByExtensionAsync(extension);
            return language.Name;
        }
        catch
        {
            return AutodetectLanguage;
        }
    }

    /// <summary>
    ///     Gets the name of a language by its name.
    /// </summary>
    /// <param name="name">The name of the language to match.</param>
    /// <returns>The recognized name of the language, or <c>Autodetect</c> if the language failed to be detected.</returns>
    public async Task<string> GetLanguageNameByNameAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AutodetectLanguage;
        }

        try
        {
            PasteMystLanguage language = await _pasteMystClient.GetLanguageByNameAsync(name);
            return language.Name;
        }
        catch
        {
            return AutodetectLanguage;
        }
    }
}
