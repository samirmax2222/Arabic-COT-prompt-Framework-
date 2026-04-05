using System.Text.Json.Serialization;

namespace UAssetHorizon.Core.Models;

/// <summary>
/// Unified asset data model that normalizes output across all UE versions.
/// The frontend always receives this consistent schema regardless of source version.
/// </summary>
public class ParsedAsset
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public AssetType Type { get; set; } = AssetType.Unknown;
    public VersionInfo VersionInfo { get; set; } = new();
    public List<string> Names { get; set; } = new();
    public List<ImportEntry> Imports { get; set; } = new();
    public List<ExportEntry> Exports { get; set; } = new();
    public BlueprintData? Blueprint { get; set; }
    public MeshData? Mesh { get; set; }
    public MaterialData? Material { get; set; }
    public TextureData? Texture { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public long FileSize { get; set; }
    public string ParsedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetType
{
    Unknown,
    Blueprint,
    StaticMesh,
    SkeletalMesh,
    Material,
    MaterialInstance,
    Texture2D,
    Animation,
    Sound,
    ParticleSystem,
    Widget,
    DataTable,
    Level,
    Other
}

public class ImportEntry
{
    public int Index { get; set; }
    public string ClassPackage { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public int OuterIndex { get; set; }
}

public class ExportEntry
{
    public int Index { get; set; }
    public string ObjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SuperName { get; set; } = string.Empty;
    public long SerialSize { get; set; }
    public long SerialOffset { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

// ─── Blueprint Models ───────────────────────────────────────────────

public class BlueprintData
{
    public string ParentClass { get; set; } = string.Empty;
    public List<BlueprintNode> Nodes { get; set; } = new();
    public List<BlueprintEdge> Edges { get; set; } = new();
    public List<BlueprintVariable> Variables { get; set; } = new();
    public List<BlueprintFunction> Functions { get; set; } = new();
    public List<BlueprintComponent> Components { get; set; } = new();
    public List<BlueprintGraph> Graphs { get; set; } = new();
    public string? PseudoCode { get; set; }
}

public class BlueprintNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public NodeCategory Category { get; set; } = NodeCategory.Function;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<BlueprintPin> Pins { get; set; } = new();
    public bool IsCompact { get; set; }
    public bool IsPure { get; set; }
    public string? TargetClass { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NodeCategory
{
    Event,
    Function,
    FlowControl,
    Variable,
    Macro,
    Cast,
    Math,
    Conversion,
    Custom,
    Comment
}

public class BlueprintPin
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PinDirection Direction { get; set; }
    public PinType PinType { get; set; } = PinType.Exec;
    public string? DefaultValue { get; set; }
    public string? LinkedTo { get; set; }
    public bool IsHidden { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PinDirection
{
    Input,
    Output
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PinType
{
    Exec,
    Boolean,
    Byte,
    Int,
    Int64,
    Float,
    Double,
    String,
    Text,
    Name,
    Vector,
    Rotator,
    Transform,
    Color,
    Object,
    Class,
    Interface,
    Struct,
    Enum,
    Array,
    Set,
    Map,
    Delegate,
    Wildcard
}

public class BlueprintEdge
{
    public string Id { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourcePinId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetPinId { get; set; } = string.Empty;
    public PinType PinType { get; set; } = PinType.Exec;
}

public class BlueprintVariable
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsPublic { get; set; }
    public string? Category { get; set; }
    public string? Tooltip { get; set; }
}

public class BlueprintFunction
{
    public string Name { get; set; } = string.Empty;
    public bool IsEvent { get; set; }
    public bool IsPure { get; set; }
    public List<BlueprintPin> Parameters { get; set; } = new();
    public List<BlueprintPin> ReturnValues { get; set; } = new();
    public string? Description { get; set; }
}

public class BlueprintComponent
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? ParentComponent { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class BlueprintGraph
{
    public string Name { get; set; } = string.Empty;
    public string GraphType { get; set; } = string.Empty;
    public List<string> NodeIds { get; set; } = new();
}

// ─── Mesh Models ────────────────────────────────────────────────────

public class MeshData
{
    public string MeshType { get; set; } = string.Empty; // Static or Skeletal
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public int LODCount { get; set; }
    public List<MeshLOD> LODs { get; set; } = new();
    public BoundingBox Bounds { get; set; } = new();
    public List<string> MaterialSlots { get; set; } = new();
    public string? ExportedGltfPath { get; set; }
}

public class MeshLOD
{
    public int Level { get; set; }
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public float ScreenSize { get; set; }
}

public class BoundingBox
{
    public float MinX { get; set; }
    public float MinY { get; set; }
    public float MinZ { get; set; }
    public float MaxX { get; set; }
    public float MaxY { get; set; }
    public float MaxZ { get; set; }
}

// ─── Material Models ────────────────────────────────────────────────

public class MaterialData
{
    public string MaterialName { get; set; } = string.Empty;
    public string? ParentMaterial { get; set; }
    public string BlendMode { get; set; } = "Opaque";
    public string ShadingModel { get; set; } = "DefaultLit";
    public bool TwoSided { get; set; }
    public List<MaterialParameter> Parameters { get; set; } = new();
    public List<MaterialNode> Nodes { get; set; } = new();
    public List<MaterialConnection> Connections { get; set; } = new();
    public List<TextureReference> Textures { get; set; } = new();
}

public class MaterialParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Group { get; set; }
}

public class MaterialNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<MaterialPin> Inputs { get; set; } = new();
    public List<MaterialPin> Outputs { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class MaterialPin
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class MaterialConnection
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourcePinId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetPinId { get; set; } = string.Empty;
}

public class TextureReference
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ExportedPath { get; set; }
}

// ─── Texture Models ─────────────────────────────────────────────────

public class TextureData
{
    public string TextureName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string PixelFormat { get; set; } = string.Empty;
    public int MipCount { get; set; }
    public string CompressionType { get; set; } = string.Empty;
    public bool SRGB { get; set; }
    public string? ExportedPath { get; set; }
}
