using DebatePrep.Core.Data;
using DebatePrep.Core.Services;
using DebatePrep.Core.Models;
using Xunit;

namespace DebatePrep.Tests;

public sealed class SessionServiceTests : IDisposable
{
    private readonly DebatePrepContext _context;
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        // Use in-memory SQLite database for testing
        _context = new DebatePrepContext("Data Source=:memory:");
        _sessionService = new SessionService(_context);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateSession_WithValidData()
    {
        // Arrange
        var title = "Test Debate";
        var topic = "Should we test our code?";
        var rules = "Be respectful";

        // Act
        var session = await _sessionService.CreateSessionAsync(title, topic, rules);

        // Assert
        Assert.NotEqual(0, session.Id);
        Assert.Equal(title, session.Title);
        Assert.Equal(topic, session.Topic);
        Assert.Equal(rules, session.Rules);
        Assert.True(session.IsActive);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldAddParticipant_ToSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Test", "Topic");
        var name = "Policy Hawk";
        var position = "Supports aggressive fiscal tightening";

        // Act
        var participant = await _sessionService.AddParticipantAsync(
            session.Id, name, position);

        // Assert
        Assert.NotEqual(0, participant.Id);
        Assert.Equal(session.Id, participant.SessionId);
        Assert.Equal(name, participant.Name);
        Assert.Equal(position, participant.Position);
    }

    [Fact]
    public async Task GetSessionAsync_ShouldReturnSessionWithParticipants()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync("Test", "Topic");
        await _sessionService.AddParticipantAsync(session.Id, "Participant 1", "Position 1");
        await _sessionService.AddParticipantAsync(session.Id, "Participant 2", "Position 2");

        // Act
        var retrievedSession = await _sessionService.GetSessionAsync(session.Id);

        // Assert
        Assert.NotNull(retrievedSession);
        Assert.Equal(2, retrievedSession.Participants.Count);
        Assert.Equal("Participant 1", retrievedSession.Participants[0].Name);
        Assert.Equal("Participant 2", retrievedSession.Participants[1].Name);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
