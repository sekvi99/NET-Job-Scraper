using FluentAssertions;
using JobScraper.Infrastructure.Extensions;
using Xunit;

namespace JobScraper.Tests.Infrastructure;

public class StringExtensionsTests
{
    [Fact]
    public void Truncate_ShouldReturnOriginalString_WhenLengthIsWithinLimit()
    {
        // Arrange
        var text = "Short text";
        var maxLength = 20;

        // Act
        var result = text.Truncate(maxLength);

        // Assert
        result.Should().Be(text);
    }

    [Fact]
    public void Truncate_ShouldTruncateAndAddEllipsis_WhenLengthExceedsLimit()
    {
        // Arrange
        var text = "This is a very long text that should be truncated";
        var maxLength = 20;

        // Act
        var result = text.Truncate(maxLength);

        // Assert
        result.Should().HaveLength(maxLength);
        result.Should().EndWith("...");
        result.Should().Be("This is a very lo...");
    }

    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenInputIsNull()
    {
        // Arrange
        string? text = null;
        var maxLength = 10;

        // Act
        var result = text.Truncate(maxLength);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        // Arrange
        var text = "";
        var maxLength = 10;

        // Act
        var result = text.Truncate(maxLength);

        // Assert
        result.Should().Be("");
    }
}
