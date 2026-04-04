using System.Text.Json;
using UAssetHorizon.Core.Models;

namespace UAssetHorizon.Core.AES;

/// <summary>
/// Manages a persistent database of AES keys with validation history.
/// Keys are stored in an encrypted JSON file for security.
/// </summary>
public class AesKeyDatabase
{
    private readonly string _databasePath;
    private List<AesKeyEntry> _keys = new();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AesKeyDatabase(string? databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UAssetHorizon", "keys.json");
    }

    public IReadOnlyList<AesKeyEntry> Keys => _keys.AsReadOnly();

    /// <summary>
    /// Load keys from disk.
    /// </summary>
    public async Task LoadAsync()
    {
        if (!File.Exists(_databasePath))
        {
            _keys = new List<AesKeyEntry>();
            return;
        }

        var json = await File.ReadAllTextAsync(_databasePath);
        _keys = JsonSerializer.Deserialize<List<AesKeyEntry>>(json, JsonOpts) ?? new();
    }

    /// <summary>
    /// Save keys to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_keys, JsonOpts);
        await File.WriteAllTextAsync(_databasePath, json);
    }

    /// <summary>
    /// Add a new key entry. Returns the entry with generated ID.
    /// </summary>
    public AesKeyEntry AddKey(string keyHex, string? gameName = null,
        KeySource source = KeySource.Manual, string? notes = null)
    {
        // Normalize hex
        keyHex = keyHex.Replace(" ", "").Replace("0x", "").ToUpperInvariant();

        // Check for duplicate
        var existing = _keys.FirstOrDefault(k =>
            k.KeyHex.Equals(keyHex, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        var entry = new AesKeyEntry
        {
            KeyHex = keyHex,
            GameName = gameName,
            Source = source.ToString(),
            KeySource = source,
            Notes = notes,
            DiscoveredAt = DateTime.UtcNow
        };

        _keys.Add(entry);
        return entry;
    }

    /// <summary>
    /// Remove a key by ID.
    /// </summary>
    public bool RemoveKey(string id)
    {
        return _keys.RemoveAll(k => k.Id == id) > 0;
    }

    /// <summary>
    /// Mark a key as validated (successfully decrypted a file).
    /// </summary>
    public void MarkValidated(string id, bool isValid)
    {
        var key = _keys.FirstOrDefault(k => k.Id == id);
        if (key != null)
            key.IsValidated = isValid;
    }

    /// <summary>
    /// Get keys for a specific game.
    /// </summary>
    public IEnumerable<AesKeyEntry> GetKeysForGame(string gameName)
    {
        return _keys.Where(k =>
            k.GameName?.Contains(gameName, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Import keys from a JSON string (e.g., from FModel or other tools).
    /// </summary>
    public int ImportFromJson(string json)
    {
        int imported = 0;
        try
        {
            // Try parsing as array of key strings
            var keys = JsonSerializer.Deserialize<List<string>>(json);
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    AddKey(key, source: KeySource.Import);
                    imported++;
                }
            }
        }
        catch
        {
            // Try parsing as FModel format { "key": "hex" }
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        AddKey(kvp.Value, gameName: kvp.Key, source: KeySource.Import);
                        imported++;
                    }
                }
            }
            catch { /* Not a recognized format */ }
        }
        return imported;
    }

    /// <summary>
    /// Export all keys to JSON string.
    /// </summary>
    public string ExportToJson()
    {
        return JsonSerializer.Serialize(_keys, JsonOpts);
    }
}
