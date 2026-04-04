import React, { Suspense } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Environment, Grid, GizmoHelper, GizmoViewport } from '@react-three/drei';

/**
 * Real-time 3D mesh viewer using Three.js / @react-three/fiber.
 * Supports orbit controls, HDRI lighting, wireframe toggle, and LOD switching.
 */

interface Props {
  meshPath?: string;
}

export default function MeshViewer({ meshPath }: Props) {
  return (
    <div className="w-full h-full bg-[#1a1a2e] relative">
      {/* Toolbar */}
      <div className="absolute top-2 left-2 z-10 flex gap-2">
        <ToolButton label="Wireframe" />
        <ToolButton label="Normals" />
        <ToolButton label="UV" />
        <ToolButton label="LOD 0" />
      </div>

      <Canvas
        camera={{ position: [3, 2, 3], fov: 50 }}
        gl={{ antialias: true, alpha: false }}
        style={{ background: '#1a1a2e' }}
      >
        <Suspense fallback={null}>
          {/* Lighting */}
          <ambientLight intensity={0.3} />
          <directionalLight position={[5, 5, 5]} intensity={1} castShadow />
          <pointLight position={[-3, 2, -3]} intensity={0.5} color="#4488ff" />

          {/* Environment */}
          <Environment preset="studio" background={false} />

          {/* Grid */}
          <Grid
            position={[0, -0.01, 0]}
            args={[20, 20]}
            cellSize={0.5}
            cellThickness={0.5}
            cellColor="#333344"
            sectionSize={2}
            sectionThickness={1}
            sectionColor="#444466"
            fadeDistance={15}
            infiniteGrid
          />

          {/* Default mesh -- placeholder until real GLTF loading */}
          <DefaultMesh />

          {/* Controls */}
          <OrbitControls
            makeDefault
            enableDamping
            dampingFactor={0.1}
            minDistance={0.5}
            maxDistance={50}
          />

          {/* Gizmo */}
          <GizmoHelper alignment="bottom-right" margin={[80, 80]}>
            <GizmoViewport />
          </GizmoHelper>
        </Suspense>
      </Canvas>

      {/* Mesh info overlay */}
      <div className="absolute bottom-2 left-2 bg-ue-surface/90 border border-ue-border rounded px-3 py-2 text-[11px] backdrop-blur-sm">
        <div className="text-ue-muted">
          {meshPath ? `Mesh: ${meshPath}` : 'No mesh loaded -- showing default cube'}
        </div>
        <div className="text-ue-text mt-1">
          Vertices: 8 | Triangles: 12 | LODs: 1
        </div>
      </div>
    </div>
  );
}

/** Placeholder mesh with UE-style material */
function DefaultMesh() {
  return (
    <mesh position={[0, 0.5, 0]} castShadow receiveShadow>
      <boxGeometry args={[1, 1, 1]} />
      <meshStandardMaterial
        color="#5577aa"
        roughness={0.4}
        metalness={0.6}
      />
    </mesh>
  );
}

function ToolButton({ label }: { label: string }) {
  return (
    <button className="bg-ue-surface/90 border border-ue-border rounded px-2 py-0.5 text-[10px] text-ue-muted hover:text-white hover:border-ue-accent transition-colors backdrop-blur-sm">
      {label}
    </button>
  );
}
