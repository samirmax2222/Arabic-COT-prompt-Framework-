import React from 'react';
import { useAppStore } from './stores/appStore';
import AssetExplorer from './components/explorer/AssetExplorer';
import BlueprintEditor from './components/blueprint/BlueprintEditor';
import MeshViewer from './components/viewer3d/MeshViewer';
import MaterialEditor from './components/material/MaterialEditor';
import AesPanel from './components/aes/AesPanel';
import type { TabId } from './types';

/**
 * UAsset Horizon -- Main Application Shell.
 * Professional Unreal Engine dark theme with sidebar + tabbed content area.
 */

const TABS: { id: TabId; label: string; shortcut: string }[] = [
  { id: 'explorer', label: 'Explorer', shortcut: '1' },
  { id: 'blueprint', label: 'Blueprint', shortcut: '2' },
  { id: 'viewer3d', label: '3D Viewer', shortcut: '3' },
  { id: 'material', label: 'Material', shortcut: '4' },
  { id: 'aes', label: 'AES / Decrypt', shortcut: '5' },
  { id: 'pseudo', label: 'Pseudo Code', shortcut: '6' },
];

export default function App() {
  const { activeTab, setActiveTab, currentAsset, sidebarWidth, isLoading, error } = useAppStore();

  return (
    <div className="h-screen w-screen flex flex-col bg-ue-bg text-ue-text overflow-hidden select-none">
      {/* Title Bar */}
      <header className="h-9 bg-[#1e1e1e] border-b border-ue-border flex items-center px-4 gap-4 shrink-0">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-gradient-to-br from-blue-500 to-purple-600" />
          <span className="text-xs font-bold tracking-wide">UASSET HORIZON</span>
        </div>

        {/* Menu items */}
        <nav className="flex gap-3 text-[11px] text-ue-muted">
          <span className="hover:text-white cursor-pointer">File</span>
          <span className="hover:text-white cursor-pointer">Edit</span>
          <span className="hover:text-white cursor-pointer">View</span>
          <span className="hover:text-white cursor-pointer">Tools</span>
          <span className="hover:text-white cursor-pointer">Help</span>
        </nav>

        <div className="ml-auto flex items-center gap-3 text-[10px] text-ue-muted">
          {isLoading && (
            <span className="text-ue-accent animate-pulse">Parsing...</span>
          )}
          {currentAsset && (
            <span>{currentAsset.fileName} | {currentAsset.versionInfo.version}</span>
          )}
        </div>
      </header>

      {/* Tab Bar */}
      <div className="h-8 bg-ue-surface border-b border-ue-border flex shrink-0">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`tab-button text-[11px] ${activeTab === tab.id ? 'active' : ''}`}
          >
            {tab.label}
            <span className="ml-1 text-[9px] opacity-40">({tab.shortcut})</span>
          </button>
        ))}
      </div>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar (Explorer) -- always visible on left */}
        <aside
          className="shrink-0 border-r border-ue-border overflow-hidden"
          style={{ width: sidebarWidth }}
        >
          <AssetExplorer />
        </aside>

        {/* Content Area */}
        <main className="flex-1 overflow-hidden relative">
          {error && (
            <div className="absolute top-2 left-1/2 -translate-x-1/2 z-50 bg-red-900/90 text-red-200 px-4 py-2 rounded text-sm border border-red-700 animate-fade-in">
              {error}
            </div>
          )}

          {activeTab === 'explorer' && (
            <WelcomeScreen />
          )}

          {activeTab === 'blueprint' && currentAsset?.blueprint && (
            <BlueprintEditor blueprint={currentAsset.blueprint} />
          )}
          {activeTab === 'blueprint' && !currentAsset?.blueprint && (
            <EmptyState message="Load a Blueprint .uasset to view the node graph" />
          )}

          {activeTab === 'viewer3d' && (
            <MeshViewer meshPath={currentAsset?.mesh?.exportedGltfPath} />
          )}

          {activeTab === 'material' && (
            <MaterialEditor material={currentAsset?.material} />
          )}

          {activeTab === 'aes' && (
            <AesPanel />
          )}

          {activeTab === 'pseudo' && currentAsset?.blueprint?.pseudoCode && (
            <PseudoCodeView code={currentAsset.blueprint.pseudoCode} />
          )}
          {activeTab === 'pseudo' && !currentAsset?.blueprint?.pseudoCode && (
            <EmptyState message="Load a Blueprint to generate pseudo-code" />
          )}
        </main>
      </div>

      {/* Status Bar */}
      <footer className="h-6 bg-[#1e1e1e] border-t border-ue-border flex items-center px-4 text-[10px] text-ue-muted shrink-0">
        <span>UAsset Horizon v1.0.0</span>
        <span className="mx-2">|</span>
        <span>UE3 - UE5.6 Support</span>
        <span className="mx-2">|</span>
        <span>UAssetAPI Engine</span>
        <div className="ml-auto flex gap-4">
          <span>API: Connected</span>
          <span>Ready</span>
        </div>
      </footer>
    </div>
  );
}

// ─── Welcome Screen ─────────────────────────────────────────────────

function WelcomeScreen() {
  return (
    <div className="h-full flex flex-col items-center justify-center text-center p-8">
      <div className="w-16 h-16 rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 mb-6 shadow-glow" />
      <h1 className="text-2xl font-bold text-white mb-2">UAsset Horizon</h1>
      <p className="text-sm text-ue-muted max-w-md">
        Universal Unreal Asset Explorer & Blueprint Visualizer.
        Open a .uasset file or directory to begin exploring.
      </p>
      <div className="mt-8 grid grid-cols-3 gap-4 text-[11px]">
        <FeatureCard title="Parse Assets" desc="UE3 to UE5.6" color="#0078d4" />
        <FeatureCard title="Blueprint Editor" desc="Node graph viewer" color="#8c1a1a" />
        <FeatureCard title="3D Viewer" desc="Mesh & material preview" color="#1a6a3a" />
        <FeatureCard title="AES Extractor" desc="Key scanning & decrypt" color="#6040b0" />
        <FeatureCard title="Material Editor" desc="Shader graph viewer" color="#a8e650" />
        <FeatureCard title="Pseudo Code" desc="Blueprint to code" color="#f08040" />
      </div>
    </div>
  );
}

function FeatureCard({ title, desc, color }: { title: string; desc: string; color: string }) {
  return (
    <div className="bg-ue-surface rounded-lg p-3 border border-ue-border hover:border-ue-accent transition-colors cursor-pointer">
      <div className="w-2 h-2 rounded-full mb-2" style={{ backgroundColor: color }} />
      <div className="font-medium text-white">{title}</div>
      <div className="text-ue-muted text-[10px] mt-0.5">{desc}</div>
    </div>
  );
}

function EmptyState({ message }: { message: string }) {
  return (
    <div className="h-full flex items-center justify-center text-ue-muted text-sm">
      {message}
    </div>
  );
}

function PseudoCodeView({ code }: { code: string }) {
  return (
    <div className="h-full overflow-auto bg-[#1e1e2e] p-6">
      <div className="text-[11px] text-ue-muted uppercase tracking-wide mb-4">
        Generated Pseudo Code
      </div>
      <pre className="font-mono text-sm text-ue-text whitespace-pre-wrap leading-6">
        {code}
      </pre>
    </div>
  );
}
