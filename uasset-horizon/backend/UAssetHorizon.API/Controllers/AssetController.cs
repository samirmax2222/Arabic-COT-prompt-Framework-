using Microsoft.AspNetCore.Mvc;
using UAssetHorizon.API.Security;
using UAssetHorizon.Core.Parsers;

namespace UAssetHorizon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetController : ControllerBase
{
    private readonly UniversalAssetParser _parser;
    private readonly PathSanitizer _pathSanitizer;

    public AssetController(UniversalAssetParser parser, PathSanitizer pathSanitizer)
    {
        _parser = parser;
        _pathSanitizer = pathSanitizer;
    }

    /// <summary>
    /// Parse a .uasset file and return the unified asset model.
    /// </summary>
    [HttpPost("parse")]
    public IActionResult Parse([FromBody] ParseRequest request)
    {
        var (safePath, error) = _pathSanitizer.Validate(request.FilePath);
        if (safePath is null)
            return BadRequest(new { error });

        if (!System.IO.File.Exists(safePath))
            return NotFound(new { error = $"File not found: {safePath}" });

        // Also validate optional uexp companion path
        string? safeUexpPath = null;
        if (!string.IsNullOrEmpty(request.UexpPath))
        {
            var (uexp, uexpErr) = _pathSanitizer.Validate(request.UexpPath);
            if (uexp is null)
                return BadRequest(new { error = uexpErr });
            safeUexpPath = uexp;
        }

        var result = _parser.Parse(safePath, safeUexpPath);
        return Ok(result);
    }

    /// <summary>
    /// Detect the UE version of a .uasset file.
    /// </summary>
    [HttpPost("detect-version")]
    public IActionResult DetectVersion([FromBody] ParseRequest request)
    {
        var (safePath, error) = _pathSanitizer.Validate(request.FilePath);
        if (safePath is null)
            return BadRequest(new { error });

        var info = VersionDetector.DetectFromFile(safePath);
        return Ok(info);
    }

    /// <summary>
    /// Browse a directory and list all asset files.
    /// </summary>
    [HttpPost("browse")]
    public IActionResult Browse([FromBody] BrowseRequest request)
    {
        var (safePath, error) = _pathSanitizer.Validate(request.DirectoryPath);
        if (safePath is null)
            return BadRequest(new { error });

        if (!Directory.Exists(safePath))
            return NotFound(new { error = $"Directory not found: {safePath}" });

        var extensions = new[] { ".uasset", ".uexp", ".ubulk", ".umap", ".pak", ".ucas", ".utoc" };

        var files = Directory.EnumerateFiles(safePath,
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
