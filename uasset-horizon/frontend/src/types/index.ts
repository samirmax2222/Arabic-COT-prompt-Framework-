// ─── Asset Types ─────────────────────────────────────────────────────

export type AssetType =
  | 'Unknown' | 'Blueprint' | 'StaticMesh' | 'SkeletalMesh'
  | 'Material' | 'MaterialInstance' | 'Texture2D' | 'Animation'
  | 'Sound' | 'ParticleSystem' | 'Widget' | 'DataTable' | 'Level' | 'Other';

export type PinType =
  | 'Exec' | 'Boolean' | 'Byte' | 'Int' | 'Int64' | 'Float' | 'Double'
  | 'String' | 'Text' | 'Name' | 'Vector' | 'Rotator' | 'Transform'
  | 'Color' | 'Object' | 'Class' | 'Interface' | 'Struct' | 'Enum'
  | 'Array' | 'Set' | 'Map' | 'Delegate' | 'Wildcard';

export type PinDirection = 'Input' | 'Output';

export type NodeCategory =
  | 'Event' | 'Function' | 'FlowControl' | 'Variable'
  | 'Macro' | 'Cast' | 'Math' | 'Conversion' | 'Custom' | 'Comment';

export interface VersionInfo {
  version: string;
  packageFileVersion: number;
  licenseeVersion: number;
  customVersionCount: number;
  engineVersionString: string;
  isEncrypted: boolean;
  isCompressed: boolean;
  packageFlags: string;
}

export interface ParsedAsset {
  fileName: string;
  filePath: string;
  type: AssetType;
  versionInfo: VersionInfo;
  names: string[];
  imports: ImportEntry[];
  exports: ExportEntry[];
  blueprint?: BlueprintData;
  mesh?: MeshData;
  material?: MaterialData;
  texture?: TextureData;
  properties: Record<string, unknown>;
  dependencies: string[];
  fileSize: number;
  parsedAt: string;
}

export interface ImportEntry {
  index: number;
  classPackage: string;
  className: string;
  objectName: string;
  outerIndex: number;
}

export interface ExportEntry {
  index: number;
  objectName: string;
  className: string;
  superName: string;
  serialSize: number;
  serialOffset: number;
  properties: Record<string, unknown>;
}

// ─── Blueprint Types ────────────────────────────────────────────────

export interface BlueprintData {
  parentClass: string;
  nodes: BlueprintNode[];
  edges: BlueprintEdge[];
  variables: BlueprintVariable[];
  functions: BlueprintFunction[];
  components: BlueprintComponent[];
  graphs: BlueprintGraph[];
  pseudoCode?: string;
}

export interface BlueprintNode {
  id: string;
  type: string;
  title: string;
  comment?: string;
  category: NodeCategory;
  positionX: number;
  positionY: number;
  pins: BlueprintPin[];
  isCompact: boolean;
  isPure: boolean;
  targetClass?: string;
}

export interface BlueprintPin {
  id: string;
  name: string;
  direction: PinDirection;
  pinType: PinType;
  defaultValue?: string;
  linkedTo?: string;
  isHidden: boolean;
}

export interface BlueprintEdge {
  id: string;
  sourceNodeId: string;
  sourcePinId: string;
  targetNodeId: string;
  targetPinId: string;
  pinType: PinType;
}

export interface BlueprintVariable {
  name: string;
  type: string;
  defaultValue?: string;
  isPublic: boolean;
  category?: string;
  tooltip?: string;
}

export interface BlueprintFunction {
  name: string;
  isEvent: boolean;
  isPure: boolean;
  parameters: BlueprintPin[];
  returnValues: BlueprintPin[];
  description?: string;
}

export interface BlueprintComponent {
  name: string;
  className: string;
  parentComponent?: string;
  properties: Record<string, unknown>;
}

export interface BlueprintGraph {
  name: string;
  graphType: string;
  nodeIds: string[];
}

// ─── Mesh / Material / Texture ──────────────────────────────────────

export interface MeshData {
  meshType: string;
  vertexCount: number;
  triangleCount: number;
  lodCount: number;
  materialSlots: string[];
  exportedGltfPath?: string;
}

export interface MaterialData {
  materialName: string;
  parentMaterial?: string;
  blendMode: string;
  shadingModel: string;
  twoSided: boolean;
  parameters: MaterialParameter[];
  nodes: MaterialNode[];
  connections: MaterialConnection[];
  textures: TextureReference[];
}

export interface MaterialParameter {
  name: string;
  type: string;
  value: unknown;
  group?: string;
}

export interface MaterialNode {
  id: string;
  type: string;
  title: string;
  positionX: number;
  positionY: number;
  inputs: { id: string; name: string; type: string }[];
  outputs: { id: string; name: string; type: string }[];
  properties: Record<string, unknown>;
}

export interface MaterialConnection {
  sourceNodeId: string;
  sourcePinId: string;
  targetNodeId: string;
  targetPinId: string;
}

export interface TextureReference {
  name: string;
  path: string;
  exportedPath?: string;
}

export interface TextureData {
  textureName: string;
  width: number;
  height: number;
  pixelFormat: string;
  mipCount: number;
  compressionType: string;
  srgb: boolean;
  exportedPath?: string;
}

// ─── AES Types ──────────────────────────────────────────────────────

export interface AesKeyEntry {
  id: string;
  keyHex: string;
  gameName?: string;
  source?: string;
  isValidated: boolean;
  discoveredAt: string;
  notes?: string;
  keySource: string;
}

export interface AesScanResult {
  candidates: AesKeyCandidate[];
  totalBytesScanned: number;
  scanDurationMs: number;
  sourceFile: string;
  method: string;
}

export interface AesKeyCandidate {
  keyHex: string;
  offsetInFile: number;
  entropy: number;
  confidence: number;
  matchesUnrealSignature: boolean;
  nearbyContext?: string;
}

export interface DecryptionResult {
  success: boolean;
  keyUsed: string;
  filePath: string;
  errorMessage?: string;
  filesExtracted: number;
  bytesDecrypted: number;
  durationMs: number;
  extractedFiles: { path: string; assetType: string; size: number }[];
}

export interface OperationProgress {
  operationId: string;
  status: string;
  progressPercent: number;
  currentStep: string;
  bytesProcessed: number;
  totalBytes: number;
  estimatedRemainingMs: number;
}

// ─── UI Types ───────────────────────────────────────────────────────

export type TabId = 'explorer' | 'blueprint' | 'viewer3d' | 'material' | 'aes' | 'pseudo';

export interface FileTreeItem {
  path: string;
  name: string;
  extension: string;
  size: number;
  directory: string;
  isFolder?: boolean;
  children?: FileTreeItem[];
}
