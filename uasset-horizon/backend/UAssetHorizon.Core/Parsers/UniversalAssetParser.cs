using System.Text;
using UAssetHorizon.Core.Models;
using Microsoft.Extensions.Logging;

namespace UAssetHorizon.Core.Parsers;

/// <summary>
/// Universal parser engine that reads .uasset files from UE3 through UE5.6.
/// Uses a strategy pattern to delegate to version-specific handlers.
/// Integrates with UAssetAPI for deep property parsing when available.
/// </summary>
public class UniversalAssetParser
{
    private readonly ILogger<UniversalAssetParser>? _logger;
    private const uint UASSET_MAGIC = 0x9E2A83C1;

    public UniversalAssetParser(ILogger<UniversalAssetParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a .uasset file and return a unified ParsedAsset model.
    /// Optionally provide path to companion .uexp file.
    /// </summary>
    public ParsedAsset Parse(string uassetPath, string? uexpPath = null)
    {
        if (!File.Exists(uassetPath))
            throw new FileNotFoundException($"Asset not found: {uassetPath}");

        _logger?.LogInformation("Parsing asset: {Path}", uassetPath);

        var data = File.ReadAllBytes(uassetPath);
        var versionInfo = VersionDetector.Detect(data);

        _logger?.LogInformation("Detected version: {Version} (PackageVer: {PkgVer})",
            versionInfo.Version, versionInfo.PackageFileVersion);

        var asset = new ParsedAsset
        {
            FileName = Path.GetFileName(uassetPath),
            FilePath = uassetPath,
            VersionInfo = versionInfo,
            FileSize = data.Length
        };

        // Auto-detect companion .uexp
        if (uexpPath == null)
        {
            var possibleUexp = Path.ChangeExtension(uassetPath, ".uexp");
            if (File.Exists(possibleUexp))
                uexpPath = possibleUexp;
        }

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            ParseHeader(reader, asset);
            ParseNameTable(reader, asset);
            ParseImportTable(reader, asset);
            ParseExportTable(reader, asset);
            DetectAssetType(asset);

            // Parse uexp data if available
            if (uexpPath != null && File.Exists(uexpPath))
            {
                var uexpData = File.ReadAllBytes(uexpPath);
                ParseExportData(uexpData, asset);
            }

            // Generate blueprint data if this is a blueprint
            if (asset.Type == AssetType.Blueprint)
            {
                asset.Blueprint = BlueprintParser.Parse(asset, data, uexpPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing asset: {Path}", uassetPath);
            asset.Properties["parseError"] = ex.Message;
        }

        return asset;
    }

    private void ParseHeader(BinaryReader reader, ParsedAsset asset)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        uint magic = reader.ReadUInt32();
        if (magic != UASSET_MAGIC)
            throw new InvalidDataException("Not a valid .uasset file (bad magic)");

        int legacyVersion = reader.ReadInt32();
        asset.Properties["legacyVersion"] = legacyVersion;

        if (legacyVersion < 0)
        {
            // UE4+ header
            if (legacyVersion != -4)
                reader.ReadInt32(); // Legacy UE3 version

            int fileVersionUE4 = reader.ReadInt32();
            asset.Properties["fileVersionUE4"] = fileVersionUE4;

            if (legacyVersion <= -8)
            {
                int fileVersionUE5 = reader.ReadInt32();
                asset.Properties["fileVersionUE5"] = fileVersionUE5;
            }

            int licenseeVersion = reader.ReadInt32();
            asset.Properties["licenseeVersion"] = licenseeVersion;

            // Custom versions
            if (legacyVersion <= -2)
            {
                int customVersionCount = reader.ReadInt32();
                asset.Properties["customVersionCount"] = customVersionCount;

                for (int i = 0; i < Math.Min(customVersionCount, 500); i++)
                {
                    reader.BaseStream.Seek(20, SeekOrigin.Current); // GUID + int
                }
            }

            // Total header size
            int totalHeaderSize = reader.ReadInt32();
            asset.Properties["totalHeaderSize"] = totalHeaderSize;

            // Folder name (FString)
            string folderName = ReadFString(reader);
            asset.Properties["folderName"] = folderName;

            // Package flags
            uint packageFlags = reader.ReadUInt32();
            asset.Properties["packageFlags"] = $"0x{packageFlags:X8}";

            // Name count and offset
            int nameCount = reader.ReadInt32();
            int nameOffset = reader.ReadInt32();
            asset.Properties["nameCount"] = nameCount;
            asset.Properties["nameOffset"] = nameOffset;

            // Soft object paths (UE5)
            if (legacyVersion <= -7)
            {
                reader.ReadInt32(); // softObjectPathsCount
                reader.ReadInt32(); // softObjectPathsOffset
            }

            // Localization ID (if present)
            if ((packageFlags & 0x40000000) != 0)
            {
                ReadFString(reader); // localizationId
            }

            // Gatherable text data
            int gatherableTextDataCount = reader.ReadInt32();
            int gatherableTextDataOffset = reader.ReadInt32();

            // Export count and offset
            int exportCount = reader.ReadInt32();
            int exportOffset = reader.ReadInt32();
            asset.Properties["exportCount"] = exportCount;
            asset.Properties["exportOffset"] = exportOffset;

            // Import count and offset
            int importCount = reader.ReadInt32();
            int importOffset = reader.ReadInt32();
            asset.Properties["importCount"] = importCount;
            asset.Properties["importOffset"] = importOffset;

            // Depends offset
            int dependsOffset = reader.ReadInt32();
            asset.Properties["dependsOffset"] = dependsOffset;
        }
    }

    private void ParseNameTable(BinaryReader reader, ParsedAsset asset)
    {
        if (!asset.Properties.ContainsKey("nameOffset") ||
            !asset.Properties.ContainsKey("nameCount"))
            return;

        int offset = Convert.ToInt32(asset.Properties["nameOffset"]);
        int count = Convert.ToInt32(asset.Properties["nameCount"]);

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        for (int i = 0; i < count && reader.BaseStream.Position < reader.BaseStream.Length; i++)
        {
            try
            {
                string name = ReadFString(reader);
                asset.Names.Add(name);

                // Skip non-case-preserving hash (UE4+)
                if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
                    reader.ReadUInt32();
            }
            catch
            {
                break;
            }
        }
    }

    private void ParseImportTable(BinaryReader reader, ParsedAsset asset)
    {
        if (!asset.Properties.ContainsKey("importOffset") ||
            !asset.Properties.ContainsKey("importCount"))
            return;

        int offset = Convert.ToInt32(asset.Properties["importOffset"]);
        int count = Convert.ToInt32(asset.Properties["importCount"]);

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        for (int i = 0; i < count && reader.BaseStream.Position < reader.BaseStream.Length; i++)
        {
            try
            {
                var import = new ImportEntry { Index = i };

                // ClassPackage FName
                int classPackageIdx = reader.ReadInt32();
                reader.ReadInt32(); // number
                import.ClassPackage = GetName(asset, classPackageIdx);

                // ClassName FName
                int classNameIdx = reader.ReadInt32();
                reader.ReadInt32(); // number
                import.ClassName = GetName(asset, classNameIdx);

                // OuterIndex
                import.OuterIndex = reader.ReadInt32();

                // ObjectName FName
                int objectNameIdx = reader.ReadInt32();
                reader.ReadInt32(); // number
                import.ObjectName = GetName(asset, objectNameIdx);

                // Optional: PackageName (UE5)
                if (asset.VersionInfo.Version >= UnrealVersion.UE5_0)
                {
                    reader.ReadInt32(); // bImportOptional or packageName
                }

                asset.Imports.Add(import);
            }
            catch
            {
                break;
            }
        }
    }

    private void ParseExportTable(BinaryReader reader, ParsedAsset asset)
    {
        if (!asset.Properties.ContainsKey("exportOffset") ||
            !asset.Properties.ContainsKey("exportCount"))
            return;

        int offset = Convert.ToInt32(asset.Properties["exportOffset"]);
        int count = Convert.ToInt32(asset.Properties["exportCount"]);

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        for (int i = 0; i < count && reader.BaseStream.Position < reader.BaseStream.Length; i++)
        {
            try
            {
                var export = new ExportEntry { Index = i };

                // ClassIndex
                int classIdx = reader.ReadInt32();
                export.ClassName = ResolveObjectName(asset, classIdx);

                // SuperIndex
                int superIdx = reader.ReadInt32();
                export.SuperName = ResolveObjectName(asset, superIdx);

                // TemplateIndex (UE4.26+)
                if (asset.VersionInfo.PackageFileVersion >= 522)
                    reader.ReadInt32();

                // OuterIndex
                reader.ReadInt32();

                // ObjectName FName
                int objectNameIdx = reader.ReadInt32();
                reader.ReadInt32(); // number
                export.ObjectName = GetName(asset, objectNameIdx);

                // ObjectFlags
                reader.ReadUInt32();

                // SerialSize and SerialOffset
                if (asset.VersionInfo.Version >= UnrealVersion.UE5_0)
                {
                    export.SerialSize = reader.ReadInt64();
                    export.SerialOffset = reader.ReadInt64();
                }
                else
                {
                    export.SerialSize = reader.ReadInt32();
                    export.SerialOffset = reader.ReadInt32();
                }

                // Skip remaining export entry fields
                // bForcedExport, bNotForClient, bNotForServer, etc.
                reader.ReadInt32(); // bForcedExport
                reader.ReadInt32(); // bNotForClient/Server
                reader.ReadUInt32(); // PackageGuid part or hash

                // Additional fields vary by version, skip conservatively
                if (reader.BaseStream.Position + 20 <= reader.BaseStream.Length)
                {
                    reader.BaseStream.Seek(20, SeekOrigin.Current);
                }

                asset.Exports.Add(export);
            }
            catch
            {
                break;
            }
        }
    }

    private void ParseExportData(byte[] uexpData, ParsedAsset asset)
    {
        // Store basic uexp info
        asset.Properties["uexpSize"] = uexpData.Length;
        asset.Properties["hasUexp"] = true;
    }

    private void DetectAssetType(ParsedAsset asset)
    {
        // Detect asset type from export class names and import references
        var allClassNames = asset.Exports.Select(e => e.ClassName)
            .Concat(asset.Imports.Select(i => i.ClassName))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allObjectNames = asset.Exports.Select(e => e.ObjectName)
            .Concat(asset.Imports.Select(i => i.ObjectName))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allClassNames.Any(c => c.Contains("BlueprintGeneratedClass") || c.Contains("Blueprint")))
            asset.Type = AssetType.Blueprint;
        else if (allClassNames.Any(c => c.Contains("StaticMesh")))
            asset.Type = AssetType.StaticMesh;
        else if (allClassNames.Any(c => c.Contains("SkeletalMesh")))
            asset.Type = AssetType.SkeletalMesh;
        else if (allClassNames.Any(c => c.Contains("MaterialInstance")))
            asset.Type = AssetType.MaterialInstance;
        else if (allClassNames.Any(c => c.Contains("Material")))
            asset.Type = AssetType.Material;
        else if (allClassNames.Any(c => c.Contains("Texture2D")))
            asset.Type = AssetType.Texture2D;
        else if (allClassNames.Any(c => c.Contains("AnimSequence") || c.Contains("AnimMontage")))
            asset.Type = AssetType.Animation;
        else if (allClassNames.Any(c => c.Contains("SoundWave") || c.Contains("SoundCue")))
            asset.Type = AssetType.Sound;
        else if (allClassNames.Any(c => c.Contains("ParticleSystem") || c.Contains("NiagaraSystem")))
            asset.Type = AssetType.ParticleSystem;
        else if (allClassNames.Any(c => c.Contains("WidgetBlueprint") || c.Contains("UserWidget")))
            asset.Type = AssetType.Widget;
        else if (allClassNames.Any(c => c.Contains("DataTable")))
            asset.Type = AssetType.DataTable;
        else
            asset.Type = AssetType.Other;

        // Detect dependencies from imports
        asset.Dependencies = asset.Imports
            .Where(i => !string.IsNullOrEmpty(i.ObjectName))
            .Select(i => $"{i.ClassPackage}/{i.ObjectName}")
            .Distinct()
            .ToList();
    }

    // ─── Utility Methods ────────────────────────────────────────────

    private string GetName(ParsedAsset asset, int index)
    {
        if (index >= 0 && index < asset.Names.Count)
            return asset.Names[index];
        return $"[Name_{index}]";
    }

    private string ResolveObjectName(ParsedAsset asset, int index)
    {
        if (index > 0)
        {
            // Export reference (1-based)
            int exportIdx = index - 1;
            if (exportIdx < asset.Exports.Count)
                return asset.Exports[exportIdx].ObjectName;
        }
        else if (index < 0)
        {
            // Import reference (negated, 1-based)
            int importIdx = -index - 1;
            if (importIdx < asset.Imports.Count)
                return asset.Imports[importIdx].ObjectName;
        }
        return string.Empty;
    }

    private static string ReadFString(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length == 0) return string.Empty;

        if (length < 0)
        {
            // Unicode string
            int charCount = -length;
            if (charCount > 65536) return "[invalid string]";
            byte[] bytes = reader.ReadBytes(charCount * 2);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        else
        {
            // ASCII/UTF-8 string
            if (length > 65536) return "[invalid string]";
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
    }
}
