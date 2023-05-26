using System.Text;
using Cysharp.Text;

namespace PasteMystBot.Services;

/// <summary>
///     Represents a service which detects codeblocks from strings.
/// </summary>
internal sealed class CodeblockDetectionService
{
    /// <summary>
    ///     Detects the codeblocks from a given string.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <returns>A read-only view of the codeblocks that appear in <paramref name="source" />.</returns>
    public IReadOnlyList<string> DetectCodeblocks(string source)
    {
        var codeblocks = new List<string>();
        Span<char> destination = stackalloc char[source.Length];
        int count = DetectCodeblocks(source, destination);

        for (int index = 0, start = 0; index < destination.Length; index++)
        {
            char current = destination[index];
            if (current == '\0')
            {
                codeblocks.Add(destination[start..index].ToString());
                start = index + 1;
                if (codeblocks.Count == count)
                {
                    break;
                }
            }
        }

        return codeblocks.AsReadOnly();
    }

    /// <summary>
    ///     Detects the codeblocks from a given character span.
    /// </summary>
    /// <param name="source">The source character span.</param>
    /// <param name="destination">The destination to which codeblocks will be written.</param>
    /// <returns>The number of codeblocks that appear in <paramref name="source" />.</returns>
    public int DetectCodeblocks(ReadOnlySpan<char> source, Span<char> destination)
    {
        // Ported from an original C implementation by Pi Man:
        /* int detectCodeblock(char * s, char ** blocks, int blocks_size) {
  char * text = s;
  int block_index = 0;
  int is_in_block = 0;
  blocks[0] = NULL;
  while(*text) {
    if (block_index + 2 >= blocks_size) return -1;
    if (strncmp(text, "```", 3) == 0) {
      if (is_in_block) {
        blocks[block_index + 1] = (text += 3);
        blocks[block_index + 2] = NULL;
        block_index += 2;
      }
      else {
        blocks[block_index] = (text += 3);
      }
    }
    text++;
  }
  if (is_in_block) {
    blocks[block_index] = NULL;
  }
  return 0;
} */
        Utf8ValueStringBuilder buffer = ZString.CreateUtf8StringBuilder();
        Span<char> chars = stackalloc char[source.Length];
        destination.Fill('\0');

        var isInBlock = false;
        var index = 0;
        var destinationIndex = 0;
        var blockCount = 0;

        while (index < source.Length)
        {
            int end = Math.Min(index + 3, source.Length);
            ReadOnlySpan<char> segment = source[index..end];
            if (segment.Equals("```", StringComparison.Ordinal))
            {
                index += 2;

                if (isInBlock)
                {
                    if (buffer.Length > 0)
                    {
                        ReadOnlySpan<byte> bytes = buffer.AsSpan();
                        Encoding.UTF8.GetChars(bytes, chars);
                        int copy = CopyChars(chars, destination, 0, destinationIndex, bytes.Length);
                        if (copy != -1)
                        {
                            destinationIndex += bytes.Length + 1;
                            blockCount++;
                        }

                        buffer.Clear();
                        chars.Clear();
                    }

                    isInBlock = false;
                }
                else
                {
                    isInBlock = true;
                }
            }
            else if (isInBlock)
            {
                buffer.Append(source[index]);
            }

            index++;
        }

        return blockCount;

        static int CopyChars(Span<char> source, Span<char> destination, int sourceStart, int destinationStart, int count)
        {
            if (source.Length <= sourceStart + count)
            {
                return -1;
            }

            var success = 0;

            for (int sourceIndex = sourceStart, destinationIndex = destinationStart;
                 sourceIndex < count && sourceIndex < destination.Length;)
            {
                if (destinationIndex >= destination.Length)
                {
                    break;
                }

                if (source[sourceIndex] == '\0')
                {
                    break;
                }

                destination[destinationIndex++] = source[sourceIndex++];
                success++;
            }

            return success;
        }
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
        return DetectCodeblocks(content, Span<char>.Empty) > 0;
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
        source = source.Trim();
        if (!source.StartsWith("```"))
        {
            return false;
        }

        if (!source.EndsWith("```"))
        {
            return false;
        }

        var isInBlock = false;
        var index = 0;

        while (index < source.Length)
        {
            int end = Math.Min(index + 3, source.Length);
            ReadOnlySpan<char> segment = source[index..end];
            if (segment.Equals("```", StringComparison.Ordinal))
            {
                index += 2;
                isInBlock = !isInBlock;
            }
            else if (!isInBlock && !char.IsWhiteSpace(source[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
