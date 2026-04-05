# UAsset Horizon

**Universal Unreal Asset Explorer & Blueprint Visualizer**

A production-grade desktop application for reading, parsing, and visualizing Unreal Engine assets from **UE3 through UE5.6**, including encrypted game files. Features a high-fidelity Blueprint Node Editor, 3D Mesh Viewer, Material Editor, AES Key Extractor, and full asset extraction pipeline.

---

## Architecture

```
UAsset Horizon
├── backend/                    # C# .NET 8 Core Engine
│   ├── UAssetHorizon.Core/     # Parser, AES, Decryption libraries
│   │   ├── Parsers/            # Version detection + universal parser
│   │   ├── AES/                # Key scanner + database
│   │   ├── Decryption/         # Pak/IoStore decryptor
│   │   └── Models/             # Unified data models
│   └── UAssetHorizon.API/      # REST API (ASP.NET Minimal)
│       └── Controllers/        # Asset + AES endpoints
├── frontend/                   # React 18 + TypeScript + Tailwind
│   └── src/
│       ├── components/
│       │   ├── blueprint/      # UE5 Blueprint Node Editor
│       │   ├── viewer3d/       # Three.js Mesh Viewer
│       │   ├── material/       # Material Graph Editor
│       │   ├── explorer/       # Asset Browser (Content Browser)
│       │   └── aes/            # AES Key Management Panel
│       ├── stores/             # Zustand state management
│       ├── api/                # Backend API client
│       ├── types/              # TypeScript type definitions
│       └── styles/             # Global CSS + UE5 theme
├── electron/                   # Electron shell for desktop packaging
└── docs/                       # Documentation
```

## Features

### 1. Universal Multi-Version Parser
- Auto-detects UE version from file headers (magic bytes + version fields)
- Supports UE3 legacy format through UE5.6 modern format
- Parses Name tables, Import/Export tables, properties
- Outputs unified JSON schema regardless of source version
- Handles companion `.uexp` and `.ubulk` files automatically

### 2. Blueprint System (High-Fidelity)
- Full K2Node parsing and graph reconstruction
- Node categories: Events, Functions, Flow Control, Variables, Macros, Casts, Math
- Pin system with type-accurate colors matching UE5 exactly:
  - Exec (white), Bool (red), Float (yellow-green), Object (cyan), String (pink), etc.
- Execution flow animation on edges
- Pseudo-code generation from blueprint graph
- Interactive editor with zoom, pan, snap grid, minimap

### 3. AES Key Extractor & Decryption
- **Static Scanner**: High-entropy 256-bit key detection in .exe/.dll binaries
- **Signature Matching**: Unreal-specific byte patterns to reduce false positives
- **Key Validation**: Test keys against .pak files with instant feedback
- **Decryption Engine**: AES-256-ECB for .pak index decryption + file extraction
- **Key Database**: Persistent JSON storage with import/export support
- Progress reporting for large files (50GB+)

### 4. 3D Mesh Viewer
- Real-time rendering with Three.js / @react-three/fiber
- Orbit controls, studio HDRI lighting
- Grid, gizmo viewport, wireframe toggle
- LOD switching support

### 5. Material Editor
- Node-based material graph (React Flow)
- Live preview sphere with PBR materials
- Texture sample, constant, math operation nodes
- Base Color, Normal, Roughness, Metallic channels

### 6. Asset Explorer
- Tree-based file browser like Unreal Content Browser
- Category filtering (Blueprint, Mesh, Material, Texture, etc.)
- Search functionality
- Drag-and-drop file loading

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [npm](https://www.npmjs.com/) or [yarn](https://yarnpkg.com/)

## Quick Start

### 1. Backend (C# API)

```bash
cd backend/UAssetHorizon.API
dotnet restore
dotnet run
# API runs on http://localhost:5175
```

### 2. Frontend (React)

```bash
cd frontend
npm install
npm run dev
# UI runs on http://localhost:5173
```

### 3. Desktop App (Electron)

```bash
cd frontend
npm run electron:dev
```

### Production Build

```bash
# Build backend
cd backend/UAssetHorizon.API
dotnet publish -c Release

# Build frontend + Electron
cd frontend
npm run electron:build
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/asset/parse` | Parse a .uasset file |
| POST | `/api/asset/detect-version` | Detect UE version from header |
| POST | `/api/asset/browse` | List asset files in directory |
| POST | `/api/aes/scan` | Scan binary for AES keys |
| POST | `/api/aes/validate` | Validate key against .pak |
| POST | `/api/aes/decrypt` | Decrypt and extract .pak |
| GET | `/api/aes/keys` | List saved keys |
| POST | `/api/aes/keys` | Add a new key |
| DELETE | `/api/aes/keys/{id}` | Remove a key |

## Extending for Future UE Versions

The parser uses a **version-aware strategy pattern**:

1. `VersionDetector.cs` reads the header and classifies the version
2. `UniversalAssetParser.cs` adapts its parsing based on detected version
3. New versions only require updating version constants and any changed serialization

To add support for UE5.7+:
1. Add the new enum value to `UnrealVersion.cs`
2. Update `ClassifyVersion()` in `VersionDetector.cs` with the new file version number
3. Handle any serialization changes in the parser

## Development Roadmap

- **Phase 1**: Core Parser + AES Extractor + GUI [Current]
- **Phase 2**: Blueprint Node Editor (high-fidelity)
- **Phase 3**: 3D Viewer + Material Editor
- **Phase 4**: Full integration + polishing + performance
- **Phase 5**: Plugin system + export features

## Technology Stack

| Layer | Technology |
|-------|------------|
| Backend | C# .NET 8, UAssetAPI |
| Frontend | React 18, TypeScript, Tailwind CSS |
| Node Editor | @xyflow/react (React Flow) |
| 3D Rendering | Three.js, @react-three/fiber, @react-three/drei |
| State | Zustand |
| Desktop | Electron |
| Build | Vite, electron-builder |

## License

This project is for educational and modding purposes. Respect game developers' intellectual property.
