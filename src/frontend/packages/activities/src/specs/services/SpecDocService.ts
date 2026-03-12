import { config } from '@sddp/shell/core';
import { getAccessToken, getAuthState } from '@sddp/shell/auth/stores';
import { fetchWithAuth } from '../../shared/api';
import type { SpecSummaryResult } from '../types';

interface DocRequestOptions {
  tenantId?: string;
  projectId?: string;
}

async function fetchDocText(
  endpoint: string,
  accept: string,
  options: DocRequestOptions = {}
): Promise<string> {
  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error('Not authenticated');
  }

  const tenantId = options.tenantId || getAuthState().user?.tenantId || '';
  if (!tenantId) {
    throw new Error('No tenant ID available');
  }

  const headers: Record<string, string> = {
    Authorization: `Bearer ${accessToken}`,
    'X-Tenant-Id': tenantId,
    Accept: accept,
  };

  if (options.projectId) {
    headers['X-Project-Id'] = options.projectId;
  }

  const response = await fetch(`${config.getBaseApiUrl()}${endpoint}`, {
    method: 'GET',
    headers,
    credentials: 'include',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({
      message: `Request failed: ${response.status}`,
    }));
    throw new Error(error.message || `Request failed: ${response.status}`);
  }

  return response.text();
}

export async function getSpecDocMarkdown(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<string> {
  return fetchDocText(`/docs/${specId}`, 'text/markdown', { tenantId, projectId });
}

export async function getSpecDocHtml(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<string> {
  return fetchDocText(`/docs/${specId}`, 'text/html', { tenantId, projectId });
}

export async function getSpecSummary(
  tenantId: string,
  projectId: string,
  specId: string,
  refresh: boolean = false
): Promise<SpecSummaryResult> {
  const endpoint = `/docs/${specId}/summary${refresh ? '?refresh=true' : ''}`;
  return fetchWithAuth<SpecSummaryResult>(endpoint, { tenantId, projectId });
}
