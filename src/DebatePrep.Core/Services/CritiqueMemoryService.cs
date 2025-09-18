using DebatePrep.Core.Data;
using DebatePrep.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;

namespace DebatePrep.Core.Services;

/// <summary>
/// Service for managing critique memory using ML.NET for similarity detection.
/// Implements the algorithm specified in PRD Appendix E.1.
/// </summary>
public sealed class CritiqueMemoryService
{
    private readonly DebatePrepContext _context;
    private readonly MLContext _mlContext;

    public CritiqueMemoryService(DebatePrepContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mlContext = new MLContext(seed: 42); // Fixed seed for determinism
    }

    /// <summary>
    /// Add a critique rule from a downvote, merging with similar existing rules.
    /// </summary>
    public async Task AddCritiqueRuleAsync(int participantId, string rule, string badPattern, string guidance)
    {
        // Get existing rules for similarity comparison
        var existingRules = await GetCritiqueRulesAsync(participantId);
        
        // Check for similar rules using ML.NET
        var similarRule = FindSimilarRule(existingRules, rule);
        
        if (similarRule != null)
        {
            // Merge with existing rule - increase strength and update guidance
            await MergeRuleAsync(similarRule, rule, guidance);
        }
        else
        {
            // Create new rule
            await CreateNewRuleAsync(participantId, rule, badPattern, guidance);
        }
    }

    /// <summary>
    /// Apply upvote decay to all rules not used in the last turn.
    /// </summary>
    public async Task ApplyUpvoteDecayAsync(int participantId, HashSet<int> usedRuleIds)
    {
        const double decayAmount = 0.02;
        
        const string sql = @"
            UPDATE critique_rules 
            SET strength = ROUND(MAX(0.1, strength - @decay), 2),
                updated_at = @updated_at
            WHERE participant_id = @participant_id 
            AND id NOT IN ({0})";

        var placeholders = string.Join(",", usedRuleIds.Select((_, i) => $"@used{i}"));
        var finalSql = string.Format(sql, placeholders);

        using var command = new SqliteCommand(finalSql, _context.Connection);
        command.Parameters.AddWithValue("@decay", decayAmount);
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@participant_id", participantId);

        for (int i = 0; i < usedRuleIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@used{i}", usedRuleIds.ElementAt(i));
        }

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get all critique rules for a participant, ordered by strength.
    /// </summary>
    public async Task<List<CritiqueRule>> GetCritiqueRulesAsync(int participantId)
    {
        const string sql = @"
            SELECT id, participant_id, rule, bad_pattern, guidance, strength, created_at, updated_at
            FROM critique_rules 
            WHERE participant_id = @participant_id 
            ORDER BY strength DESC, created_at DESC";

        var rules = new List<CritiqueRule>();

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@participant_id", participantId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rules.Add(new CritiqueRule
            {
                Id = reader.GetInt32(0),
                ParticipantId = reader.GetInt32(1),
                Rule = reader.GetString(2),
                BadPattern = reader.GetString(3),
                Guidance = reader.GetString(4),
                Strength = Math.Round(reader.GetDouble(5), 2),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7))
            });
        }

        return rules;
    }

    /// <summary>
    /// Generate critique guidance text for a participant's prompt.
    /// </summary>
    public async Task<string> GenerateCritiqueGuidanceAsync(int participantId, int maxTokens = 200)
    {
        var rules = await GetCritiqueRulesAsync(participantId);
        
        if (rules.Count == 0)
            return string.Empty;

        // Filter rules by strength and summarize if needed
        var significantRules = rules.Where(r => r.Strength >= 0.3).ToList();
        
        if (significantRules.Count == 0)
            return string.Empty;

        var guidance = string.Join("\n", significantRules
            .Take(5) // Limit to top 5 rules
            .Select(r => $"- {r.Guidance} (strength: {r.Strength:F2})"));

        // Simple token estimation and truncation
        var estimatedTokens = guidance.Length / 4;
        if (estimatedTokens > maxTokens)
        {
            var maxChars = maxTokens * 4;
            guidance = guidance.Substring(0, Math.Min(maxChars, guidance.Length));
            
            // Truncate to last complete sentence
            var lastPeriod = guidance.LastIndexOf('.');
            if (lastPeriod > 0)
            {
                guidance = guidance.Substring(0, lastPeriod + 1);
            }
        }

        return guidance;
    }

    private CritiqueRule? FindSimilarRule(List<CritiqueRule> existingRules, string newRule)
    {
        if (existingRules.Count == 0)
            return null;

        try
        {
            // Create input data for ML.NET
            var inputData = new List<TextData>();
            inputData.Add(new TextData { Text = newRule }); // New rule first
            inputData.AddRange(existingRules.Select(r => new TextData { Text = r.Rule }));

            var dataView = _mlContext.Data.LoadFromEnumerable(inputData);

            // Create pipeline for text featurization with bigrams
            var pipeline = _mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: nameof(TextData.Text));

            var transformer = pipeline.Fit(dataView);
            var transformedData = transformer.Transform(dataView);

            // Extract feature vectors
            var features = _mlContext.Data.CreateEnumerable<TextFeatures>(transformedData, false).ToArray();
            
            if (features.Length < 2)
                return null;

            var newRuleFeatures = features[0].Features;

            // Calculate cosine similarity with each existing rule
            for (int i = 1; i < features.Length; i++)
            {
                var similarity = CalculateCosineSimilarity(newRuleFeatures, features[i].Features);
                var roundedSimilarity = Math.Round(similarity, 2);

                if (roundedSimilarity >= 0.80) // Threshold from PRD
                {
                    return existingRules[i - 1]; // Offset by 1 since new rule is first
                }
            }
        }
        catch (Exception)
        {
            // If ML.NET fails, fall back to simple string comparison
            foreach (var rule in existingRules)
            {
                if (string.Equals(rule.Rule, newRule, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
        }

        return null;
    }

    private static double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0;

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private async Task MergeRuleAsync(CritiqueRule existingRule, string newRule, string newGuidance)
    {
        // Increase strength and update guidance
        var newStrength = Math.Round(Math.Min(1.0, existingRule.Strength + 0.1), 2);
        var combinedGuidance = $"{existingRule.Guidance}; {newGuidance}";

        const string sql = @"
            UPDATE critique_rules 
            SET strength = @strength, 
                guidance = @guidance, 
                updated_at = @updated_at
            WHERE id = @id";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@strength", newStrength);
        command.Parameters.AddWithValue("@guidance", combinedGuidance);
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@id", existingRule.Id);

        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateNewRuleAsync(int participantId, string rule, string badPattern, string guidance)
    {
        const string sql = @"
            INSERT INTO critique_rules (participant_id, rule, bad_pattern, guidance, strength, created_at, updated_at)
            VALUES (@participant_id, @rule, @bad_pattern, @guidance, @strength, @created_at, @updated_at)";

        using var command = new SqliteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@participant_id", participantId);
        command.Parameters.AddWithValue("@rule", rule);
        command.Parameters.AddWithValue("@bad_pattern", badPattern);
        command.Parameters.AddWithValue("@guidance", guidance);
        command.Parameters.AddWithValue("@strength", 0.7); // Default strength from PRD
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        await command.ExecuteNonQueryAsync();
    }

    private sealed class TextData
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class TextFeatures
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }
}
