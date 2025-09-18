using Microsoft.Data.Sqlite;
using System.Data;

namespace DebatePrep.Core.Data;

/// <summary>
/// SQLite database context for Debate-Prep application.
/// Manages database connections and provides data access methods.
/// </summary>
public sealed class DebatePrepContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public DebatePrepContext(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        InitializeDatabase();
    }

    /// <summary>
    /// Initialize the database schema if it doesn't exist.
    /// </summary>
    private void InitializeDatabase()
    {
        var createTablesScript = @"
            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                topic TEXT NOT NULL,
                rules TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                is_active INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS participants (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                position TEXT NOT NULL,
                constraints TEXT NOT NULL DEFAULT '',
                disallowed TEXT NOT NULL DEFAULT '',
                key_sources TEXT NOT NULL DEFAULT '',
                archived INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (session_id) REFERENCES sessions (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS turns (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id INTEGER NOT NULL,
                participant_id INTEGER NOT NULL,
                content TEXT NOT NULL,
                token_count INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                rating INTEGER NULL,
                downvote_reason TEXT NULL,
                is_incomplete INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (session_id) REFERENCES sessions (id) ON DELETE CASCADE,
                FOREIGN KEY (participant_id) REFERENCES participants (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS critique_rules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                participant_id INTEGER NOT NULL,
                rule TEXT NOT NULL,
                bad_pattern TEXT NOT NULL,
                guidance TEXT NOT NULL,
                strength REAL NOT NULL DEFAULT 0.7,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (participant_id) REFERENCES participants (id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_participants_session_id ON participants(session_id);
            CREATE INDEX IF NOT EXISTS idx_turns_session_id ON turns(session_id);
            CREATE INDEX IF NOT EXISTS idx_turns_participant_id ON turns(participant_id);
            CREATE INDEX IF NOT EXISTS idx_critique_rules_participant_id ON critique_rules(participant_id);
        ";

        using var command = new SqliteCommand(createTablesScript, _connection);
        command.ExecuteNonQuery();
    }

    public SqliteConnection Connection => _connection;

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
