const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

/**
 * UAsset Horizon -- Electron Main Process.
 * Launches the C# backend API server and the React frontend.
 */

let mainWindow;
let backendProcess;

const isDev = process.env.NODE_ENV === 'development' || !app.isPackaged;
const BACKEND_PORT = '5175';

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1440,
    height: 900,
    minWidth: 1024,
    minHeight: 600,
    title: 'UAsset Horizon',
    backgroundColor: '#1a1a1a',
    titleBarStyle: 'hiddenInset',
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js'),
    },
    icon: path.join(__dirname, '..', 'frontend', 'public', 'assets', 'icon.png'),
  });

  if (isDev) {
    mainWindow.loadURL('http://localhost:5173');
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  } else {
    mainWindow.loadFile(path.join(__dirname, '..', 'frontend', 'dist', 'index.html'));
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function startBackend() {
  const backendDir = path.join(__dirname, '..', 'backend', 'UAssetHorizon.API');

  if (isDev) {
    // In dev mode, assume backend is started separately
    console.log('[UAsset Horizon] Dev mode -- start backend manually with: dotnet run');
    return;
  }

  // Production: launch compiled backend
  const exePath = path.join(backendDir, 'bin', 'Release', 'net8.0', 'UAssetHorizon.API');
  backendProcess = spawn(exePath, [BACKEND_PORT], {
    cwd: backendDir,
    stdio: 'pipe',
  });

  backendProcess.stdout.on('data', (data) => {
    console.log(`[Backend] ${data}`);
  });

  backendProcess.stderr.on('data', (data) => {
    console.error(`[Backend Error] ${data}`);
  });

  backendProcess.on('close', (code) => {
    console.log(`[Backend] Process exited with code ${code}`);
  });
}

// ─── IPC Handlers ───────────────────────────────────────────────────

ipcMain.handle('dialog:openFile', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openFile'],
    filters: [
      { name: 'Unreal Assets', extensions: ['uasset', 'uexp', 'ubulk', 'pak', 'ucas', 'utoc'] },
      { name: 'Executables', extensions: ['exe', 'dll'] },
      { name: 'All Files', extensions: ['*'] },
    ],
  });
  return result.canceled ? null : result.filePaths[0];
});

ipcMain.handle('dialog:openDirectory', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openDirectory'],
  });
  return result.canceled ? null : result.filePaths[0];
});

// ─── App Lifecycle ──────────────────────────────────────────────────

app.whenReady().then(() => {
  startBackend();
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (backendProcess) {
    backendProcess.kill();
  }
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  if (backendProcess) {
    backendProcess.kill();
  }
});
