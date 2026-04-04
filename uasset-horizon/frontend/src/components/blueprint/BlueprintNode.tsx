import React, { memo } from 'react';
import { Handle, Position, type NodeProps } from '@xyflow/react';
import type { BlueprintPin, NodeCategory, PinType } from '../../types';
import { PIN_COLORS, getNodeHeaderStyle } from '../../utils/pinColors';

/**
 * High-fidelity Unreal Engine 5 Blueprint Node component.
 * Renders with gradient headers, typed pin colors, exec arrows,
 * and proper UE5 dark theme styling.
 */

interface BPNodeData {
  title: string;
  category: NodeCategory;
  pins: BlueprintPin[];
  isPure: boolean;
  isCompact: boolean;
  comment?: string;
  type: string;
}

function BlueprintNodeComponent({ data, selected }: NodeProps) {
  const nodeData = data as unknown as BPNodeData;
  const { title, category, pins = [], isPure, isCompact, comment } = nodeData;
  const headerStyle = getNodeHeaderStyle(category);

  const inputPins = pins.filter((p) => p.direction === 'Input' && !p.isHidden);
  const outputPins = pins.filter((p) => p.direction === 'Output' && !p.isHidden);
  const maxPins = Math.max(inputPins.length, outputPins.length, 1);

  // Compact nodes (like math operators) render differently
  if (isCompact) {
    return (
      <div
        className={`bp-node ${selected ? 'selected' : ''}`}
        style={{ minWidth: 120 }}
      >
        <div className="bp-node-body" style={{ padding: '6px 8px' }}>
          <div className="flex items-center justify-between gap-4">
            {/* Input handles */}
            <div className="flex flex-col gap-1">
              {inputPins.map((pin, i) => (
                <PinRow key={pin.id} pin={pin} index={i} isInput />
              ))}
            </div>

            {/* Operator label */}
            <span className="text-[13px] font-bold text-white px-2">
              {title}
            </span>

            {/* Output handles */}
            <div className="flex flex-col gap-1">
              {outputPins.map((pin, i) => (
                <PinRow key={pin.id} pin={pin} index={i} isInput={false} />
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`bp-node ${selected ? 'selected' : ''}`}>
      {/* Comment banner */}
      {comment && (
        <div className="px-3 py-1 text-[10px] text-yellow-400 bg-yellow-900/20 border-b border-yellow-800/30">
          {comment}
        </div>
      )}

      {/* Header with gradient */}
      <div className="bp-node-header" style={headerStyle}>
        {/* Category icon */}
        <CategoryIcon category={category} />
        <span className="truncate">{title}</span>
        {isPure && (
          <span className="ml-auto text-[9px] opacity-60 uppercase">Pure</span>
        )}
      </div>

      {/* Body with pins */}
      <div className="bp-node-body">
        {Array.from({ length: maxPins }).map((_, rowIdx) => (
          <div key={rowIdx} className="flex justify-between min-h-[22px]">
            {/* Input pin */}
            <div className="flex-1">
              {inputPins[rowIdx] && (
                <PinRow pin={inputPins[rowIdx]} index={rowIdx} isInput />
              )}
            </div>

            {/* Output pin */}
            <div className="flex-1">
              {outputPins[rowIdx] && (
                <PinRow pin={outputPins[rowIdx]} index={rowIdx} isInput={false} />
              )}
            </div>
          </div>
        ))}

        {maxPins === 0 && (
          <div className="px-3 py-1 text-[10px] text-gray-500 italic">
            No pins
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Pin Row Component ──────────────────────────────────────────────

interface PinRowProps {
  pin: BlueprintPin;
  index: number;
  isInput: boolean;
}

function PinRow({ pin, index, isInput }: PinRowProps) {
  const color = PIN_COLORS[pin.pinType] || '#808080';
  const isExec = pin.pinType === 'Exec';
  const hasConnection = !!pin.linkedTo;

  return (
    <div className={`bp-pin ${isInput ? 'input' : 'output'}`}>
      {/* React Flow Handle */}
      <Handle
        type={isInput ? 'target' : 'source'}
        position={isInput ? Position.Left : Position.Right}
        id={pin.id}
        style={{
          width: isExec ? 12 : 10,
          height: isExec ? 14 : 10,
          background: hasConnection ? color : 'transparent',
          border: isExec ? 'none' : `2px solid ${color}`,
          borderRadius: isExec ? 2 : '50%',
          ...(isExec
            ? {
                clipPath: 'polygon(0 0, 70% 0, 100% 50%, 70% 100%, 0 100%)',
                background: hasConnection ? color : '#444',
              }
            : {}),
        }}
      />

      {/* Pin label */}
      <span
        className="text-[11px]"
        style={{ color: pin.name ? '#cccccc' : '#666666' }}
      >
        {pin.name || (isExec ? '' : `Pin ${index}`)}
      </span>

      {/* Default value indicator */}
      {pin.defaultValue && !isExec && (
        <span className="text-[9px] text-gray-500 ml-1">
          = {pin.defaultValue}
        </span>
      )}
    </div>
  );
}

// ─── Category Icon ──────────────────────────────────────────────────

function CategoryIcon({ category }: { category: NodeCategory }) {
  const iconMap: Record<string, string> = {
    Event: '⚡',
    Function: 'ƒ',
    FlowControl: '⇌',
    Variable: '◆',
    Macro: '▣',
    Cast: '⇒',
    Math: '∑',
    Conversion: '↔',
    Custom: '★',
    Comment: '💬',
  };

  return (
    <span className="text-[10px] opacity-70">
      {iconMap[category] || '●'}
    </span>
  );
}

export const BlueprintNodeMemo = memo(BlueprintNodeComponent);
export default BlueprintNodeMemo;
