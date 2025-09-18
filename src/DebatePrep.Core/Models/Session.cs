namespace DebatePrep.Core.Models;

/// <summary>
/// Represents a debate session with participants, topic, and rules.
/// </summary>
public sealed class Session
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Rules { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<Participant> Participants { get; set; } = new();
    public List<Turn> Turns { get; set; } = new();
}
