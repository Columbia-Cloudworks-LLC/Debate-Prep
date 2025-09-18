namespace DebatePrep.Core.Models;

/// <summary>
/// Represents a chunk of tokens from a streaming response.
/// </summary>
public sealed record TokenChunk(
    string Text,        // raw text chunk
    int TokenCount,     // number of tokens in this chunk
    bool IsFinal        // true if provider signals end-of-stream
);
