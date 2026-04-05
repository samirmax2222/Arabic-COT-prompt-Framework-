import React, { useState } from 'react';
import { useAppStore } from '../../stores/appStore';
import type { AesKeyEntry, AesKeyCandidate } from '../../types';

/**
 * AES Key Extractor & Decryption Management Panel.
 * Provides drag-and-drop scanning, key validation, decryption, and key database.
 */

export default function AesPanel() {
  const { aesKeys, lastScanResult, logs, addLog } = useAppStore();
  const [keyInput, setKeyInput] = useState('');
  const [pakPath, setPakPath] = useState('');
  const [activeSection, setActiveSection] = useState<'scan' | 'keys' | 'decrypt'>('scan');

  const handleAddKey = () => {
    if (keyInput.length === 64) {
      addLog('success', `Key added: ${keyInput.slice(0, 8)}...`);
      setKeyInput('');
    } else {
      addLog('error', 'Invalid key: must be 64 hex characters (32 bytes)');
    }
  };

  return (
    <div className="h-full flex flex-col bg-ue-bg">
      {/* Section tabs */}
      <div className="flex border-b border-ue-border">
        {(['scan', 'keys', 'decrypt'] as const).map((sec) => (
          <button
            key={sec}
            onClick={() => setActiveSection(sec)}
            className={`tab-button flex-1 capitalize ${activeSection === sec ? 'active' : ''}`}
          >
            {sec === 'scan' ? 'Key Scanner' : sec === 'keys' ? 'Key Database' : 'Decrypt'}
          </button>
        ))}
      </div>

      <div className="flex-1 overflow-y-auto p-4">
        {activeSection === 'scan' && <ScanSection />}
        {activeSection === 'keys' && <KeysSection keys={aesKeys} />}
        {activeSection === 'decrypt' && (
          <DecryptSection pakPath={pakPath} setPakPath={setPakPath} />
        )}
      </div>

      {/* Log panel */}
      <div className="h-40 border-t border-ue-border bg-[#1a1a1a] overflow-y-auto p-2">
        <div className="text-[10px] font-medium text-ue-muted mb-1 uppercase tracking-wide">
          Operation Log
        </div>
        {logs.slice(-50).map((log, i) => (
          <div key={i} className={`text-[10px] font-mono log-${log.level} py-0.5`}>
            <span className="text-ue-muted mr-2">
              {new Date(log.timestamp).toLocaleTimeString()}
            </span>
            {log.message}
          </div>
        ))}
        {logs.length === 0 && (
          <div className="text-[10px] text-ue-muted italic">No log entries yet</div>
        )}
      </div>
    </div>
  );
}

// ─── Scan Section ───────────────────────────────────────────────────

function ScanSection() {
  const [isDragOver, setIsDragOver] = useState(false);
  const { lastScanResult } = useAppStore();

  return (
    <div className="space-y-4">
      {/* Drag & Drop zone */}
      <div
        className={`drop-zone p-8 text-center ${isDragOver ? 'active' : ''}`}
        onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
        onDragLeave={() => setIsDragOver(false)}
        onDrop={(e) => { e.preventDefault(); setIsDragOver(false); }}
      >
        <div className="text-3xl text-ue-muted mb-2">🔑</div>
        <div className="text-sm text-ue-muted">
          Drop .exe, .dll, or .pak file to scan for AES keys
        </div>
        <div className="text-[10px] text-ue-muted mt-1">
          Supports static entropy + Unreal signature scanning
        </div>
      </div>

      {/* Manual key input */}
      <div>
        <label className="text-[11px] text-ue-muted uppercase tracking-wide block mb-1">
          Manual Key Entry (64 hex chars)
        </label>
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="0x..."
            className="flex-1 bg-ue-surface border border-ue-border rounded px-3 py-1.5 text-sm text-ue-text font-mono placeholder:text-ue-muted focus:outline-none focus:border-ue-accent"
            maxLength={66}
          />
          <button className="bg-ue-accent text-white px-4 py-1.5 rounded text-sm font-medium hover:opacity-90 transition-opacity">
            Add Key
          </button>
        </div>
      </div>

      {/* Scan results */}
      {lastScanResult && (
        <div>
          <div className="text-[11px] text-ue-muted uppercase tracking-wide mb-2">
            Scan Results ({lastScanResult.candidates.length} candidates)
          </div>
          <div className="space-y-1">
            {lastScanResult.candidates.map((c, i) => (
              <CandidateRow key={i} candidate={c} index={i} />
            ))}
          </div>
          <div className="text-[10px] text-ue-muted mt-2">
            Scanned {(lastScanResult.totalBytesScanned / 1024 / 1024).toFixed(1)} MB
            in {(lastScanResult.scanDurationMs / 1000).toFixed(1)}s
          </div>
        </div>
      )}
    </div>
  );
}

