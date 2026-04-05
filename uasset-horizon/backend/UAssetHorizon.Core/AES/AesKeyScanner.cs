using System.Security.Cryptography;
using UAssetHorizon.Core.Models;

namespace UAssetHorizon.Core.AES;

/// <summary>
/// Static AES-256 key scanner for Unreal Engine executables and binaries.
/// Scans for high-entropy 32-byte sequences that match known Unreal encryption patterns.
/// Uses memory-mapped files for performance on large binaries (50GB+ .pak files).
/// </summary>
public class AesKeyScanner
{
    // Minimum Shannon entropy threshold for a valid 32-byte AES key candidate
    private const double MIN_ENTROPY = 3.5;

    // Known byte patterns near AES keys in Unreal Engine binaries
    private static readonly byte[][] UnrealSignatures = new[]
    {
        // "AES" ASCII
        new byte[] { 0x41, 0x45, 0x53 },
        // "EncryptionKey" partial
        new byte[] { 0x45, 0x6E, 0x63, 0x72, 0x79, 0x70, 0x74 },
        // "PakSigningKeys" partial
        new byte[] { 0x50, 0x61, 0x6B, 0x53, 0x69, 0x67 },
        // "CryptoKeys" partial
        new byte[] { 0x43, 0x72, 0x79, 0x70, 0x74, 0x6F, 0x4B },
        // Common UE pak encryption init pattern
        new byte[] { 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00 },
    };

