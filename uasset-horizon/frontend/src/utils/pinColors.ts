import type { PinType } from '../types';

/**
 * Maps UE Blueprint pin types to their canonical colors.
 * These match the official Unreal Engine 5 Blueprint Editor colors.
 */
export const PIN_COLORS: Record<PinType, string> = {
  Exec: '#ffffff',
  Boolean: '#9c0000',
  Byte: '#006464',
  Int: '#1ca864',
  Int64: '#a8e6a8',
  Float: '#a8e650',
  Double: '#94fc13',
  String: '#f0a0f0',
  Text: '#f080a8',
  Name: '#b898de',
  Vector: '#ffc040',
  Rotator: '#99ccff',
  Transform: '#f08040',
  Color: '#ff8000',
  Object: '#00a0f0',
  Class: '#6040b0',
  Interface: '#b8d0a0',
  Struct: '#0088cc',
  Enum: '#006464',
  Array: '#808080',
  Set: '#808080',
  Map: '#808080',
  Delegate: '#f04040',
  Wildcard: '#808080',
};

/**
 * Get the CSS class for a pin type.
 */
export function getPinColorClass(pinType: PinType): string {
  return `pin-${pinType.toLowerCase()}`;
}

/**
 * Get the header gradient for a node category.
 */
export function getNodeHeaderStyle(category: string): React.CSSProperties {
  switch (category) {
    case 'Event':
      return {
        background: 'linear-gradient(135deg, #8c1a1a 0%, #6b1414 100%)',
        color: '#ffffff',
      };
    case 'Function':
      return {
        background: 'linear-gradient(135deg, #1a4a8c 0%, #143a6b 100%)',
        color: '#ffffff',
      };
    case 'FlowControl':
      return {
        background: 'linear-gradient(135deg, #3a3a4a 0%, #2a2a3a 100%)',
        color: '#ffffff',
      };
    case 'Variable':
      return {
        background: 'linear-gradient(135deg, #1a6a3a 0%, #145a2e 100%)',
        color: '#ffffff',
      };
    case 'Macro':
      return {
        background: 'linear-gradient(135deg, #4a4a5a 0%, #3a3a4a 100%)',
        color: '#ffffff',
      };
    case 'Cast':
      return {
        background: 'linear-gradient(135deg, #2a6a4a 0%, #1a5a3a 100%)',
        color: '#ffffff',
      };
    case 'Math':
      return {
        background: 'linear-gradient(135deg, #3a5a2a 0%, #2a4a1a 100%)',
        color: '#ffffff',
      };
    default:
      return {
        background: 'linear-gradient(135deg, #3a3a4a 0%, #2a2a3a 100%)',
        color: '#ffffff',
      };
  }
}

/**
 * Get the edge (connection) color based on pin type.
 */
export function getEdgeColor(pinType: PinType): string {
  return PIN_COLORS[pinType] || '#808080';
}

/**
 * Format asset type to an icon label.
 */
export function getAssetIcon(type: string): string {
  const icons: Record<string, string> = {
    Blueprint: 'BP',
    StaticMesh: 'SM',
    SkeletalMesh: 'SK',
    Material: 'M',
    MaterialInstance: 'MI',
    Texture2D: 'T',
    Animation: 'AN',
    Sound: 'SN',
    ParticleSystem: 'FX',
    Widget: 'W',
    DataTable: 'DT',
    Level: 'LV',
  };
  return icons[type] || '?';
}

export function getAssetColor(type: string): string {
  const colors: Record<string, string> = {
    Blueprint: '#0078d4',
    StaticMesh: '#00a0f0',
    SkeletalMesh: '#00a0f0',
    Material: '#a8e650',
    MaterialInstance: '#a8e650',
    Texture2D: '#ffc040',
    Animation: '#f08040',
    Sound: '#f0a0f0',
    ParticleSystem: '#f04040',
    Widget: '#b898de',
    DataTable: '#1ca864',
    Level: '#99ccff',
  };
  return colors[type] || '#808080';
}
