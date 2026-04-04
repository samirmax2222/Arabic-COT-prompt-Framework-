/**
 * API client for the UAsset Horizon C# backend.
 * All communication goes through the local API server.
 */

const API_BASE = '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error || `API Error: ${res.status}`);
  }

  return res.json();
}

// ─── Asset API ──────────────────────────────────────────────────────

export const assetApi = {
  parse: (filePath: string, uexpPath?: string) =>
    request('/asset/parse', {
      method: 'POST',
      body: JSON.stringify({ filePath, uexpPath }),
    }),

  detectVersion: (filePath: string) =>
    request('/asset/detect-version', {
      method: 'POST',
      body: JSON.stringify({ filePath }),
    }),

  browse: (directoryPath: string, recursive = true) =>
    request('/asset/browse', {
      method: 'POST',
      body: JSON.stringify({ directoryPath, recursive }),
    }),
};

// ─── AES API ────────────────────────────────────────────────────────

export const aesApi = {
  scan: (filePath: string) =>
    request('/aes/scan', {
      method: 'POST',
      body: JSON.stringify({ filePath }),
    }),

  validate: (pakPath: string, keyHex: string) =>
    request('/aes/validate', {
      method: 'POST',
      body: JSON.stringify({ pakPath, keyHex }),
    }),

  decrypt: (pakPath: string, keyHex: string, outputDir?: string) =>
    request('/aes/decrypt', {
      method: 'POST',
      body: JSON.stringify({ pakPath, keyHex, outputDir }),
    }),

  getKeys: () => request('/aes/keys'),

  addKey: (keyHex: string, gameName?: string, notes?: string) =>
    request('/aes/keys', {
      method: 'POST',
      body: JSON.stringify({ keyHex, gameName, notes }),
    }),

  deleteKey: (id: string) =>
    request(`/aes/keys/${id}`, { method: 'DELETE' }),

  importKeys: (json: string) =>
    request('/aes/keys/import', {
      method: 'POST',
      body: JSON.stringify({ json }),
    }),

  exportKeys: () => request('/aes/keys/export'),
};
