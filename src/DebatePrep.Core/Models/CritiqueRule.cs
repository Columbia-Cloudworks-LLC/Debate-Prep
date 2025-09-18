namespace DebatePrep.Core.Models;

/// <summary>
/// Represents a critique rule stored in a participant's memory for future guidance.
/// </summary>
public sealed class CritiqueRule
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public string Rule { get; set; } = string.Empty;
    public string BadPattern { get; set; } = string.Empty;
    public string Guidance { get; set; } = string.Empty;
    public double Strength { get; set; } = 0.7;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Participant? Participant { get; set; }
}
