using System.Diagnostics;
using System.Text;
using Cysharp.Text;
using PasteMystBot.Data;
using PasteMystBot.Services;
using X10D.Text;

namespace PasteMyst.Tests;

[TestClass]
public sealed class CodeblockDetectTests
{
    private readonly CodeblockDetectionService _codeblockDetectionService = new();

    [TestMethod]
    public void DetectCodeblocks_Content_ShouldMatchForString()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Span<char> chars = stackalloc char[source.Length];
        int count = _codeblockDetectionService.DetectCodeblocks(source, chars);
        Assert.AreEqual(1, count);

        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();
        for (var index = 0; chars[index] != '\0'; index++)
            builder.Append(chars[index]);

        chars = stackalloc char[builder.Length];
        Encoding.UTF8.GetChars(builder.AsSpan(), chars);

        Codeblock codeblock = Codeblock.Parse(chars);
        Assert.AreEqual("cs", codeblock.Language, codeblock.Language);
        Assert.AreEqual("Console.WriteLine(\"Hello World\");", codeblock.Content, codeblock.Content);
    }

    [TestMethod]
    public void DetectCodeblocks_Content_ShouldMatchForEmptyCodeblock()
    {
        const string source = "``` ```";
        Span<char> chars = stackalloc char[source.Length];
        int count = _codeblockDetectionService.DetectCodeblocks(source, chars);
        Assert.AreEqual(1, count);

        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();
        for (var index = 0; chars[index] != '\0'; index++)
            builder.Append(chars[index]);

        chars = stackalloc char[builder.Length];
        Encoding.UTF8.GetChars(builder.AsSpan(), chars);

        Codeblock codeblock = Codeblock.Parse(chars);
        Assert.AreEqual(null, codeblock.Language);
        Assert.AreEqual(string.Empty, codeblock.Content);
    }

    [TestMethod]
    public void DetectCodeblocks_Content_ShouldMatchForCodeblockClosingOnSameLine()
    {
        const string source = "```\nConsole.WriteLine(\"Hello World\");```";
        Span<char> chars = stackalloc char[source.Length];
        int count = _codeblockDetectionService.DetectCodeblocks(source, chars);
        Assert.AreEqual(1, count);

        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();
        for (var index = 0; chars[index] != '\0'; index++)
            builder.Append(chars[index]);

        chars = stackalloc char[builder.Length];
        Encoding.UTF8.GetChars(builder.AsSpan(), chars);

        Codeblock codeblock = Codeblock.Parse(chars);
        Assert.AreEqual(null, codeblock.Language);
        Assert.AreEqual("Console.WriteLine(\"Hello World\");", codeblock.Content);
    }

    [TestMethod]
    public void DetectCodeblocks_Content_ShouldMatchForCodeblockOpeningOnSameLine()
    {
        const string source = "```Console.WriteLine(\"Hello World\");\n```";
        Span<char> chars = stackalloc char[source.Length];
        int count = _codeblockDetectionService.DetectCodeblocks(source, chars);
        Assert.AreEqual(1, count);

        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();
        for (var index = 0; chars[index] != '\0'; index++)
            builder.Append(chars[index]);

        chars = stackalloc char[builder.Length];
        Encoding.UTF8.GetChars(builder.AsSpan(), chars);

        Codeblock codeblock = Codeblock.Parse(chars);
        Assert.AreEqual(null, codeblock.Language);
        Assert.AreEqual("Console.WriteLine(\"Hello World\");", codeblock.Content);
    }

    [TestMethod]
    public void DetectCodeblocks_Content_ShouldMatchForOneLineCodeblock()
    {
        const string source = "```Console.WriteLine(\"Hello World\");```";
        Span<char> chars = stackalloc char[source.Length];
        int count = _codeblockDetectionService.DetectCodeblocks(source, chars);
        Assert.AreEqual(1, count);

        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();
        for (var index = 0; chars[index] != '\0'; index++)
            builder.Append(chars[index]);

        chars = stackalloc char[builder.Length];
        Encoding.UTF8.GetChars(builder.AsSpan(), chars);

        Codeblock codeblock = Codeblock.Parse(chars);
        Assert.AreEqual(null, codeblock.Language);
        Assert.AreEqual("Console.WriteLine(\"Hello World\");", codeblock.Content);
    }

    [TestMethod]
    public void DetectCodeblocks_WithSpan_ShouldReturn2()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        int count = _codeblockDetectionService.DetectCodeblocks(source, Span<char>.Empty);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void DetectCodeblocks_WithString_ShouldReturn2()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        IReadOnlyList<string> list = _codeblockDetectionService.DetectCodeblocks(source);
        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public void DetectCodeblocks()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Span<char> blocks = stackalloc char[source.Length];
        Span<char> chars = stackalloc char[source.Length];

        int blockCount = _codeblockDetectionService.DetectCodeblocks(source, blocks);
        Assert.AreEqual(2, blockCount);
        Utf8ValueStringBuilder builder = ZString.CreateUtf8StringBuilder();

        for (int index = 0, blockIndex = 0; blockIndex < blockCount && index < blocks.Length; index++)
        {
            if (blocks[index] == '\0')
            {
                Trace.WriteLine($"Block {blockIndex++}:");
                ReadOnlySpan<byte> bytes = builder.AsSpan();
                Encoding.UTF8.GetChars(bytes, chars);

                Codeblock codeblock = Codeblock.Parse(chars[..bytes.Length]);
                Trace.WriteLine(
                    $"Detected codeblock with language {codeblock.Language?.WithWhiteSpaceAlternative("no language")}:\n{codeblock.Content}");
                Trace.WriteLine("---");

                builder.Clear();
                chars.Clear();
            }
            else
            {
                builder.Append(blocks[index]);
            }
        }
    }

    [TestMethod]
    public void HasCodeblock_WithExclusiveCodeblock_ShouldBeTrue()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithJust3Accents_ShouldBeFalse()
    {
        const string source = "```";
        Assert.IsFalse(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithJust6Accents_ShouldBeFalse()
    {
        const string source = "``````";
        Assert.IsFalse(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithMultipleCodeblocks_ShouldBeTrue()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithMultipleCodeblocksSeparatedByText_ShouldBeTrue()
    {
        const string source =
            "```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithMultipleCodeblocksSurroundedByText_ShouldBeTrue()
    {
        const string source =
            "Regular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithSpacedOutAccentsBefore_ShouldBeFalse()
    {
        const string source = "`` `cs\nConsole.WriteLine(\"Hello World\");```";
        Assert.IsFalse(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithSpacedOutAccentsAfter_ShouldBeFalse()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n`` `";
        Assert.IsFalse(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithTextAfter_ShouldBeTrue()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\nThat was the code";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithTextBefore_ShouldBeTrue()
    {
        const string source = "Here is the code:\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void HasCodeblock_WithTextBeforeAndAfter_ShouldBeFalse()
    {
        const string source = "Here is the code:\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nThat was the code";
        Assert.IsTrue(_codeblockDetectionService.HasCodeblock(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithExclusiveCodeblock_ShouldBeTrue()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithMultipleCodeblocks_ShouldBeTrue()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsTrue(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithMultipleCodeblocksSeparatedByText_ShouldBeFalse()
    {
        const string source =
            "```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsFalse(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithMultipleCodeblocksSurroundedByText_ShouldBeFalse()
    {
        const string source =
            "Regular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nRegular text";
        Assert.IsFalse(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithTextAfter_ShouldBeFalse()
    {
        const string source = "```cs\nConsole.WriteLine(\"Hello World\");\n```\nThat was the code";
        Assert.IsFalse(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithTextBefore_ShouldBeFalse()
    {
        const string source = "Here is the code:\n```cs\nConsole.WriteLine(\"Hello World\");\n```";
        Assert.IsFalse(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }

    [TestMethod]
    public void IsExclusivelyCodeblocks_WithTextBeforeAndAfter_ShouldBeFalse()
    {
        const string source = "Here is the code:\n```cs\nConsole.WriteLine(\"Hello World\");\n```\nThat was the code";
        Assert.IsFalse(_codeblockDetectionService.IsExclusivelyCodeblocks(source), source);
    }
}
