const { contextBridge, ipcRenderer } = require('electron');

/**
 * Preload script -- exposes safe IPC methods to the renderer process.
 */
contextBridge.exposeInMainWorld('electronAPI', {
  openFile: () => ipcRenderer.invoke('dialog:openFile'),
  openDirectory: () => ipcRenderer.invoke('dialog:openDirectory'),
  platform: process.platform,
});
