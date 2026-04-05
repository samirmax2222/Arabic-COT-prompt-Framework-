namespace UAssetHorizon.Core.Models;

/// <summary>
/// Represents an AES-256 encryption key used for pak/ucas/utoc decryption.
/// </summary>
public class AesKeyEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string KeyHex { get; set; } = string.Empty;
    public string? GameName { get; set; }
    public string? Source { get; set; }
    public bool IsValidated { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public KeySource KeySource { get; set; } = KeySource.Manual;
}

public enum KeySource
{
    Manual,
    StaticScan,
    DynamicHook,
    Database,
    Import
}

/// <summary>
/// Result from an AES key scan operation.
/// </summary>
public class AesScanResult
{
    public List<AesKeyCandidate> Candidates { get; set; } = new();
    public int TotalBytesScanned { get; set; }
    public double ScanDurationMs { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public ScanMethod Method { get; set; }
}

public class AesKeyCandidate
{
    public string KeyHex { get; set; } = string.Empty;
    public long OffsetInFile { get; set; }
    public double Entropy { get; set; }
    public double Confidence { get; set; }
    public bool MatchesUnrealSignature { get; set; }
    public string? NearbyContext { get; set; }
}

public enum ScanMethod
{
    StaticEntropy,
    SignaturePattern,
    DynamicMemory,
    Combined
}

/// <summary>
/// Result from attempting to decrypt a pak/ucas file with a given key.
/// </summary>
public class DecryptionResult
{
    public bool Success { get; set; }
    public string KeyUsed { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int FilesExtracted { get; set; }
    public long BytesDecrypted { get; set; }
    public double DurationMs { get; set; }
    public List<ExtractedFileInfo> ExtractedFiles { get; set; } = new();
}

public class ExtractedFileInfo
{
    public string Path { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// Progress report for long-running scan/decrypt operations.
/// </summary>
public class OperationProgress
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = "idle";
    public double ProgressPercent { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public long BytesProcessed { get; set; }
    public long TotalBytes { get; set; }
    public double EstimatedRemainingMs { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = string.Empty;
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Success
}
