using DebatePrep.Core.Data;
using DebatePrep.Core.Models;
using Microsoft.Data.Sqlite;

namespace DebatePrep.Core.Services;

/// <summary>
/// Service for managing debate sessions and participants.
/// </summary>
public sealed class SessionService
{
    private readonly DebatePrepContext _context;

    public SessionService(DebatePrepContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Create a new debate session.
    /// </summary>
    public async Task<Session> CreateSessionAsync(string title, string topic, string rules = "")
    {
        var session = new Session
        {
            Title = title,
            Topic = topic,
            Rules = rules,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        const string sql = @"
            INSERT INTO sessions (title, topic, rules, created_at, updated_at, is_active)
            VALUES (@title, @topic, @rules, @created_at, @updated_at, @is_active);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@title", session.Title);
        command.Parameters.AddWithValue("@topic", session.Topic);
        command.Parameters.AddWithValue("@rules", session.Rules);
        command.Parameters.AddWithValue("@created_at", session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@updated_at", session.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@is_active", session.IsActive ? 1 : 0);

        var id = await command.ExecuteScalarAsync();
        session.Id = Convert.ToInt32(id);

        return session;
    }

    /// <summary>
    /// Get a session by ID with participants and turns.
    /// </summary>
    public async Task<Session?> GetSessionAsync(int sessionId)
    {
        const string sessionSql = @"
            SELECT id, title, topic, rules, created_at, updated_at, is_active
            FROM sessions WHERE id = @id";

        using var command = new SqliteCommand(sessionSql, _context.Connection);
        command.Parameters.AddWithValue("@id", sessionId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var session = new Session
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Topic = reader.GetString(2),
            Rules = reader.GetString(3),
            CreatedAt = DateTime.Parse(reader.GetString(4)),
            UpdatedAt = DateTime.Parse(reader.GetString(5)),
            IsActive = reader.GetInt32(6) == 1
        };

        // Load participants
        session.Participants = await GetParticipantsAsync(sessionId);

        // Load turns
        session.Turns = await GetTurnsAsync(sessionId);

        return session;
    }

    /// <summary>
    /// Add a participant to a session.
    /// </summary>
    public async Task<Participant> AddParticipantAsync(
        int sessionId, 
        string name, 
        string position, 
        string constraints = "", 
        string disallowed = "", 
        string keySources = "")
    {
        var participant = new Participant
        {
            SessionId = sessionId,
            Name = name,
            Position = position,
            Constraints = constraints,
            Disallowed = disallowed,
            KeySources = keySources,
            CreatedAt = DateTime.UtcNow
        };

        const string sql = @"
            INSERT INTO participants (session_id, name, position, constraints, disallowed, key_sources, created_at)
            VALUES (@session_id, @name, @position, @constraints, @disallowed, @key_sources, @created_at);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@session_id", participant.SessionId);
        command.Parameters.AddWithValue("@name", participant.Name);
        command.Parameters.AddWithValue("@position", participant.Position);
        command.Parameters.AddWithValue("@constraints", participant.Constraints);
        command.Parameters.AddWithValue("@disallowed", participant.Disallowed);
        command.Parameters.AddWithValue("@key_sources", participant.KeySources);
        command.Parameters.AddWithValue("@created_at", participant.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var id = await command.ExecuteScalarAsync();
        participant.Id = Convert.ToInt32(id);

        return participant;
    }

    /// <summary>
    /// Add a turn to the session.
    /// </summary>
    public async Task<Turn> AddTurnAsync(int sessionId, int participantId, string content, int tokenCount = 0)
    {
        var turn = new Turn
        {
            SessionId = sessionId,
            ParticipantId = participantId,
            Content = content,
            TokenCount = tokenCount,
            CreatedAt = DateTime.UtcNow
        };

        const string sql = @"
            INSERT INTO turns (session_id, participant_id, content, token_count, created_at)
            VALUES (@session_id, @participant_id, @content, @token_count, @created_at);
            SELECT last_insert_rowid();";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@session_id", turn.SessionId);
        command.Parameters.AddWithValue("@participant_id", turn.ParticipantId);
        command.Parameters.AddWithValue("@content", turn.Content);
        command.Parameters.AddWithValue("@token_count", turn.TokenCount);
        command.Parameters.AddWithValue("@created_at", turn.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var id = await command.ExecuteScalarAsync();
        turn.Id = Convert.ToInt32(id);

        return turn;
    }

    /// <summary>
    /// Rate a turn and optionally provide downvote reason.
    /// </summary>
    public async Task RateTurnAsync(int turnId, TurnRating rating, string? downvoteReason = null)
    {
        const string sql = @"
            UPDATE turns 
            SET rating = @rating, downvote_reason = @downvote_reason
            WHERE id = @id";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@rating", (int)rating);
        command.Parameters.AddWithValue("@downvote_reason", downvoteReason ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@id", turnId);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<Participant>> GetParticipantsAsync(int sessionId)
    {
        const string sql = @"
            SELECT id, session_id, name, position, constraints, disallowed, key_sources, archived, created_at
            FROM participants WHERE session_id = @session_id ORDER BY created_at";

        var participants = new List<Participant>();

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@session_id", sessionId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            participants.Add(new Participant
            {
                Id = reader.GetInt32(0),
                SessionId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Position = reader.GetString(3),
                Constraints = reader.GetString(4),
                Disallowed = reader.GetString(5),
                KeySources = reader.GetString(6),
                Archived = reader.GetInt32(7) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(8))
            });
        }

        return participants;
    }

    private async Task<List<Turn>> GetTurnsAsync(int sessionId)
    {
        const string sql = @"
            SELECT id, session_id, participant_id, content, token_count, created_at, rating, downvote_reason, is_incomplete
            FROM turns WHERE session_id = @session_id ORDER BY created_at";

        var turns = new List<Turn>();

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@session_id", sessionId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var rating = reader.IsDBNull(6) ? (TurnRating?)null : (TurnRating)reader.GetInt32(6);
            var downvoteReason = reader.IsDBNull(7) ? null : reader.GetString(7);

            turns.Add(new Turn
            {
                Id = reader.GetInt32(0),
                SessionId = reader.GetInt32(1),
                ParticipantId = reader.GetInt32(2),
                Content = reader.GetString(3),
                TokenCount = reader.GetInt32(4),
                CreatedAt = DateTime.Parse(reader.GetString(5)),
                Rating = rating,
                DownvoteReason = downvoteReason,
                IsIncomplete = reader.GetInt32(8) == 1
            });
        }

        return turns;
    }
}
