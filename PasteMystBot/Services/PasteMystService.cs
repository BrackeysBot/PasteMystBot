using DSharpPlus.Entities;
using PasteMystNet;
using X10D.DSharpPlus;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which abstracts connection to PasteMyst.
/// </summary>
internal sealed class PasteMystService
{
    private const string AutodetectLanguage = "Autodetect";

    /// <summary>
    ///     Gets the name of a language by its name.
    /// </summary>
    /// <param name="nameOrExtension">The name or extension of the language to match.</param>
    /// <returns>The recognized name of the language, or <c>Autodetect</c> if the language failed to be detected.</returns>
    public async Task<string> GetLanguageNameAsync(string? nameOrExtension)
    {
        nameOrExtension = await GetLanguageNameByExtensionAsync(nameOrExtension).ConfigureAwait(false);
        if (nameOrExtension == AutodetectLanguage)
        {
            nameOrExtension = await GetLanguageNameByNameAsync(nameOrExtension).ConfigureAwait(false);
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
            PasteMystLanguage language = await PasteMystLanguage.GetLanguageByExtensionAsync(extension);
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
            PasteMystLanguage language = await PasteMystLanguage.GetLanguageByNameAsync(name);
            return language.Name;
        }
        catch
        {
            return AutodetectLanguage;
        }
    }

    /// <summary>
    ///     Uploads an enumerable collection of pasties to PasteMyst, attributed to the specified user.
    /// </summary>
    /// <param name="user">The user who should be attributed for the paste.</param>
    /// <param name="pasties">The pasties to upload.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="user" /> or <paramref name="pasties" /> is <see langword="null" />.
    /// </exception>
    public async Task<PasteMystPaste?> PastePastiesAsync(DiscordUser user, IReadOnlyList<PasteMystPastyForm> pasties)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(pasties);

        if (pasties.Count == 0)
        {
            return null;
        }

        var pasteForm = new PasteMystPasteForm
        {
            Title = $"Automatic paste by {user.GetUsernameWithDiscriminator()}",
            Pasties = pasties.ToArray()
        };

        try
        {
            return await pasteForm.PostPasteAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
