using FluentAssertions;
using SquadCommerce.Web.Services;

namespace SquadCommerce.Web.Tests.Services;

public class ChatCommandServiceTests
{
    [Fact]
    public void Should_FireOnCommandRequested_When_SendCommandCalled()
    {
        // Arrange
        var service = new ChatCommandService();
        string? receivedCommand = null;
        service.OnCommandRequested += cmd => receivedCommand = cmd;

        // Act
        service.SendCommand("analyze SKU-1001");

        // Assert
        receivedCommand.Should().Be("analyze SKU-1001");
    }

    [Fact]
    public void Should_DeliverCommandToAllSubscribers_When_MultipleSubscribed()
    {
        // Arrange
        var service = new ChatCommandService();
        var received = new List<string>();
        service.OnCommandRequested += cmd => received.Add($"sub1:{cmd}");
        service.OnCommandRequested += cmd => received.Add($"sub2:{cmd}");

        // Act
        service.SendCommand("price drop");

        // Assert
        received.Should().HaveCount(2);
        received.Should().Contain("sub1:price drop");
        received.Should().Contain("sub2:price drop");
    }

    [Fact]
    public void Should_NotThrow_When_SendCommandCalledWithNoSubscribers()
    {
        // Arrange
        var service = new ChatCommandService();

        // Act
        var act = () => service.SendCommand("orphan command");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_DeliverCorrectCommand_When_MultipleSendCommandsCalled()
    {
        // Arrange
        var service = new ChatCommandService();
        var commands = new List<string>();
        service.OnCommandRequested += cmd => commands.Add(cmd);

        // Act
        service.SendCommand("first");
        service.SendCommand("second");
        service.SendCommand("third");

        // Assert
        commands.Should().Equal("first", "second", "third");
    }
}
