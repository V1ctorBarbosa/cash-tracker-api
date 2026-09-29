using CashTrack.Domain.Common;
using FluentAssertions;

namespace CashTrack.Tests.Domain.Common;

public sealed class NewEntityIdTest
{
    [Fact]
    public void Generate_ReturnsAParsableGuidString()
    {
        // Act
        var id = NewEntityId.Generate();

        // Assert
        Guid.TryParse(id, out _).Should().BeTrue();
    }

    [Fact]
    public void Generate_ReturnsAVersion7Guid()
    {
        // Act
        var id = NewEntityId.Generate();

        // Assert
        Guid.Parse(id).Version.Should().Be(7);
    }

    [Fact]
    public void Generate_ReturnsADifferentValue_OnEachCall()
    {
        // Act
        var first = NewEntityId.Generate();
        var second = NewEntityId.Generate();

        // Assert
        first.Should().NotBe(second);
    }
}
