using CrosswordAIGenerator.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Domain.Common;

/// <summary>
/// Testy dla Constants
/// </summary>
public class ConstantsTests
{
    [Fact]
    public void Constants_Powinien_Miec_Poprawne_Wartosci_Domyslne()
    {
        // Assert - sprawdź że stałe mają sensowne wartości
        Constants.DefaultWallProbability.Should().BeInRange(0.0, 1.0);
        Constants.DefaultTargetWordCount.Should().BeGreaterThan(0);
        Constants.MinDatasetSize.Should().BeGreaterThan(0);
        Constants.MaxDatasetSize.Should().BeGreaterThan(Constants.MinDatasetSize);
        Constants.DefaultXamlWidth.Should().BeGreaterThan(0);
        Constants.DefaultXamlHeight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constants_Powinien_Miec_Poprawne_Ograniczenia()
    {
        // Assert - sprawdź że min < max
        Constants.MinTargetWordCount.Should().BeLessThan(Constants.MaxTargetWordCount);
        Constants.MinDatasetSize.Should().BeLessThan(Constants.MaxDatasetSize);
    }
}

