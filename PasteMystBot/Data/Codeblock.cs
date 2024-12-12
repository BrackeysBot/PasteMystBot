namespace PasteMystBot.Data;

/// <summary>
///     Represents a codeblock.
/// </summary>
public readonly struct Codeblock
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Codeblock" /> struct.
    /// </summary>
    /// <param name="content">The content of the codeblock.</param>
    /// <param name="language">The language name of the codeblock.</param>
    public Codeblock(string content, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            content = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            language = null;
        }

        Content = content;
        Language = language;
    }

    /// <summary>
    ///     Gets the content of the codeblock.
    /// </summary>
    /// <value>The content.</value>
    public string Content { get; }

    /// <summary>
    ///     Gets the language name of the codeblock.
    /// </summary>
    /// <value>The language name, or <see langword="null" /> if hi language was specified.</value>
    public string? Language { get; }
}
