using FluentAssertions;
using JobScraper.Domain.Enums;
using JobScraper.Domain.ValueObjects;
using Xunit;

namespace JobScraper.Tests.Domain;

public class ValueObjectsTests
{
    [Fact]
    public void SalaryInfo_FromText_ShouldCreateWithRawText()
    {
        // Arrange
        var text = "5000-7000 PLN";

        // Act
        var salary = SalaryInfo.FromText(text);

        // Assert
        salary.RawText.Should().Be(text);
        salary.Min.Should().BeNull();
        salary.Max.Should().BeNull();
        salary.Currency.Should().BeNull();
    }

    [Fact]
    public void SalaryInfo_FromStructured_ShouldCreateWithStructuredData()
    {
        // Arrange
        var min = 5000m;
        var max = 7000m;
        var currency = "PLN";
        var period = SalaryPeriod.Monthly;
        var grossNet = GrossNet.Gross;

        // Act
        var salary = SalaryInfo.FromStructured(min, max, currency, period, grossNet);

        // Assert
        salary.Min.Should().Be(min);
        salary.Max.Should().Be(max);
        salary.Currency.Should().Be(currency);
        salary.Period.Should().Be(period);
        salary.GrossNet.Should().Be(grossNet);
    }

    [Fact]
    public void YearsExperience_FromNumber_ShouldCreateWithMinMax()
    {
        // Arrange
        var years = 3;

        // Act
        var experience = YearsExperience.FromNumber(years);

        // Assert
        experience.Min.Should().Be(years);
        experience.Max.Should().Be(years);
        experience.RawText.Should().BeNull();
    }

    [Fact]
    public void YearsExperience_FromRange_ShouldCreateWithRange()
    {
        // Arrange
        var min = 2;
        var max = 5;

        // Act
        var experience = YearsExperience.FromRange(min, max);

        // Assert
        experience.Min.Should().Be(min);
        experience.Max.Should().Be(max);
        experience.RawText.Should().BeNull();
    }
}
