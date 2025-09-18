namespace DebatePrep.Core.Models;

/// <summary>
/// Represents a single turn in the debate with participant response and feedback.
/// </summary>
public sealed class Turn
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ParticipantId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public TurnRating? Rating { get; set; }
    public string? DownvoteReason { get; set; }
    public bool IsIncomplete { get; set; } = false;
    
    public Session? Session { get; set; }
    public Participant? Participant { get; set; }
}

/// <summary>
/// Rating for a turn (thumbs up/down).
/// </summary>
public enum TurnRating
{
    Up = 1,
    Down = -1
}
