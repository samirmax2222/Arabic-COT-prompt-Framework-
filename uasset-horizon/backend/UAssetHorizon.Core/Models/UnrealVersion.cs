namespace UAssetHorizon.Core.Models;

/// <summary>
/// Supported Unreal Engine version ranges for asset parsing.
/// Each version has different serialization formats and features.
/// </summary>
public enum UnrealVersion
{
    UE3 = 3,
    UE4_0 = 400,
    UE4_10 = 410,
    UE4_20 = 420,
    UE4_25 = 425,
    UE4_26 = 426,
    UE4_27 = 427,
    UE5_0 = 500,
    UE5_1 = 501,
    UE5_2 = 502,
    UE5_3 = 503,
    UE5_4 = 504,
    UE5_5 = 505,
    UE5_6 = 506,
    Unknown = -1
}

/// <summary>
/// Describes the detected engine version and package details from a uasset header.
/// </summary>
public class VersionInfo
{
    public UnrealVersion Version { get; set; } = UnrealVersion.Unknown;
    public int PackageFileVersion { get; set; }
    public int LicenseeVersion { get; set; }
    public int CustomVersionCount { get; set; }
    public string EngineVersionString { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public bool IsCompressed { get; set; }
    public string PackageFlags { get; set; } = string.Empty;
}