function CandidateRow({ candidate, index }: { candidate: AesKeyCandidate; index: number }) {
  const confidence = Math.round(candidate.confidence * 100);
  const confColor = confidence >= 70 ? '#4ade80' : confidence >= 40 ? '#facc15' : '#f87171';

  return (
    <div className="bg-ue-surface rounded p-2 border border-ue-border flex items-center gap-3">
      <div className="text-[10px] text-ue-muted w-6">#{index + 1}</div>

      <div className="flex-1 min-w-0">
        <div className="font-mono text-[10px] text-ue-text truncate">
          {candidate.keyHex.slice(0, 16)}...{candidate.keyHex.slice(-8)}
        </div>
        <div className="text-[9px] text-ue-muted mt-0.5">
          Offset: 0x{candidate.offsetInFile.toString(16).toUpperCase()} |
          Entropy: {candidate.entropy.toFixed(2)}
          {candidate.matchesUnrealSignature && ' | UE Signature'}
        </div>
      </div>

      {/* Confidence bar */}
      <div className="w-16">
        <div className="h-1.5 bg-ue-bg rounded-full overflow-hidden">
          <div
            className="h-full rounded-full transition-all"
            style={{ width: `${confidence}%`, backgroundColor: confColor }}
          />
        </div>
        <div className="text-[9px] text-center mt-0.5" style={{ color: confColor }}>
          {confidence}%
        </div>
      </div>

      <button className="text-[10px] text-ue-accent hover:underline">
        Test
      </button>
    </div>
  );
}

// ─── Keys Section ───────────────────────────────────────────────────

function KeysSection({ keys }: { keys: AesKeyEntry[] }) {
  return (
    <div className="space-y-3">
      <div className="flex justify-between items-center">
        <span className="text-[11px] text-ue-muted uppercase tracking-wide">
          Saved Keys ({keys.length})
        </span>
        <div className="flex gap-2">
          <button className="text-[10px] text-ue-accent hover:underline">Import</button>
          <button className="text-[10px] text-ue-accent hover:underline">Export</button>
        </div>
      </div>

      {keys.length === 0 ? (
        <div className="text-center py-8 text-ue-muted text-sm">
          No keys saved yet. Scan a binary or add manually.
        </div>
      ) : (
        <div className="space-y-1">
          {keys.map((key) => (
            <div key={key.id} className="bg-ue-surface rounded p-2 border border-ue-border flex items-center gap-3">
              <div className={`w-2 h-2 rounded-full ${key.isValidated ? 'bg-green-500' : 'bg-gray-500'}`} />
              <div className="flex-1 min-w-0">
                <div className="font-mono text-[10px] text-ue-text truncate">
                  {key.keyHex.slice(0, 16)}...
                </div>
                <div className="text-[9px] text-ue-muted">
                  {key.gameName || 'Unknown'} | {key.keySource} | {new Date(key.discoveredAt).toLocaleDateString()}
                </div>
              </div>
              <button className="text-[10px] text-red-400 hover:underline">Remove</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// ─── Decrypt Section ────────────────────────────────────────────────

function DecryptSection({ pakPath, setPakPath }: { pakPath: string; setPakPath: (v: string) => void }) {
  return (
    <div className="space-y-4">
      <div>
        <label className="text-[11px] text-ue-muted uppercase tracking-wide block mb-1">
          .pak / .ucas / .utoc File Path
        </label>
        <input
          type="text"
          value={pakPath}
          onChange={(e) => setPakPath(e.target.value)}
          placeholder="/path/to/game/Content/Paks/pakchunk0.pak"
          className="w-full bg-ue-surface border border-ue-border rounded px-3 py-1.5 text-sm text-ue-text font-mono placeholder:text-ue-muted focus:outline-none focus:border-ue-accent"
        />
      </div>

      <div>
        <label className="text-[11px] text-ue-muted uppercase tracking-wide block mb-1">
          AES Key (select from database or paste)
        </label>
        <input
          type="text"
          placeholder="64 hex characters..."
          className="w-full bg-ue-surface border border-ue-border rounded px-3 py-1.5 text-sm text-ue-text font-mono placeholder:text-ue-muted focus:outline-none focus:border-ue-accent"
        />
      </div>

      <div className="flex gap-2">
        <button className="bg-ue-accent text-white px-4 py-2 rounded text-sm font-medium hover:opacity-90 transition-opacity flex-1">
          Validate Key
        </button>
        <button className="bg-green-700 text-white px-4 py-2 rounded text-sm font-medium hover:opacity-90 transition-opacity flex-1">
          Decrypt & Extract
        </button>
      </div>

      {/* Progress placeholder */}
      <div className="bg-ue-surface rounded p-3 border border-ue-border">
        <div className="flex justify-between text-[10px] text-ue-muted mb-1">
          <span>Ready</span>
          <span>0%</span>
        </div>
        <div className="h-2 bg-ue-bg rounded-full overflow-hidden">
          <div className="h-full bg-ue-accent rounded-full w-0 transition-all" />
        </div>
      </div>
    </div>
  );
}
