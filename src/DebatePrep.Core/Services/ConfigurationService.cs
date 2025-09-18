using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DebatePrep.Core.Services;

/// <summary>
/// Service for managing application configuration and encrypted settings.
/// </summary>
public sealed class ConfigurationService
{
    private readonly string _configPath;
    private AppConfiguration _configuration;

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "DebatePrep");
        Directory.CreateDirectory(appFolder);
        
        _configPath = Path.Combine(appFolder, "config.json");
        _configuration = LoadConfiguration();
    }

    /// <summary>
    /// Get the current configuration.
    /// </summary>
    public AppConfiguration Configuration => _configuration;

    /// <summary>
    /// Set the API key for a provider (encrypted storage).
    /// </summary>
    public void SetApiKey(string providerName, string apiKey)
    {
        var encryptedKey = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(apiKey),
            null,
            DataProtectionScope.CurrentUser);

        _configuration.EncryptedApiKeys[providerName] = Convert.ToBase64String(encryptedKey);
        SaveConfiguration();
    }

    /// <summary>
    /// Get the decrypted API key for a provider.
    /// </summary>
    public string? GetApiKey(string providerName)
    {
        if (!_configuration.EncryptedApiKeys.TryGetValue(providerName, out var encryptedBase64))
            return null;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException)
        {
            // Key was encrypted by different user or corrupted
            return null;
        }
    }

    /// <summary>
    /// Set the current model provider and model.
    /// </summary>
    public void SetCurrentModel(string providerName, string modelId)
    {
        _configuration.CurrentProvider = providerName;
        _configuration.CurrentModel = modelId;
        SaveConfiguration();
    }

    /// <summary>
    /// Update generation parameters.
    /// </summary>
    public void SetGenerationParameters(double temperature, int maxTokens, double topP)
    {
        _configuration.Temperature = Math.Round(temperature, 2);
        _configuration.MaxTokens = maxTokens;
        _configuration.TopP = Math.Round(topP, 2);
        SaveConfiguration();
    }

    private AppConfiguration LoadConfiguration()
    {
        if (!File.Exists(_configPath))
        {
            return new AppConfiguration();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
        }
        catch (Exception)
        {
            // If config is corrupted, start fresh
            return new AppConfiguration();
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception)
        {
            // Silently fail to avoid crashes, but log in real implementation
        }
    }
}

/// <summary>
/// Application configuration model.
/// </summary>
public sealed class AppConfiguration
{
    public string CurrentProvider { get; set; } = string.Empty;
    public string CurrentModel { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public double TopP { get; set; } = 0.9;
    public Dictionary<string, string> EncryptedApiKeys { get; set; } = new();
    public string DatabasePath { get; set; } = string.Empty;
    public bool EnableAutoSave { get; set; } = true;
    public int AutoSaveIntervalMs { get; set; } = 2000;
}
