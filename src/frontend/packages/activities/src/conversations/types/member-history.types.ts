/** Display contract shared by conversation views, independent of the optional AI service. */
export interface AiMemberHistoryStatus {
  status: 'unprepared' | 'queued' | 'running' | 'ready' | 'failed';
  sourceCount: number;
  processedCount: number;
  errorCode: string | null;
}
