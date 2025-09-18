using DebatePrep.Core.Models;
using System.Text;

namespace DebatePrep.Core.Services;

/// <summary>
/// Service for exporting debate sessions to various formats.
/// </summary>
public sealed class ExportService
{
    /// <summary>
    /// Export formats supported by the application.
    /// </summary>
    public enum ExportFormat
    {
        Markdown,
        Html,
        PlainText
    }

    /// <summary>
    /// Export a session to the specified format.
    /// </summary>
    public string ExportSession(Session session, ExportFormat format, string? providerModel = null)
    {
        return format switch
        {
            ExportFormat.Markdown => ExportToMarkdown(session, providerModel),
            ExportFormat.Html => ExportToHtml(session, providerModel),
            ExportFormat.PlainText => ExportToPlainText(session, providerModel),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static string ExportToMarkdown(Session session, string? providerModel)
    {
        var sb = new StringBuilder();
        
        // Header with metadata (fixed order as per PRD Appendix E.4)
        sb.AppendLine($"# {session.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Topic**: {session.Topic}");
        
        if (!string.IsNullOrWhiteSpace(session.Rules))
        {
            sb.AppendLine($"**Rules**: {session.Rules}");
        }
        
        sb.AppendLine($"**Date**: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        
        if (!string.IsNullOrEmpty(providerModel))
        {
            sb.AppendLine($"**Provider/Model**: {providerModel}");
        }
        
        sb.AppendLine();
        
        // Participants
        sb.AppendLine("**Participants**:");
        foreach (var participant in session.Participants.OrderBy(p => p.CreatedAt))
        {
            var name = participant.Archived ? $"{participant.Name} (archived)" : participant.Name;
            sb.AppendLine($"- {name}: {participant.Position}");
        }
        
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        
        // Transcript
        sb.AppendLine("## Transcript");
        sb.AppendLine();
        
        if (session.Turns.Count == 0)
        {
            sb.AppendLine("*No turns recorded.*");
        }
        else
        {
            foreach (var turn in session.Turns.OrderBy(t => t.CreatedAt))
            {
                var participant = session.Participants.FirstOrDefault(p => p.Id == turn.ParticipantId);
                var participantName = participant?.Name ?? "Unknown";
                
                if (participant?.Archived == true)
                {
                    participantName += " (archived)";
                }
                
                sb.AppendLine($"**[{turn.CreatedAt:HH:mm:ss}] {participantName}**");
                
                // Format content with proper indentation
                var lines = turn.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    sb.AppendLine($"> {line}");
                }
                
                if (turn.IsIncomplete)
                {
                    sb.AppendLine("> *(Response incomplete)*");
                }
                
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Export generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
        
        return sb.ToString();
    }

    private static string ExportToHtml(Session session, string? providerModel)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>{EscapeHtml(session.Title)}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine("        .metadata { background: #f5f5f5; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
        sb.AppendLine("        .participant { margin: 5px 0; }");
        sb.AppendLine("        .turn { margin: 20px 0; padding: 15px; border-left: 4px solid #007acc; background: #f9f9f9; }");
        sb.AppendLine("        .turn-header { font-weight: bold; color: #007acc; margin-bottom: 10px; }");
        sb.AppendLine("        .turn-content { white-space: pre-wrap; }");
        sb.AppendLine("        .incomplete { color: #666; font-style: italic; }");
        sb.AppendLine("        .archived { color: #999; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        sb.AppendLine($"    <h1>{EscapeHtml(session.Title)}</h1>");
        
        sb.AppendLine("    <div class=\"metadata\">");
        sb.AppendLine($"        <p><strong>Topic:</strong> {EscapeHtml(session.Topic)}</p>");
        
        if (!string.IsNullOrWhiteSpace(session.Rules))
        {
            sb.AppendLine($"        <p><strong>Rules:</strong> {EscapeHtml(session.Rules)}</p>");
        }
        
        sb.AppendLine($"        <p><strong>Date:</strong> {session.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>");
        
        if (!string.IsNullOrEmpty(providerModel))
        {
            sb.AppendLine($"        <p><strong>Provider/Model:</strong> {EscapeHtml(providerModel)}</p>");
        }
        
        sb.AppendLine("    </div>");
        
        sb.AppendLine("    <h2>Participants</h2>");
        sb.AppendLine("    <ul>");
        foreach (var participant in session.Participants.OrderBy(p => p.CreatedAt))
        {
            var name = participant.Archived ? $"{participant.Name} (archived)" : participant.Name;
            var cssClass = participant.Archived ? "participant archived" : "participant";
            sb.AppendLine($"        <li class=\"{cssClass}\"><strong>{EscapeHtml(name)}:</strong> {EscapeHtml(participant.Position)}</li>");
        }
        sb.AppendLine("    </ul>");
        
        sb.AppendLine("    <h2>Transcript</h2>");
        
        if (session.Turns.Count == 0)
        {
            sb.AppendLine("    <p><em>No turns recorded.</em></p>");
        }
        else
        {
            foreach (var turn in session.Turns.OrderBy(t => t.CreatedAt))
            {
                var participant = session.Participants.FirstOrDefault(p => p.Id == turn.ParticipantId);
                var participantName = participant?.Name ?? "Unknown";
                
                if (participant?.Archived == true)
                {
                    participantName += " (archived)";
                }
                
                sb.AppendLine("    <div class=\"turn\">");
                sb.AppendLine($"        <div class=\"turn-header\">[{turn.CreatedAt:HH:mm:ss}] {EscapeHtml(participantName)}</div>");
                sb.AppendLine($"        <div class=\"turn-content\">{EscapeHtml(turn.Content)}</div>");
                
                if (turn.IsIncomplete)
                {
                    sb.AppendLine("        <div class=\"incomplete\">(Response incomplete)</div>");
                }
                
                sb.AppendLine("    </div>");
            }
        }
        
        sb.AppendLine($"    <hr>");
        sb.AppendLine($"    <p><em>Export generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</em></p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private static string ExportToPlainText(Session session, string? providerModel)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"{session.Title}");
        sb.AppendLine(new string('=', session.Title.Length));
        sb.AppendLine();
        
        sb.AppendLine($"Topic: {session.Topic}");
        
        if (!string.IsNullOrWhiteSpace(session.Rules))
        {
            sb.AppendLine($"Rules: {session.Rules}");
        }
        
        sb.AppendLine($"Date: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        
        if (!string.IsNullOrEmpty(providerModel))
        {
            sb.AppendLine($"Provider/Model: {providerModel}");
        }
        
        sb.AppendLine();
        
        sb.AppendLine("Participants:");
        foreach (var participant in session.Participants.OrderBy(p => p.CreatedAt))
        {
            var name = participant.Archived ? $"{participant.Name} (archived)" : participant.Name;
            sb.AppendLine($"- {name}: {participant.Position}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Transcript:");
        sb.AppendLine(new string('-', 50));
        
        if (session.Turns.Count == 0)
        {
            sb.AppendLine("No turns recorded.");
        }
        else
        {
            foreach (var turn in session.Turns.OrderBy(t => t.CreatedAt))
            {
                var participant = session.Participants.FirstOrDefault(p => p.Id == turn.ParticipantId);
                var participantName = participant?.Name ?? "Unknown";
                
                if (participant?.Archived == true)
                {
                    participantName += " (archived)";
                }
                
                sb.AppendLine();
                sb.AppendLine($"[{turn.CreatedAt:HH:mm:ss}] {participantName}:");
                sb.AppendLine(turn.Content);
                
                if (turn.IsIncomplete)
                {
                    sb.AppendLine("(Response incomplete)");
                }
            }
        }
        
        sb.AppendLine();
        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"Export generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        return sb.ToString();
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
