import React, { useState } from 'react';
import { useAppStore } from '../../stores/appStore';
import { getAssetIcon, getAssetColor } from '../../utils/pinColors';
import type { ParsedAsset } from '../../types';

/**
 * Asset Explorer panel -- tree view like Unreal Content Browser.
 * Shows file hierarchy with thumbnails, search, filters, and categories.
 */

export default function AssetExplorer() {
  const { currentAsset, fileTree, setActiveTab } = useAppStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');

  const categories = ['All', 'Blueprint', 'Mesh', 'Material', 'Texture', 'Animation', 'Sound', 'Other'];

  return (
    <div className="h-full flex flex-col bg-ue-bg">
      {/* Search bar */}
      <div className="p-2 border-b border-ue-border">
        <input
          type="text"
          placeholder="Search assets..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full bg-ue-surface border border-ue-border rounded px-3 py-1.5 text-sm text-ue-text placeholder:text-ue-muted focus:outline-none focus:border-ue-accent"
        />
      </div>

      {/* Category filters */}
      <div className="flex gap-1 p-2 border-b border-ue-border overflow-x-auto">
        {categories.map((cat) => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            className={`px-2 py-0.5 rounded text-[10px] font-medium transition-colors whitespace-nowrap ${
              selectedCategory === cat
                ? 'bg-ue-accent text-white'
                : 'bg-ue-surface text-ue-muted hover:text-white'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Asset info (when loaded) */}
      {currentAsset && (
        <div className="p-3 border-b border-ue-border">
          <AssetCard asset={currentAsset} onOpenTab={setActiveTab} />
        </div>
      )}

      {/* File tree or drop zone */}
      <div className="flex-1 overflow-y-auto p-2">
        {fileTree.length > 0 ? (
          <div className="space-y-0.5">
            {fileTree
              .filter((f) => {
                if (searchQuery) {
                  return f.name.toLowerCase().includes(searchQuery.toLowerCase());
                }
                return true;
              })
              .map((file, i) => (
                <FileRow key={i} file={file} />
              ))}
          </div>
        ) : (
          <DropZone />
        )}
      </div>

      {/* Status bar */}
      <div className="px-3 py-1 border-t border-ue-border text-[10px] text-ue-muted flex justify-between">
        <span>{fileTree.length} files</span>
        {currentAsset && (
          <span>{currentAsset.versionInfo.version} | {currentAsset.type}</span>
        )}
      </div>
    </div>
  );
}

// ─── Asset Card ─────────────────────────────────────────────────────

function AssetCard({ asset, onOpenTab }: { asset: ParsedAsset; onOpenTab: (tab: any) => void }) {
  const iconColor = getAssetColor(asset.type);
  const icon = getAssetIcon(asset.type);

  return (
    <div className="bg-ue-surface rounded-lg p-3 border border-ue-border">
      <div className="flex items-start gap-3">
        {/* Type badge */}
        <div
          className="w-10 h-10 rounded flex items-center justify-center text-sm font-bold text-white"
          style={{ backgroundColor: iconColor }}
        >
          {icon}
        </div>

        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium text-ue-text truncate">{asset.fileName}</div>
          <div className="text-[10px] text-ue-muted mt-0.5">
            {asset.type} | {asset.versionInfo.version} | {formatSize(asset.fileSize)}
          </div>

          {/* Quick actions */}
          <div className="flex gap-1 mt-2">
            {asset.blueprint && (
              <QuickAction label="Blueprint" onClick={() => onOpenTab('blueprint')} color="#0078d4" />
            )}
            {asset.mesh && (
              <QuickAction label="3D View" onClick={() => onOpenTab('viewer3d')} color="#00a0f0" />
            )}
            {asset.material && (
              <QuickAction label="Material" onClick={() => onOpenTab('material')} color="#a8e650" />
            )}
          </div>
        </div>
      </div>

      {/* Details */}
      <div className="mt-3 grid grid-cols-2 gap-1 text-[10px]">
        <Detail label="Names" value={asset.names.length} />
        <Detail label="Imports" value={asset.imports.length} />
        <Detail label="Exports" value={asset.exports.length} />
        <Detail label="Dependencies" value={asset.dependencies.length} />
      </div>
    </div>
  );
}

function QuickAction({ label, onClick, color }: { label: string; onClick: () => void; color: string }) {
  return (
    <button
      onClick={onClick}
      className="px-2 py-0.5 rounded text-[9px] font-medium text-white transition-opacity hover:opacity-80"
      style={{ backgroundColor: color }}
    >
      {label}
    </button>
  );
}

function Detail({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex justify-between px-2 py-0.5 bg-ue-bg/50 rounded">
      <span className="text-ue-muted">{label}</span>
      <span className="text-ue-text font-medium">{value}</span>
    </div>
  );
}

// ─── File Row ───────────────────────────────────────────────────────

function FileRow({ file }: { file: any }) {
  const ext = file.extension || '';
  const color = ext === '.uasset' ? '#0078d4' : ext === '.uexp' ? '#a8e650' : '#808080';

  return (
    <div className="flex items-center gap-2 px-2 py-1 rounded hover:bg-ue-hover cursor-pointer transition-colors group">
      <div
        className="w-1.5 h-1.5 rounded-full flex-shrink-0"
        style={{ backgroundColor: color }}
      />
      <span className="text-[11px] text-ue-muted group-hover:text-ue-text truncate flex-1">
        {file.name}
      </span>
      <span className="text-[9px] text-ue-muted">
        {formatSize(file.size)}
      </span>
    </div>
  );
}

// ─── Drop Zone ──────────────────────────────────────────────────────

function DropZone() {
  const [isDragOver, setIsDragOver] = useState(false);

  return (
    <div
      className={`drop-zone flex flex-col items-center justify-center h-full p-8 text-center ${
        isDragOver ? 'active' : ''
      }`}
      onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
      onDragLeave={() => setIsDragOver(false)}
      onDrop={(e) => { e.preventDefault(); setIsDragOver(false); }}
    >
      <div className="text-4xl text-ue-muted mb-3">+</div>
      <div className="text-sm text-ue-muted">
        Drop .uasset, .pak, or folder here
      </div>
      <div className="text-[10px] text-ue-muted mt-1">
        or use File &gt; Open
      </div>
    </div>
  );
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
}
