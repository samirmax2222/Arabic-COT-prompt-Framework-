using Microsoft.AspNetCore.Mvc;
using UAssetHorizon.Core.Parsers;

namespace UAssetHorizon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetController : ControllerBase
{
    private readonly UniversalAssetParser _parser;

    public AssetController(UniversalAssetParser parser)
    {
        _parser = parser;
    }

    /// <summary>
    /// Parse a .uasset file and return the unified asset model.
    /// </summary>
    [HttpPost("parse")]
    public IActionResult Parse([FromBody] ParseRequest request)
    {
        if (string.IsNullOrEmpty(request.FilePath))
            return BadRequest(new { error = "filePath is required" });

        if (!System.IO.File.Exists(request.FilePath))
            return NotFound(new { error = $"File not found: {request.FilePath}" });

        var result = _parser.Parse(request.FilePath, request.UexpPath);
        return Ok(result);
    }

    /// <summary>
    /// Detect the UE version of a .uasset file.
    /// </summary>
    [HttpPost("detect-version")]
    public IActionResult DetectVersion([FromBody] ParseRequest request)
    {
        if (string.IsNullOrEmpty(request.FilePath))
            return BadRequest(new { error = "filePath is required" });

        var info = VersionDetector.DetectFromFile(request.FilePath);
        return Ok(info);
    }

    /// <summary>
    /// Browse a directory and list all asset files.
    /// </summary>
    [HttpPost("browse")]
    public IActionResult Browse([FromBody] BrowseRequest request)
    {
        if (string.IsNullOrEmpty(request.DirectoryPath))
            return BadRequest(new { error = "directoryPath is required" });

        if (!Directory.Exists(request.DirectoryPath))
            return NotFound(new { error = $"Directory not found: {request.DirectoryPath}" });

        var extensions = new[] { ".uasset", ".uexp", ".ubulk", ".umap", ".pak", ".ucas", ".utoc" };

        var files = Directory.EnumerateFiles(request.DirectoryPath,
                "*.*", request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => new
            {
                path = f,
                name = Path.GetFileName(f),
                extension = Path.GetExtension(f),
                size = new FileInfo(f).Length,
                directory = Path.GetDirectoryName(f)
            })
            .Take(5000)
            .ToList();

        return Ok(new { count = files.Count, files });
    }
}

public record ParseRequest(string FilePath, string? UexpPath = null);
public record BrowseRequest(string DirectoryPath, bool Recursive = true);
