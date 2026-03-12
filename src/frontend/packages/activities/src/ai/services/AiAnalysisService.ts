/**
 * AiAnalysisService - API Client for AI analysis reports
 */

import { fetchWithAuth } from '../../shared/api';
import { config as appConfig } from '@sddp/shell/core';
import type {
  AiAnalysisType,
  AiReport,
  AiReportSummary,
  TriggerAnalysisRequest,
} from '../types';

// ============================================
// AI Analysis API
// ============================================

export async function triggerAnalysis(
  tenantId: string,
  projectId: string,
  request: TriggerAnalysisRequest
): Promise<AiReport> {
  if (!appConfig.get('enableAiFeatures')) {
    throw new Error('AI features are coming soon.');
  }
  return fetchWithAuth<AiReport>('/ai/analyze', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

export async function getReportsByTarget(
  tenantId: string,
  projectId: string,
  targetId: string,
  analysisType?: AiAnalysisType
): Promise<AiReportSummary[]> {
  if (!appConfig.get('enableAiFeatures')) {
    return [];
  }
  const params = new URLSearchParams();
  if (analysisType) params.set('analysisType', analysisType);

  const queryString = params.toString();
  const endpoint = `/ai/reports/by-target/${targetId}${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<AiReportSummary[]>(endpoint, {
    tenantId,
    projectId,
  });
}

export async function getReportById(
  tenantId: string,
  projectId: string,
  reportId: string
): Promise<AiReport> {
  if (!appConfig.get('enableAiFeatures')) {
    throw new Error('AI features are coming soon.');
  }
  return fetchWithAuth<AiReport>(`/ai/reports/${reportId}`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Service Wrapper
// ============================================

export class AiAnalysisService {
  private tenantId = '';
  private projectId = '';

  setContext(tenantId: string, projectId: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  async triggerAnalysis(request: TriggerAnalysisRequest): Promise<AiReport> {
    return triggerAnalysis(this.tenantId, this.projectId, request);
  }

  async getReportsByTarget(
    targetId: string,
    analysisType?: AiAnalysisType
  ): Promise<AiReportSummary[]> {
    return getReportsByTarget(this.tenantId, this.projectId, targetId, analysisType);
  }

  async getReportById(reportId: string): Promise<AiReport> {
    return getReportById(this.tenantId, this.projectId, reportId);
  }
}

let aiAnalysisServiceInstance: AiAnalysisService | null = null;

export function getAiAnalysisService(): AiAnalysisService {
  if (!aiAnalysisServiceInstance) {
    aiAnalysisServiceInstance = new AiAnalysisService();
  }
  return aiAnalysisServiceInstance;
}

export function resetAiAnalysisService(): void {
  aiAnalysisServiceInstance = null;
}
