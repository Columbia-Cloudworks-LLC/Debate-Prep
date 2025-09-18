namespace DebatePrep.Core.Models;

/// <summary>
/// Represents a debate participant with their persona and critique memory.
/// </summary>
public sealed class Participant
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Disallowed { get; set; } = string.Empty;
    public string KeySources { get; set; } = string.Empty;
    public bool Archived { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    
    public List<CritiqueRule> CritiqueRules { get; set; } = new();
    public Session? Session { get; set; }
}
