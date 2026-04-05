import React, { Suspense, useMemo } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  BackgroundVariant,
  type Node,
  type Edge,
} from '@xyflow/react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Environment } from '@react-three/drei';
import type { MaterialData } from '../../types';

/**
 * Node-based Material Editor similar to UE5 Material Editor.
 * Shows material graph on the left and a live preview sphere on the right.
 */

interface Props {
  material?: MaterialData;
}

export default function MaterialEditor({ material }: Props) {
  // Build React Flow graph from material nodes
  const nodes: Node[] = useMemo(() => {
    if (!material?.nodes.length) return getDefaultMaterialNodes();
    return material.nodes.map((n) => ({
      id: n.id,
      position: { x: n.positionX, y: n.positionY },
      data: { label: n.title, type: n.type },
      style: getMaterialNodeStyle(n.type),
    }));
  }, [material]);

  const edges: Edge[] = useMemo(() => {
    if (!material?.connections.length) return getDefaultMaterialEdges();
    return material.connections.map((c, i) => ({
      id: `mat-edge-${i}`,
      source: c.sourceNodeId,
      sourceHandle: c.sourcePinId,
      target: c.targetNodeId,
      targetHandle: c.targetPinId,
      style: { stroke: '#a8e650' },
    }));
  }, [material]);

  return (
    <div className="flex w-full h-full">
      {/* Material Graph -- left 65% */}
      <div className="flex-1 relative" style={{ minWidth: 0 }}>
        <div className="absolute top-2 left-2 z-10 bg-ue-surface/90 border border-ue-border rounded px-3 py-1 text-[11px] text-ue-muted backdrop-blur-sm">
          Material Graph {material ? `-- ${material.materialName}` : '-- Default'}
        </div>

        <ReactFlow
          nodes={nodes}
          edges={edges}
          fitView
          minZoom={0.2}
          maxZoom={2}
          proOptions={{ hideAttribution: true }}
        >
          <Background variant={BackgroundVariant.Dots} gap={16} size={1} color="#2a2a3a" />
          <Controls position="bottom-right" showInteractive={false} />
        </ReactFlow>
      </div>

      {/* Preview Sphere -- right 35% */}
      <div className="w-[35%] border-l border-ue-border relative">
        <div className="absolute top-2 left-2 z-10 bg-ue-surface/90 border border-ue-border rounded px-3 py-1 text-[11px] text-ue-muted backdrop-blur-sm">
          Preview
        </div>

        <Canvas camera={{ position: [2, 1.5, 2], fov: 45 }}>
          <Suspense fallback={null}>
            <ambientLight intensity={0.3} />
            <directionalLight position={[3, 3, 3]} intensity={1} />
            <Environment preset="studio" background={false} />

            <mesh>
              <sphereGeometry args={[1, 64, 64]} />
              <meshStandardMaterial
                color={material?.parameters.find(p => p.name === 'BaseColor')?.value as string || '#5577aa'}
                roughness={0.3}
                metalness={0.7}
              />
            </mesh>

            <OrbitControls enableDamping dampingFactor={0.1} />
          </Suspense>
        </Canvas>

        {/* Material info */}
        <div className="absolute bottom-2 left-2 right-2 bg-ue-surface/90 border border-ue-border rounded px-3 py-2 text-[10px] backdrop-blur-sm">
          <div className="flex justify-between text-ue-muted">
            <span>Blend: {material?.blendMode || 'Opaque'}</span>
            <span>Shading: {material?.shadingModel || 'DefaultLit'}</span>
          </div>
          {material?.twoSided && (
            <div className="text-yellow-500 mt-1">Two-Sided</div>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Default Material Graph (when no data loaded) ───────────────────

function getDefaultMaterialNodes(): Node[] {
  return [
    {
      id: 'mat-result',
      position: { x: 400, y: 0 },
      data: { label: 'Material Result' },
      style: getMaterialNodeStyle('Result'),
    },
    {
      id: 'tex-base',
      position: { x: 0, y: -50 },
      data: { label: 'Texture Sample\nBase Color' },
      style: getMaterialNodeStyle('TextureSample'),
    },
    {
      id: 'tex-normal',
      position: { x: 0, y: 80 },
      data: { label: 'Texture Sample\nNormal' },
      style: getMaterialNodeStyle('TextureSample'),
    },
    {
      id: 'const-rough',
      position: { x: 0, y: 200 },
      data: { label: 'Constant\n0.4' },
      style: getMaterialNodeStyle('Constant'),
    },
    {
      id: 'const-metal',
      position: { x: 0, y: 300 },
      data: { label: 'Constant\n0.0' },
      style: getMaterialNodeStyle('Constant'),
    },
  ];
}

function getDefaultMaterialEdges(): Edge[] {
  return [
    { id: 'e1', source: 'tex-base', target: 'mat-result', style: { stroke: '#ffc040' } },
    { id: 'e2', source: 'tex-normal', target: 'mat-result', style: { stroke: '#99ccff' } },
    { id: 'e3', source: 'const-rough', target: 'mat-result', style: { stroke: '#a8e650' } },
    { id: 'e4', source: 'const-metal', target: 'mat-result', style: { stroke: '#808080' } },
  ];
}

function getMaterialNodeStyle(type: string): React.CSSProperties {
  const base: React.CSSProperties = {
    background: '#2a2a3a',
    color: '#e0e0e0',
    border: '1px solid #3a3a4a',
    borderRadius: 6,
    padding: '8px 12px',
    fontSize: 11,
    whiteSpace: 'pre-line' as const,
    minWidth: 140,
  };

  if (type === 'Result') {
    return { ...base, background: '#1a3a2a', borderColor: '#2a6a4a' };
  }
  if (type === 'TextureSample') {
    return { ...base, background: '#2a2a4a', borderColor: '#4a4a6a' };
  }
  if (type === 'Constant') {
    return { ...base, background: '#3a2a2a', borderColor: '#6a4a4a', minWidth: 100 };
  }
  return base;
}
