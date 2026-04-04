using UAssetHorizon.Core.Models;

namespace UAssetHorizon.Core.Parsers;

/// <summary>
/// Detects Unreal Engine version from .uasset file headers.
/// The magic number is always 0xC1832A9E (little-endian), followed by version fields.
/// </summary>
public static class VersionDetector
{
    // Unreal package magic: 0x9E2A83C1 stored little-endian
    private const uint UASSET_MAGIC = 0x9E2A83C1;

    // UE4/5 package file version ranges
    private const int VER_UE4_OLDEST = 214;
    private const int VER_UE4_0 = 342;
    private const int VER_UE4_10 = 390;
    private const int VER_UE4_20 = 513;
    private const int VER_UE4_25 = 518;
    private const int VER_UE4_26 = 522;
    private const int VER_UE4_27 = 522; // Same as 4.26 but with licensee diff
    private const int VER_UE5_0 = 1000;
    private const int VER_UE5_1 = 1002;
    private const int VER_UE5_2 = 1003;
    private const int VER_UE5_3 = 1004;
    private const int VER_UE5_4 = 1005;

    /// <summary>
    /// Detect the UE version from a raw file byte array.
    /// Reads the package header to determine engine version, flags, etc.
    /// </summary>
    public static VersionInfo Detect(byte[] data)
    {
        if (data.Length < 32)
            return new VersionInfo { Version = UnrealVersion.Unknown };

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var info = new VersionInfo();

        // Read magic number
        uint magic = reader.ReadUInt32();
        if (magic != UASSET_MAGIC)
        {
            // Could be a compressed or encrypted file
            info.Version = UnrealVersion.Unknown;
            info.IsEncrypted = true;
            return info;
        }

        // Legacy version (negative for UE4+)
        int legacyVersion = reader.ReadInt32();

        if (legacyVersion >= 0)
        {
            // UE3 format: positive legacy version
            info.Version = UnrealVersion.UE3;
            info.PackageFileVersion = legacyVersion;
            return info;
        }

        // UE4+ format
        if (legacyVersion != -4)
        {
            // Skip legacy UE3 version
            reader.ReadInt32();
        }

        // File version UE4
        int fileVersionUE4 = reader.ReadInt32();
        info.PackageFileVersion = fileVersionUE4;

        // File version UE5 (if present in newer formats)
        int fileVersionUE5 = 0;
        if (legacyVersion <= -8)
        {
            fileVersionUE5 = reader.ReadInt32();
        }

        // Licensee file version
        info.LicenseeVersion = reader.ReadInt32();

        // Custom versions
        if (legacyVersion <= -2)
        {
            int customVersionCount = reader.ReadInt32();
            info.CustomVersionCount = customVersionCount;

            // Skip custom version entries (each is GUID + version number = 20 bytes)
            if (customVersionCount > 0 && customVersionCount < 1000)
            {
                reader.BaseStream.Seek(customVersionCount * 20, SeekOrigin.Current);
            }
        }

        // Determine the version
        info.Version = ClassifyVersion(fileVersionUE4, fileVersionUE5, legacyVersion);

        // Try to read package flags
        if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
        {
            uint packageFlags = reader.ReadUInt32();
            info.IsEncrypted = (packageFlags & 0x00000008) != 0; // PKG_Encrypted
            info.IsCompressed = (packageFlags & 0x02000000) != 0; // PKG_StoreCompressed
            info.PackageFlags = $"0x{packageFlags:X8}";
        }

        return info;
    }

    /// <summary>
    /// Detect version from a file path.
    /// </summary>
    public static VersionInfo DetectFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Asset file not found: {filePath}");

        // Read only the header (first 256 bytes is plenty)
        byte[] header = new byte[Math.Min(256, new FileInfo(filePath).Length)];
        using var fs = File.OpenRead(filePath);
        fs.Read(header, 0, header.Length);

        return Detect(header);
    }

    private static UnrealVersion ClassifyVersion(int ue4Ver, int ue5Ver, int legacy)
    {
        // UE5 versions
        if (ue5Ver >= VER_UE5_4) return UnrealVersion.UE5_4;
        if (ue5Ver >= VER_UE5_3) return UnrealVersion.UE5_3;
        if (ue5Ver >= VER_UE5_2) return UnrealVersion.UE5_2;
        if (ue5Ver >= VER_UE5_1) return UnrealVersion.UE5_1;
        if (ue5Ver >= VER_UE5_0) return UnrealVersion.UE5_0;

        // UE4 versions
        if (ue4Ver >= VER_UE4_26) return UnrealVersion.UE4_27;
        if (ue4Ver >= VER_UE4_25) return UnrealVersion.UE4_25;
        if (ue4Ver >= VER_UE4_20) return UnrealVersion.UE4_20;
        if (ue4Ver >= VER_UE4_10) return UnrealVersion.UE4_10;
        if (ue4Ver >= VER_UE4_0) return UnrealVersion.UE4_0;
        if (ue4Ver >= VER_UE4_OLDEST) return UnrealVersion.UE4_0;

        return UnrealVersion.Unknown;
    }
}
