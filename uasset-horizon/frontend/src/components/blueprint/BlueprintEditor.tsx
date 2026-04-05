import React, { useMemo, useCallback } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  BackgroundVariant,
  type Node,
  type Edge,
  type NodeTypes,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import { BlueprintNodeMemo } from './BlueprintNode';
import { useAppStore } from '../../stores/appStore';
import { getEdgeColor } from '../../utils/pinColors';
import type { BlueprintData, PinType } from '../../types';

/**
 * Full Blueprint Graph Editor -- the core visual component of UAsset Horizon.
 * Converts parsed BlueprintData into React Flow nodes and edges,
 * rendering them with high-fidelity UE5 styling.
 */

const nodeTypes: NodeTypes = {
  blueprint: BlueprintNodeMemo,
};

interface Props {
  blueprint: BlueprintData;
}

export default function BlueprintEditor({ blueprint }: Props) {
  const showMinimap = useAppStore((s) => s.showMinimap);

  // Convert blueprint nodes to React Flow nodes
  const nodes: Node[] = useMemo(
    () =>
      blueprint.nodes.map((n) => ({
        id: n.id,
        type: 'blueprint',
        position: { x: n.positionX, y: n.positionY },
        data: {
          title: n.title,
          category: n.category,
          pins: n.pins,
          isPure: n.isPure,
          isCompact: n.isCompact,
          comment: n.comment,
          type: n.type,
        },
      })),
    [blueprint.nodes]
  );

  // Convert blueprint edges to React Flow edges
  const edges: Edge[] = useMemo(
    () =>
      blueprint.edges.map((e) => ({
        id: e.id,
        source: e.sourceNodeId,
        sourceHandle: e.sourcePinId,
        target: e.targetNodeId,
        targetHandle: e.targetPinId,
        type: 'smoothstep',
        animated: e.pinType === 'Exec',
        style: {
          stroke: getEdgeColor(e.pinType as PinType),
          strokeWidth: e.pinType === 'Exec' ? 2.5 : 1.5,
        },
        className: e.pinType === 'Exec' ? 'animated-edge' : '',
      })),
    [blueprint.edges]
  );

  const onInit = useCallback(() => {
    // Editor initialized
  }, []);

  return (
    <div className="w-full h-full relative">
      {/* Info bar */}
      <div className="absolute top-2 left-2 z-10 flex gap-2">
        <InfoBadge label="Nodes" value={blueprint.nodes.length} />
        <InfoBadge label="Edges" value={blueprint.edges.length} />
        <InfoBadge label="Variables" value={blueprint.variables.length} />
        {blueprint.parentClass && (
          <InfoBadge label="Parent" value={blueprint.parentClass} isText />
        )}
      </div>

      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        onInit={onInit}
        fitView
        fitViewOptions={{ padding: 0.2 }}
        minZoom={0.1}
        maxZoom={3}
        snapToGrid
        snapGrid={[16, 16]}
        proOptions={{ hideAttribution: true }}
        defaultEdgeOptions={{
          type: 'smoothstep',
        }}
      >
        <Background
          variant={BackgroundVariant.Dots}
          gap={16}
          size={1}
          color="#333333"
        />
        <Controls
          position="bottom-right"
          showInteractive={false}
        />
        {showMinimap && (
          <MiniMap
            position="bottom-left"
            nodeStrokeWidth={3}
            pannable
            zoomable
            style={{
              backgroundColor: '#1e1e2e',
              border: '1px solid #3a3a3a',
              borderRadius: 8,
            }}
          />
        )}
      </ReactFlow>
    </div>
  );
}

// ─── Info Badge ─────────────────────────────────────────────────────

function InfoBadge({
  label,
  value,
  isText = false,
}: {
  label: string;
  value: number | string;
  isText?: boolean;
}) {
  return (
    <div className="bg-ue-surface/90 border border-ue-border rounded px-2 py-0.5 text-[10px] backdrop-blur-sm">
      <span className="text-ue-muted">{label}: </span>
      <span className={isText ? 'text-ue-accent' : 'text-white font-bold'}>
        {value}
      </span>
    </div>
  );
}
