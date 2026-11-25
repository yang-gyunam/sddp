import type { AiAnalysisType, AiReport, AiReportSummary, TriggerAnalysisRequest } from '../types';

// SpecDetailView shares the service contract across editions.
export function createAiAnalysisService(_tenantId: string, _projectId: string) {
  return {
    async getReportsByTarget(_targetId: string, _type?: AiAnalysisType): Promise<AiReportSummary[]> {
      return [];
    },
    async getReportById(_reportId: string): Promise<AiReport> {
      throw new Error('AI reports are unavailable in this edition.');
    },
    async triggerAnalysis(_request: TriggerAnalysisRequest): Promise<AiReport> {
      throw new Error('AI analysis is unavailable in this edition.');
    },
  };
}

export function resetAiAnalysisService() {}
