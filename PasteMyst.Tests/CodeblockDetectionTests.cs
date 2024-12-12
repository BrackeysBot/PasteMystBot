using NUnit.Framework;
using PasteMystBot.Data;
using PasteMystBot.Services;

namespace PasteMyst.Tests;

[TestFixture]
public class CodeblockDetectionTests
{
    private readonly CodeblockDetectionService _codeblockDetectionService = new();

    [Test]
    public void DetectCodeblocks_ShouldReturnCodeblocks_GivenSingleCodeblocks()
    {
        const string input = "```\nHello World\n```Hello World";

        IReadOnlyList<Codeblock> result = _codeblockDetectionService.DetectCodeblocks(input);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.TypeOf<Codeblock>());
        Assert.That(result[0].Content, Is.EqualTo("Hello World"));
        Assert.That(result[0].Language, Is.Null);
    }

    [Test]
    public void DetectCodeblocks_ShouldReturnCodeblocks_GivenMultipleCodeblocks()
    {
        const string input = "```\nHello World\n```Hello World\n```cs\nGoodbye World\n```";

        IReadOnlyList<Codeblock> result = _codeblockDetectionService.DetectCodeblocks(input);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.TypeOf<Codeblock>());
        Assert.That(result[1], Is.TypeOf<Codeblock>());

        Assert.That(result[0].Content, Is.EqualTo("Hello World"));
        Assert.That(result[1].Content, Is.EqualTo("Goodbye World"));

        Assert.That(result[0].Language, Is.Null);
        Assert.That(result[1].Language, Is.EqualTo("cs"));
    }

    [Test]
    public void DetectCodeblocks_ShouldReturnEmptyResult_GivenNonCodeblocks()
    {
        const string input = "Hello World";

        IReadOnlyList<Codeblock> result = _codeblockDetectionService.DetectCodeblocks(input);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DetectCodeblocks_ShouldReturnEmptyResult_GivenEmptyString()
    {
        IReadOnlyList<Codeblock> result = _codeblockDetectionService.DetectCodeblocks(string.Empty);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void HasCodeblock_ShouldReturnTrue_GivenSingleCodeblock()
    {
        const string input = "```\nHello World\n```";

        bool result = _codeblockDetectionService.HasCodeblock(input);

        Assert.That(result);
    }

    [Test]
    public void HasCodeblock_ShouldReturnTrue_GivenMultipleCodeblock()
    {
        const string input = "```\nHello World\n```\n```\nHello World\n```";

        bool result = _codeblockDetectionService.HasCodeblock(input);

        Assert.That(result);
    }

    [Test]
    public void HasCodeblock_ShouldReturnTrue_GivenCodeblockWithNonCodeblockText()
    {
        const string input = "```\nHello World\n```\nNon-codeblock text\n```\nHello World\n```";

        bool result = _codeblockDetectionService.HasCodeblock(input);

        Assert.That(result);
    }

    [Test]
    public void HasCodeblock_ShouldReturnFalse_GivenNonCodeblockText()
    {
        const string input = "Helo World";

        bool result = _codeblockDetectionService.HasCodeblock(input);

        Assert.That(result, Is.False);
    }

    [Test]
    public void HasCodeblock_ShouldReturnFalse_GivenEmptyString()
    {
        bool result = _codeblockDetectionService.HasCodeblock(string.Empty);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsExclusivelyCodeblocks_ShouldReturnTrue_GivenSingleCodeblock()
    {
        const string input = "```\nHello World\n```";

        bool result = _codeblockDetectionService.IsExclusivelyCodeblocks(input);

        Assert.That(result);
    }

    [Test]
    public void IsExclusivelyCodeblocks_ShouldReturnTrue_GivenMultipleCodeblock()
    {
        const string input = "```\nHello World\n```\n```\nHello World\n```";

        bool result = _codeblockDetectionService.IsExclusivelyCodeblocks(input);

        Assert.That(result);
    }

    [Test]
    public void IsExclusivelyCodeblocks_ShouldReturnFalse_GivenNonCodeblockText()
    {
        const string input = "```\nHello World\n```\nNon-codeblock text\n```\nHello World\n```";

        bool result = _codeblockDetectionService.IsExclusivelyCodeblocks(input);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsExclusivelyCodeblocks_ShouldReturnFalse_GivenEmptyString()
    {
        bool result = _codeblockDetectionService.IsExclusivelyCodeblocks(string.Empty);

        Assert.That(result, Is.False);
    }
}
