using Microsoft.AspNetCore.Mvc;
using UAssetHorizon.API.Security;
using UAssetHorizon.Core.AES;
using UAssetHorizon.Core.Decryption;
using UAssetHorizon.Core.Models;

namespace UAssetHorizon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AesController : ControllerBase
{
    private readonly AesKeyScanner _scanner;
    private readonly AesKeyDatabase _keyDb;
    private readonly PakDecryptor _decryptor;
    private readonly PathSanitizer _pathSanitizer;

    public AesController(AesKeyScanner scanner, AesKeyDatabase keyDb,
        PakDecryptor decryptor, PathSanitizer pathSanitizer)
    {
        _scanner = scanner;
        _keyDb = keyDb;
        _decryptor = decryptor;
        _pathSanitizer = pathSanitizer;
    }

    /// <summary>
    /// Scan a binary file for AES key candidates.
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        var (safePath, error) = _pathSanitizer.Validate(request.FilePath);
        if (safePath is null)
            return BadRequest(new { error });

        if (!System.IO.File.Exists(safePath))
            return NotFound(new { error = $"File not found: {safePath}" });

        var result = await _scanner.ScanFileAsync(safePath);
        return Ok(result);
    }

    /// <summary>
    /// Validate a key against a .pak file.
    /// </summary>
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateRequest request)
    {
        if (string.IsNullOrEmpty(request.KeyHex))
            return BadRequest(new { error = "pakPath and keyHex are required" });

        var (safePath, error) = _pathSanitizer.Validate(request.PakPath);
        if (safePath is null)
            return BadRequest(new { error });

        bool valid = _scanner.ValidateKeyAgainstPak(safePath, request.KeyHex);
        return Ok(new { valid, pakPath = safePath });
    }

    /// <summary>
    /// Decrypt a pak file and extract contents.
    /// </summary>
    [HttpPost("decrypt")]
    public async Task<IActionResult> Decrypt([FromBody] DecryptRequest request)
    {
        if (string.IsNullOrEmpty(request.KeyHex))
            return BadRequest(new { error = "pakPath and keyHex are required" });

        var (safePakPath, pakError) = _pathSanitizer.Validate(request.PakPath);
        if (safePakPath is null)
            return BadRequest(new { error = pakError });

        string outputDir = request.OutputDir ?? Path.Combine(
            Path.GetDirectoryName(safePakPath) ?? ".",
            Path.GetFileNameWithoutExtension(safePakPath) + "_extracted");

        // Validate the output directory is also within the allowed base
        var (safeOutputDir, outError) = _pathSanitizer.Validate(outputDir);
        if (safeOutputDir is null)
            return BadRequest(new { error = outError });

        var result = await _decryptor.DecryptPakAsync(safePakPath, request.KeyHex, safeOutputDir);
        return Ok(result);
    }

    // ─── Key Database ───────────────────────────────────────────────

    [HttpGet("keys")]
    public IActionResult GetKeys()
    {
        return Ok(_keyDb.Keys);
    }

    [HttpPost("keys")]
    public async Task<IActionResult> AddKey([FromBody] AddKeyRequest request)
    {
        if (string.IsNullOrEmpty(request.KeyHex))
            return BadRequest(new { error = "keyHex is required" });

        var entry = _keyDb.AddKey(request.KeyHex, request.GameName,
            request.Source ?? KeySource.Manual, request.Notes);
        await _keyDb.SaveAsync();
        return Ok(entry);
    }

    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> DeleteKey(string id)
    {
        bool removed = _keyDb.RemoveKey(id);
        if (!removed) return NotFound();
        await _keyDb.SaveAsync();
        return Ok(new { removed = true });
    }

    [HttpPost("keys/import")]
    public async Task<IActionResult> ImportKeys([FromBody] ImportKeysRequest request)
    {
        int count = _keyDb.ImportFromJson(request.Json);
        await _keyDb.SaveAsync();
        return Ok(new { imported = count });
    }

    [HttpGet("keys/export")]
    public IActionResult ExportKeys()
    {
        return Content(_keyDb.ExportToJson(), "application/json");
    }
}

public record ScanRequest(string FilePath);
public record ValidateRequest(string PakPath, string KeyHex);
public record DecryptRequest(string PakPath, string KeyHex, string? OutputDir = null);
public record AddKeyRequest(string KeyHex, string? GameName = null, KeySource? Source = null, string? Notes = null);
public record ImportKeysRequest(string Json);
