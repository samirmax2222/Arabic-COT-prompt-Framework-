namespace UAssetHorizon.API.Security;

/// <summary>
/// Validates that user-supplied filesystem paths resolve within an allowed
/// base directory, preventing path-traversal attacks via the REST API.
/// The allowed root defaults to the user's home folder but can be overridden
/// with the UASSET_HORIZON_BASE_DIR environment variable or
/// "Security:AllowedBaseDirectory" in appsettings.
/// </summary>
public sealed class PathSanitizer
{
    private readonly string _allowedBaseDir;

    public PathSanitizer(IConfiguration configuration)
    {
        // Precedence: env var > appsettings > user home directory
        var configured = Environment.GetEnvironmentVariable("UASSET_HORIZON_BASE_DIR")
            ?? configuration.GetValue<string>("Security:AllowedBaseDirectory");

        _allowedBaseDir = Path.GetFullPath(
            !string.IsNullOrWhiteSpace(configured)
                ? configured
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    /// <summary>
    /// Resolve <paramref name="userPath"/> to its full, canonical form and
    /// verify it lives under the allowed base directory.
    /// Returns the resolved path on success, or an error message on failure.
    /// </summary>
    public (string? SafePath, string? Error) Validate(string? userPath)
    {
        if (string.IsNullOrWhiteSpace(userPath))
            return (null, "Path must not be empty.");

        string resolved;
        try
        {
            resolved = Path.GetFullPath(userPath);
        }
        catch (Exception ex)
        {
            return (null, $"Invalid path: {ex.Message}");
        }

        // Ensure the resolved path starts with the allowed directory.
        // Append a separator so that "/home/user2" is not accepted when
        // the base is "/home/user".
        var baseDirWithSep = _allowedBaseDir.TrimEnd(Path.DirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(baseDirWithSep, StringComparison.OrdinalIgnoreCase)
            && !resolved.Equals(_allowedBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"Access denied: path is outside the allowed directory ({_allowedBaseDir}).");
        }

        return (resolved, null);
    }
}