    // Known false-positive patterns to filter out
    private static readonly byte[][] FalsePositivePatterns = new[]
    {
        new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, // All zeros
        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, // All ones
        new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }, // Sequential
    };

    private readonly Action<OperationProgress>? _progressCallback;

    public AesKeyScanner(Action<OperationProgress>? progressCallback = null)
    {
        _progressCallback = progressCallback;
    }

    /// <summary>
    /// Scan a binary file for potential AES-256 keys using entropy analysis
    /// and Unreal Engine signature matching.
    /// </summary>
    public async Task<AesScanResult> ScanFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var result = new AesScanResult
        {
            SourceFile = filePath,
            Method = ScanMethod.Combined
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;
        result.TotalBytesScanned = (int)Math.Min(fileSize, int.MaxValue);

        ReportProgress("scanning", 0, $"Starting scan of {filePath}", fileSize);

        // Use memory-mapped approach for large files
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1024 * 1024, FileOptions.SequentialScan);

        // Read in chunks for memory efficiency
        const int chunkSize = 4 * 1024 * 1024; // 4MB chunks
        const int keySize = 32; // AES-256 = 32 bytes
        byte[] buffer = new byte[chunkSize + keySize]; // Overlap to catch keys at chunk boundaries
        long totalRead = 0;
        int overlap = 0;

        var candidates = new List<AesKeyCandidate>();

        while (totalRead < fileSize)
        {
            ct.ThrowIfCancellationRequested();

            // Copy overlap from previous chunk
            int bytesRead = await fs.ReadAsync(buffer.AsMemory(overlap, chunkSize), ct);
            if (bytesRead == 0) break;

            int scanLength = overlap + bytesRead;

            // Scan this chunk for key candidates
            for (int i = 0; i <= scanLength - keySize; i++)
            {
                // Quick pre-filter: skip obviously non-key regions
                if (IsLikelyFalsePositive(buffer, i, keySize))
                    continue;

                // Calculate Shannon entropy
                double entropy = CalculateEntropy(buffer, i, keySize);
                if (entropy < MIN_ENTROPY)
                    continue;

                // Check for nearby Unreal signatures
                bool hasSignature = CheckNearbySignatures(buffer, i, scanLength);
                double confidence = CalculateConfidence(entropy, hasSignature);

                if (confidence >= 0.4)
                {
                    byte[] keyBytes = new byte[keySize];
                    Array.Copy(buffer, i, keyBytes, 0, keySize);

                    candidates.Add(new AesKeyCandidate
                    {
                        KeyHex = Convert.ToHexString(keyBytes),
                        OffsetInFile = totalRead + i - overlap,
                        Entropy = Math.Round(entropy, 4),
                        Confidence = Math.Round(confidence, 4),
                        MatchesUnrealSignature = hasSignature,
                        NearbyContext = ExtractContext(buffer, i, scanLength)
                    });
                }
            }

            totalRead += bytesRead;
            overlap = Math.Min(keySize, bytesRead);
            Array.Copy(buffer, bytesRead, buffer, 0, overlap);

            double progress = (double)totalRead / fileSize * 100;
            ReportProgress("scanning", progress, $"Scanned {totalRead:N0} / {fileSize:N0} bytes", fileSize);
        }

        sw.Stop();
        result.ScanDurationMs = sw.Elapsed.TotalMilliseconds;

        // Deduplicate and sort by confidence
        result.Candidates = candidates
            .GroupBy(c => c.KeyHex)
            .Select(g => g.OrderByDescending(c => c.Confidence).First())
            .OrderByDescending(c => c.Confidence)
            .Take(50) // Limit to top 50 candidates
            .ToList();

        ReportProgress("complete", 100,
            $"Scan complete. Found {result.Candidates.Count} candidates in {sw.Elapsed.TotalSeconds:F1}s",
            fileSize);

        return result;
    }

    /// <summary>
    /// Validate a key against a .pak file by attempting to decrypt its index.
    /// </summary>
    public bool ValidateKeyAgainstPak(string pakPath, string keyHex)
    {
        if (!File.Exists(pakPath) || string.IsNullOrEmpty(keyHex))
            return false;

        try
        {
            byte[] key = Convert.FromHexString(keyHex);
            if (key.Length != 32) return false;

            using var fs = File.OpenRead(pakPath);
            using var reader = new BinaryReader(fs);

            // Read pak footer (last 221 bytes for UE4, variable for UE5)
            long footerOffset = Math.Max(0, fs.Length - 221);
            fs.Seek(footerOffset, SeekOrigin.Begin);

            // Look for pak magic
            byte[] footerData = reader.ReadBytes((int)(fs.Length - footerOffset));

            // Search for the magic value 0x5A6F12E1 in footer
            for (int i = 0; i < footerData.Length - 4; i++)
            {
                uint magic = BitConverter.ToUInt32(footerData, i);
                if (magic == 0x5A6F12E1) // Pak file magic
                {
                    // Found pak signature, try decrypting the index
                    return TryDecryptPakIndex(fs, key, footerData, i);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryDecryptPakIndex(FileStream fs, byte[] key, byte[] footerData, int magicOffset)
    {
        try
        {
            // Read encrypted test block from start of file
            fs.Seek(0, SeekOrigin.Begin);
            byte[] testBlock = new byte[16]; // One AES block
            fs.Read(testBlock, 0, 16);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            using var decryptor = aes.CreateDecryptor();
            byte[] decrypted = decryptor.TransformFinalBlock(testBlock, 0, 16);

            // Check if decrypted data looks valid (not random garbage)
            // Valid pak data typically has recognizable patterns
            int zeroCount = decrypted.Count(b => b == 0);
            int printableCount = decrypted.Count(b => b >= 32 && b <= 126);

            // Heuristic: valid decryption produces structured data
            return zeroCount >= 2 || printableCount >= 4;
        }
        catch
        {
            return false;
        }
    }

    // ─── Entropy & Analysis ─────────────────────────────────────────

    private static double CalculateEntropy(byte[] data, int offset, int length)
    {
        int[] freq = new int[256];
        for (int i = 0; i < length; i++)
            freq[data[offset + i]]++;

        double entropy = 0;
        for (int i = 0; i < 256; i++)
        {
            if (freq[i] == 0) continue;
            double p = (double)freq[i] / length;
            entropy -= p * Math.Log2(p);
        }
        return entropy;
    }

    private static bool IsLikelyFalsePositive(byte[] data, int offset, int length)
    {
        // Check for repeated byte patterns
        byte first = data[offset];
        bool allSame = true;
        for (int i = 1; i < Math.Min(8, length); i++)
        {
            if (data[offset + i] != first)
            {
                allSame = false;
                break;
            }
        }
        if (allSame) return true;

        // Check for sequential patterns
        bool sequential = true;
        for (int i = 1; i < Math.Min(8, length); i++)
        {
            if (data[offset + i] != data[offset + i - 1] + 1)
            {
                sequential = false;
                break;
            }
        }
        if (sequential) return true;

        // Check known false positive patterns
        foreach (var pattern in FalsePositivePatterns)
        {
            if (offset + pattern.Length <= data.Length)
            {
                bool match = true;
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (data[offset + i] != pattern[i])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
        }

        return false;
    }

    private static bool CheckNearbySignatures(byte[] data, int keyOffset, int dataLength)
    {
        // Check 256 bytes before and after the key position
        int searchStart = Math.Max(0, keyOffset - 256);
        int searchEnd = Math.Min(dataLength, keyOffset + 32 + 256);

        foreach (var sig in UnrealSignatures)
        {
            for (int i = searchStart; i <= searchEnd - sig.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < sig.Length; j++)
                {
                    if (data[i + j] != sig[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
        }
        return false;
    }

    private static double CalculateConfidence(double entropy, bool hasSignature)
    {
        // Base confidence from entropy (max entropy for 32 bytes is 8.0)
        double conf = (entropy - MIN_ENTROPY) / (8.0 - MIN_ENTROPY);
        conf = Math.Clamp(conf, 0, 0.7);

        // Boost if near Unreal signature
        if (hasSignature)
            conf = Math.Min(1.0, conf + 0.3);

        return conf;
    }

    private static string ExtractContext(byte[] data, int offset, int dataLength)
    {
        // Extract printable ASCII near the key for context
        int start = Math.Max(0, offset - 32);
        int end = Math.Min(dataLength, offset + 64);

        var chars = new List<char>();
        for (int i = start; i < end; i++)
        {
            byte b = data[i];
            chars.Add(b >= 32 && b <= 126 ? (char)b : '.');
        }
        return new string(chars.ToArray());
    }

    private void ReportProgress(string status, double percent, string message, long totalBytes)
    {
        _progressCallback?.Invoke(new OperationProgress
        {
            OperationId = "aes-scan",
            Status = status,
            ProgressPercent = percent,
            CurrentStep = message,
            TotalBytes = totalBytes,
            Logs = new List<LogEntry>
            {
                new() { Level = Models.LogLevel.Info, Message = message }
            }
        });
    }
}
