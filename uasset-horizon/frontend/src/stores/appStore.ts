import { create } from 'zustand';
import type { ParsedAsset, AesKeyEntry, AesScanResult, TabId, FileTreeItem } from '../types';

interface AppState {
  // Active tab
  activeTab: TabId;
  setActiveTab: (tab: TabId) => void;

  // Current asset
  currentAsset: ParsedAsset | null;
  setCurrentAsset: (asset: ParsedAsset | null) => void;
  isLoading: boolean;
  setIsLoading: (loading: boolean) => void;
  error: string | null;
  setError: (error: string | null) => void;

  // File tree
  fileTree: FileTreeItem[];
  setFileTree: (files: FileTreeItem[]) => void;
  selectedFilePath: string | null;
  setSelectedFilePath: (path: string | null) => void;

  // AES
  aesKeys: AesKeyEntry[];
  setAesKeys: (keys: AesKeyEntry[]) => void;
  lastScanResult: AesScanResult | null;
  setLastScanResult: (result: AesScanResult | null) => void;

  // Logs
  logs: { timestamp: string; level: string; message: string }[];
  addLog: (level: string, message: string) => void;
  clearLogs: () => void;

  // UI state
  sidebarWidth: number;
  setSidebarWidth: (width: number) => void;
  showMinimap: boolean;
  toggleMinimap: () => void;
}

export const useAppStore = create<AppState>((set) => ({
  activeTab: 'explorer',
  setActiveTab: (tab) => set({ activeTab: tab }),

  currentAsset: null,
  setCurrentAsset: (asset) => set({ currentAsset: asset }),
  isLoading: false,
  setIsLoading: (loading) => set({ isLoading: loading }),
  error: null,
  setError: (error) => set({ error }),

  fileTree: [],
  setFileTree: (files) => set({ fileTree: files }),
  selectedFilePath: null,
  setSelectedFilePath: (path) => set({ selectedFilePath: path }),

  aesKeys: [],
  setAesKeys: (keys) => set({ aesKeys: keys }),
  lastScanResult: null,
  setLastScanResult: (result) => set({ lastScanResult: result }),

  logs: [],
  addLog: (level, message) =>
    set((state) => ({
      logs: [
        ...state.logs.slice(-500), // Keep last 500 logs
        { timestamp: new Date().toISOString(), level, message },
      ],
    })),
  clearLogs: () => set({ logs: [] }),

  sidebarWidth: 280,
  setSidebarWidth: (width) => set({ sidebarWidth: width }),
  showMinimap: true,
  toggleMinimap: () => set((state) => ({ showMinimap: !state.showMinimap })),
}));
