import { getStoredToken } from './client';
import { ApiError } from './client';

const API_BASE = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5279';

export type DocumentEntityType = 'Expense' | 'Income' | 'Transaction';

export interface SupportingDocumentDto {
  id: string;
  entityType: number;
  entityId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  label: string | null;
  createdAt: string;
}

async function authenticatedFetch(path: string, options: RequestInit): Promise<Response> {
  const token = await getStoredToken();
  const url = path.startsWith('http') ? path : `${API_BASE}${path}`;
  const headers: Record<string, string> = { ...(options.headers as Record<string, string>) };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return fetch(url, { ...options, headers });
}

/**
 * Upload a supporting document (e.g. receipt photo) for an expense or income.
 * Uses multipart/form-data. Call after creating the expense/income to attach the file.
 */
export async function uploadSupportingDocument(
  entityType: DocumentEntityType,
  entityId: string,
  fileUri: string,
  fileName: string,
  mimeType: string,
  label?: string | null
): Promise<SupportingDocumentDto> {
  const formData = new FormData();
  formData.append('entityType', entityType);
  formData.append('entityId', entityId);
  formData.append('file', {
    uri: fileUri,
    name: fileName || 'receipt.jpg',
    type: mimeType || 'image/jpeg',
  } as any);
  if (label != null) formData.append('label', label);

  const res = await authenticatedFetch('/api/supportingdocuments', {
    method: 'POST',
    body: formData,
    headers: {}, // Let FormData set Content-Type with boundary
  });

  if (!res.ok) {
    const text = await res.text();
    let message = res.statusText;
    try {
      const j = JSON.parse(text);
      if (j.message) message = j.message;
    } catch {
      if (text) message = text.slice(0, 200);
    }
    throw new ApiError(res.status, message);
  }

  return res.json();
}
