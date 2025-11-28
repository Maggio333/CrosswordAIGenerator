using CrosswordAIGenerator.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Domain.Common;

/// <summary>
/// Testy dla Result pattern (Railway Oriented Programming)
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_Powinien_Utworzyc_Sukces()
    {
        // Act
        var result = Result<string, string>.Success("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("test");
        // Nie sprawdzamy Error dla sukcesu - klasa Result rzuca wyjątek przy próbie dostępu
    }

    [Fact]
    public void Failure_Powinien_Utworzyc_Bled()
    {
        // Act
        var result = Result<string, string>.Failure("error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error");
    }

    [Fact]
    public void Success_Powinien_Pozwolic_Na_Dostep_Do_Wartosci()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act & Assert
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_Powinien_Pozwolic_Na_Dostep_Do_Bledu()
    {
        // Arrange
        var result = Result<int, string>.Failure("Something went wrong");

        // Act & Assert
        result.Error.Should().Be("Something went wrong");
    }
}

