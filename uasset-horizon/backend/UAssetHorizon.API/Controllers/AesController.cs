using Microsoft.AspNetCore.Mvc;
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

    public AesController(AesKeyScanner scanner, AesKeyDatabase keyDb, PakDecryptor decryptor)
    {
        _scanner = scanner;
        _keyDb = keyDb;
        _decryptor = decryptor;
    }

    /// <summary>
    /// Scan a binary file for AES key candidates.
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        if (string.IsNullOrEmpty(request.FilePath))
            return BadRequest(new { error = "filePath is required" });

        if (!System.IO.File.Exists(request.FilePath))
            return NotFound(new { error = $"File not found: {request.FilePath}" });

        var result = await _scanner.ScanFileAsync(request.FilePath);
        return Ok(result);
    }

    /// <summary>
    /// Validate a key against a .pak file.
    /// </summary>
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateRequest request)
    {
        if (string.IsNullOrEmpty(request.PakPath) || string.IsNullOrEmpty(request.KeyHex))
            return BadRequest(new { error = "pakPath and keyHex are required" });

        bool valid = _scanner.ValidateKeyAgainstPak(request.PakPath, request.KeyHex);
        return Ok(new { valid, pakPath = request.PakPath });
    }

    /// <summary>
    /// Decrypt a pak file and extract contents.
    /// </summary>
    [HttpPost("decrypt")]
    public async Task<IActionResult> Decrypt([FromBody] DecryptRequest request)
    {
        if (string.IsNullOrEmpty(request.PakPath) || string.IsNullOrEmpty(request.KeyHex))
            return BadRequest(new { error = "pakPath and keyHex are required" });

        string outputDir = request.OutputDir ?? Path.Combine(
            Path.GetDirectoryName(request.PakPath) ?? ".",
            Path.GetFileNameWithoutExtension(request.PakPath) + "_extracted");

        var result = await _decryptor.DecryptPakAsync(request.PakPath, request.KeyHex, outputDir);
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
