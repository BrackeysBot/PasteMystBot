using PasteMystBot.Data;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which detects codeblocks from strings.
/// </summary>
internal sealed class CodeblockDetectionService
{
    /// <summary>
    ///     Returns a list of all the codeblocks in the input.
    /// </summary>
    /// <param name="source">The string through which to search.</param>
    /// <returns>A read-only view of the codeblocks in <paramref name="source" />.</returns>
    public IReadOnlyList<Codeblock> DetectCodeblocks(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Array.Empty<Codeblock>();
        }

        return DetectCodeblocks(source.AsSpan());
    }

    /// <summary>
    ///     Returns a list of all the codeblocks in the input.
    /// </summary>
    /// <param name="source">The span of characters through which to search.</param>
    /// <returns>A read-only view of the codeblocks in <paramref name="source" />.</returns>
    public IReadOnlyList<Codeblock> DetectCodeblocks(ReadOnlySpan<char> source)
    {
        const string fence = "```";
        var codeblocks = new List<Codeblock>();
        int start = 0;

        while (start < source.Length)
        {
            int fenceStart = source[start..].IndexOf(fence.AsSpan());
            if (fenceStart == -1)
            {
                break;
            }

            fenceStart += start;
            int fenceEnd = fenceStart + fence.Length;

            int newlineIdx = source[fenceEnd..].IndexOf('\n');
            if (newlineIdx == -1)
            {
                break;
            }

            newlineIdx += fenceEnd;

            ReadOnlySpan<char> languageLine = source.Slice(fenceEnd, newlineIdx - fenceEnd).Trim();

            int closingFenceStart = source[newlineIdx..].IndexOf(fence.AsSpan());
            if (closingFenceStart == -1)
            {
                break;
            }

            closingFenceStart += newlineIdx;
            int closingFenceEnd = closingFenceStart + fence.Length;

            // exclude fences
            var content = source.Slice(newlineIdx, closingFenceStart - newlineIdx).Trim().ToString();
            codeblocks.Add(new Codeblock(content, languageLine.ToString()));

            start = closingFenceEnd;
        }

        return codeblocks.AsReadOnly();
    }

    /// <summary>
    ///     Returns a value indicating whether the specified content consists only of a single codeblock.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="content" /> contains a one or more codeblocks; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool HasCodeblock(string content)
    {
        return !string.IsNullOrWhiteSpace(content) && HasCodeblock(content.AsSpan());
    }

    /// <summary>
    ///     Returns a value indicating whether the specified content consists only of a single codeblock.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="content" /> contains a one or more codeblocks; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool HasCodeblock(ReadOnlySpan<char> content)
    {
        const string fence = "```";
        int start = 0;

        while (start < content.Length)
        {
            int fenceStart = content[start..].IndexOf(fence.AsSpan());
            if (fenceStart == -1)
            {
                break;
            }

            fenceStart += start;
            int fenceEnd = fenceStart + fence.Length;

            // check for closing fence
            int closingFenceStart = content[fenceEnd..].IndexOf(fence.AsSpan());
            if (closingFenceStart != -1)
            {
                return true;
            }

            start = fenceEnd;
        }

        return false;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified content consists only codeblocks.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="content" /> contains a single codeblock; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool IsExclusivelyCodeblocks(string content)
    {
        return !string.IsNullOrWhiteSpace(content) && IsExclusivelyCodeblocks(content.AsSpan());
    }

    /// <summary>
    ///     Returns a value indicating whether the specified content consists only codeblocks.
    /// </summary>
    /// <param name="source">The character span to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="source" /> contains a single codeblock; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool IsExclusivelyCodeblocks(ReadOnlySpan<char> source)
    {
        const string fence = "```";
        int start = 0;

        while (start < source.Length)
        {
            source = source[start..].TrimStart();
            if (source.IsEmpty)
            {
                return true; // Only whitespace left
            }

            if (!source.StartsWith(fence.AsSpan()))
            {
                return false; // Non-codeblock content found
            }

            int fenceEnd = fence.Length;

            int closingFenceStart = source[fenceEnd..].IndexOf(fence.AsSpan());
            if (closingFenceStart == -1)
            {
                return false; // unclosed codeblock found
            }

            closingFenceStart += fenceEnd;

            start = closingFenceStart + fence.Length;
        }

        return true; // Entire source is valid
    }
}
