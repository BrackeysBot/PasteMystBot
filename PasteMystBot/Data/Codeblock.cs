using System.Text;
using Cysharp.Text;
using X10D.Text;

namespace PasteMystBot.Data;

public readonly struct Codeblock
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Codeblock" /> struct.
    /// </summary>
    /// <param name="content">The content of the codeblock.</param>
    /// <param name="language">The language name of the codeblock.</param>
    public Codeblock(string content, string? language)
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

    public static Codeblock Parse(ReadOnlySpan<char> source)
    {
        Utf8ValueStringBuilder lineBuffer = ZString.CreateUtf8StringBuilder();
        Utf8ValueStringBuilder codeBuffer = ZString.CreateUtf8StringBuilder();
        Span<char> chars = stackalloc char[source.Length];
        Span<char> language = stackalloc char[100];
        var lineLength = 0;
        var firstLine = true;

        for (var index = 0; index < source.Length; index++)
        {
            char current = source[index];
            if (current == '\n')
            {
                lineLength = lineBuffer.Length;
                ReadOnlySpan<byte> bytes = lineBuffer.AsSpan();
                Encoding.UTF8.GetChars(bytes, chars);

                if (firstLine)
                {
                    for (var lineIndex = 0; lineIndex < lineLength; lineIndex++)
                    {
                        char lineChar = chars[lineIndex];
                        if (!char.IsLetter(lineChar) && !char.IsDigit(lineChar))
                        {
                            codeBuffer.Append(chars[..lineLength]);
                            language = Span<char>.Empty;
                            break;
                        }

                        language[lineIndex] = lineChar;
                    }

                    if (language.Length > 0)
                    {
                        language = language[..lineLength];
                    }
                }
                else
                {
                    codeBuffer.AppendLine(chars[..lineLength]);
                }

                lineLength = 0;
                lineBuffer.Clear();
                firstLine = false;
            }
            else
            {
                lineLength++;
                lineBuffer.Append(current);
            }
        }

        if (lineBuffer.Length > 0)
        {
            lineLength = lineBuffer.Length;
            ReadOnlySpan<byte> bytes = lineBuffer.AsSpan();
            Encoding.UTF8.GetChars(bytes, chars);
            codeBuffer.AppendLine(chars[..lineLength]);
        }

        chars = stackalloc char[codeBuffer.Length];
        Encoding.UTF8.GetChars(codeBuffer.AsSpan(), chars);

        if (chars.IsEmpty)
        {
            chars = Span<char>.Empty;
        }

        if (language.IsEmpty)
        {
            language = Span<char>.Empty;
        }

        return new Codeblock(chars.Trim('\0').Trim().ToString(), language.Trim('\0').Trim().ToString().AsNullIfWhiteSpace());
    }
}
