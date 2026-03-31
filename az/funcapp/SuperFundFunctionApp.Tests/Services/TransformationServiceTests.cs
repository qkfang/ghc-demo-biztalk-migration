using FluentAssertions;
using SuperFundFunctionApp.Services;
using Xunit;

namespace SuperFundFunctionApp.Tests.Services;

/// <summary>
/// Tests for TransformationService methods.
/// Validates the business logic from BizTalk's ContributionMapHelper.cs
/// </summary>
public class TransformationServiceTests
{
    private readonly TransformationService _service;

    public TransformationServiceTests()
    {
        _service = new TransformationService();
    }

    #region FormatABN Tests (Scripting Functoid 3)

    [Fact]
    public void FormatABN_ValidElevenDigitABN_ReturnsFormattedString()
    {
        // Arrange
        string input = "51824753556";
        string expected = "51 824 753 556";

        // Act
        string result = _service.FormatABN(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatABN_ABNWithSpaces_ReturnsFormattedString()
    {
        // Arrange - ABN already has spaces but in wrong format
        string input = "518 247 535 56";
        string expected = "51 824 753 556";

        // Act
        string result = _service.FormatABN(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatABN_ABNWithHyphens_ReturnsFormattedString()
    {
        // Arrange
        string input = "51-824-753-556";
        string expected = "51 824 753 556";

        // Act
        string result = _service.FormatABN(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatABN_InvalidLength_ReturnsOriginalValue()
    {
        // Arrange - only 10 digits
        string input = "1234567890";

        // Act
        string result = _service.FormatABN(input);

        // Assert
        result.Should().Be(input); // Returns as-is when invalid
    }

    [Fact]
    public void FormatABN_ContainsNonDigits_ReturnsOriginalValue()
    {
        // Arrange
        string input = "51824753ABC";

        // Act
        string result = _service.FormatABN(input);

        // Assert
        result.Should().Be(input); // Returns as-is when invalid
    }

    [Fact]
    public void FormatABN_NullOrEmpty_ReturnsEmptyString()
    {
        // Act & Assert
        _service.FormatABN(null!).Should().Be(string.Empty);
        _service.FormatABN("").Should().Be("");
        _service.FormatABN("   ").Should().Be("   ");
    }

    #endregion

    #region CalculateNetContribution Tests (Scripting Functoid 4)

    [Fact]
    public void CalculateNetContribution_ValidAmount_AppliesFifteenPercentTax()
    {
        // Arrange - 15% tax means multiply by 0.85
        decimal grossAmount = 875.00m;
        decimal expected = 743.75m; // 875.00 * 0.85

        // Act
        decimal result = _service.CalculateNetContribution(grossAmount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateNetContribution_SecondSampleAmount_AppliesFifteenPercentTax()
    {
        // Arrange
        decimal grossAmount = 750.00m;
        decimal expected = 637.50m; // 750.00 * 0.85

        // Act
        decimal result = _service.CalculateNetContribution(grossAmount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateNetContribution_RoundsToTwoDecimalPlaces()
    {
        // Arrange - test rounding behavior
        decimal grossAmount = 100.33m;
        decimal expected = 85.28m; // 100.33 * 0.85 = 85.2805, rounded to 85.28

        // Act
        decimal result = _service.CalculateNetContribution(grossAmount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateNetContribution_ZeroAmount_ReturnsZero()
    {
        // Arrange
        decimal grossAmount = 0m;
        decimal expected = 0m;

        // Act
        decimal result = _service.CalculateNetContribution(grossAmount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateNetContribution_VerySmallAmount_RoundsCorrectly()
    {
        // Arrange
        decimal grossAmount = 0.01m;
        decimal expected = 0.01m; // 0.01 * 0.85 = 0.0085, rounds to 0.01

        // Act
        decimal result = _service.CalculateNetContribution(grossAmount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
