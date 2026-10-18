import { fetchWithAuth } from '../../shared/api';
import type { ConversationHubCallbacks } from '../../conversations/services/ConversationHubService';

export type RunStatus =
  'queued' | 'running' | 'cancelling' | 'completed' | 'cancelled' | 'failed' | 'interrupted';
export interface ConversationRun {
  id: string;
  sessionId: string;
  inputMessageId: string;
  status: RunStatus;
  lastEventSequence: number;
  outputMessageId: string | null;
  errorCode: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}
export interface RunScope {
  tenantId: string;
  projectId: string;
  userId: string;
  containerType: 'conversation' | 'topic';
  containerId: string;
}
export interface ConversationPreview {
  runId: string;
  sequence: number;
  completedItems: number;
  answer: string;
}
export interface RunTrackerState {
  preview?: ConversationPreview | null;
  run: ConversationRun | null;
  unavailable: boolean;
}
interface Hub {
  setCallbacks(callbacks: ConversationHubCallbacks): () => void;
}
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const sequence: Record<RunStatus, number> = {
  queued: 0,
  running: 1,
  cancelling: 2,
  completed: 3,
  cancelled: 3,
  failed: 3,
  interrupted: 3,
};

function validRun(value: unknown): value is ConversationRun {
  if (!value || typeof value !== 'object') return false;
  const run = value as ConversationRun;
  return (
    [run.id, run.sessionId, run.inputMessageId].every(
      (id) => typeof id === 'string' && uuid.test(id)
    ) &&
    Object.hasOwn(sequence, run.status) &&
    sequence[run.status] === run.lastEventSequence &&
    (run.outputMessageId === null ||
      (typeof run.outputMessageId === 'string' && uuid.test(run.outputMessageId))) &&
    (run.errorCode === null || typeof run.errorCode === 'string') &&
    typeof run.createdAt === 'string' &&
    Number.isFinite(Date.parse(run.createdAt)) &&
    [run.startedAt, run.completedAt].every(
      (t) => t === null || (typeof t === 'string' && Number.isFinite(Date.parse(t)))
    ) &&
    (run.status === 'completed' ? run.outputMessageId !== null : run.outputMessageId === null) &&
    (run.lastEventSequence === 3 ? run.completedAt !== null : run.completedAt === null)
  );
}

/** One stored question in one immutable view scope. Dispose on logout or navigation. */
export class ConversationRunTracker {
  private readonly scope: Readonly<RunScope>;
  private state: RunTrackerState = { run: null, unavailable: false };
  private disposed = false;
  private runId: string | null = null;
  private confirmed: ConversationRun | null = null;
  private request: { requestId: string; inputMessageId: string } | null = null;
  private requestVersion = 0;
  private pendingReads = 0;
  private lastNotice = -1;
  private previewVersion = 0;
  private lastPreviewNotice = -1;
  private readonly unsubscribe: () => void;
  private readonly timer: ReturnType<typeof setInterval>;

  constructor(
    scope: RunScope,
    hub: Hub,
    private readonly changed: (state: RunTrackerState) => void
  ) {
    if (
      ![scope.tenantId, scope.projectId, scope.userId, scope.containerId].every((id) =>
        uuid.test(id)
      ) ||
      !['conversation', 'topic'].includes(scope.containerType)
    )
      throw new Error('Invalid AI run scope');
    this.scope = Object.freeze({ ...scope });
    this.unsubscribe = hub.setCallbacks({
      onAiRunEvent: (value) => {
        if (this.disposed || !value || typeof value !== 'object') return;
        const notice = value as { runId?: unknown; sequence?: unknown };
        if (
          notice.runId !== this.runId ||
          typeof notice.sequence !== 'number' ||
          !Number.isInteger(notice.sequence) ||
          notice.sequence < 0 ||
          notice.sequence > 3 ||
          notice.sequence <= this.lastNotice
        )
          return;
        this.lastNotice = notice.sequence;
        void this.refresh();
      },
      onAiRunPreviewEvent: (value) => {
        if (this.disposed || !value || typeof value !== 'object') return;
        const notice = value as { runId?: unknown; sequence?: unknown };
        if (
          notice.runId !== this.runId ||
          typeof notice.sequence !== 'number' ||
          !Number.isSafeInteger(notice.sequence) ||
          notice.sequence <= this.lastPreviewNotice
        )
          return;
        this.lastPreviewNotice = notice.sequence;
        void this.refreshPreview();
      },
      onConnectionStateChange: (connected) => {
        if (connected) void this.refresh();
      },
    });
    this.timer = setInterval(() => {
      if (
        this.pendingReads === 0 &&
        this.runId &&
        (!this.state.run || this.state.run.lastEventSequence < 3)
      )
        void this.refresh();
    }, 5000);
  }

  get snapshot(): RunTrackerState {
    return { ...this.state, run: this.state.run ? { ...this.state.run } : null };
  }
  private emit(): void {
    if (!this.disposed) this.changed(this.snapshot);
  }
  private accept(value: unknown, expectedId?: string): void {
    if (
      !validRun(value) ||
      (expectedId && value.id !== expectedId) ||
      (this.request && value.inputMessageId !== this.request.inputMessageId)
    )
      throw new Error('Invalid AI run response');
    const previous = this.confirmed;
    if (
      previous &&
      (previous.id !== value.id ||
        previous.sessionId !== value.sessionId ||
        previous.inputMessageId !== value.inputMessageId)
    )
      throw new Error('AI run identity changed');
    if (
      previous &&
      (value.lastEventSequence < previous.lastEventSequence ||
        (previous.lastEventSequence === 3 && value.status !== previous.status))
    )
      return;
    this.runId = value.id;
    this.confirmed = { ...value };
    const preview = value.status === 'running' ? this.state.preview : undefined;
    this.state = { run: { ...value }, unavailable: false, ...(preview ? { preview } : {}) };
    if (value.status !== 'running') this.previewVersion++;
    this.emit();
    if (value.status === 'running') void this.refreshPreview();
  }
  async create(requestId: string, inputMessageId: string): Promise<void> {
    if (this.disposed) return;
    if (!uuid.test(requestId) || !uuid.test(inputMessageId))
      throw new Error('Invalid request identity');
    if (
      this.request &&
      (this.request.requestId !== requestId || this.request.inputMessageId !== inputMessageId)
    )
      throw new Error('Create a new tracker for another question');
    this.request = { requestId, inputMessageId };
    const version = ++this.requestVersion;
    try {
      const value = await fetchWithAuth<unknown>(
        `/${this.scope.containerType === 'topic' ? 'topics' : 'conversations'}/${this.scope.containerId}/ai/runs`,
        {
          tenantId: this.scope.tenantId,
          projectId: this.scope.projectId,
          method: 'POST',
          body: { requestId, inputMessageId, agentKey: 'assistant' },
        }
      );
      if (!this.disposed && version === this.requestVersion) this.accept(value);
    } catch {
      if (!this.disposed && version === this.requestVersion) {
        this.state = { run: null, unavailable: true };
        this.emit();
      }
    }
  }
  async discover(): Promise<void> {
    if (this.disposed) return;
    const version = ++this.requestVersion;
    this.pendingReads++;
    try {
      const value = await fetchWithAuth<unknown>(
        `/${this.scope.containerType === 'topic' ? 'topics' : 'conversations'}/${this.scope.containerId}/ai/runs/latest`,
        { tenantId: this.scope.tenantId, projectId: this.scope.projectId }
      );
      if (this.disposed || version !== this.requestVersion) return;
      if (value === null) {
        this.state = { run: null, unavailable: false };
        this.emit();
      } else this.accept(value);
    } catch {
      if (!this.disposed && version === this.requestVersion) {
        this.state = { run: null, unavailable: true };
        this.emit();
      }
    } finally {
      this.pendingReads--;
    }
  }
  async restore(runId: string): Promise<void> {
    if (this.disposed) return;
    if (!uuid.test(runId) || (this.runId && this.runId !== runId))
      throw new Error('Invalid run identity');
    this.runId = runId;
    await this.read(runId);
  }
  async refresh(): Promise<void> {
    if (this.runId && !this.disposed) await this.read(this.runId);
  }
  private async read(runId: string): Promise<void> {
    const version = ++this.requestVersion;
    this.pendingReads++;
    try {
      const value = await fetchWithAuth<unknown>(`/ai/runs/${runId}`, {
        tenantId: this.scope.tenantId,
        projectId: this.scope.projectId,
      });
      if (!this.disposed && version === this.requestVersion) this.accept(value, runId);
    } catch {
      if (!this.disposed && version === this.requestVersion) {
        this.state = { run: null, unavailable: true };
        this.emit();
      }
    } finally {
      this.pendingReads--;
    }
  }
  private async refreshPreview(): Promise<void> {
    if (this.disposed || this.state.run?.status !== 'running') return;
    const runId = this.runId;
    const version = ++this.previewVersion;
    try {
      const value = await fetchWithAuth<unknown>(`/ai/runs/${runId}/preview`, {
        tenantId: this.scope.tenantId,
        projectId: this.scope.projectId,
      });
      if (
        this.disposed ||
        version !== this.previewVersion ||
        runId !== this.runId ||
        this.state.run?.status !== 'running' ||
        this.state.unavailable
      )
        return;
      if (value === null) this.state = { ...this.state, preview: null };
      else {
        const preview = value as ConversationPreview;
        if (
          !preview ||
          preview.runId !== runId ||
          !Number.isSafeInteger(preview.sequence) ||
          preview.sequence < 1 ||
          !Number.isInteger(preview.completedItems) ||
          preview.completedItems < 1 ||
          preview.completedItems > 8 ||
          typeof preview.answer !== 'string' ||
          !preview.answer.trim() ||
          preview.answer.length > 8000
        )
          throw new Error('Invalid temporary response');
        if (this.state.preview && preview.sequence < this.state.preview.sequence) return;
        this.state = { ...this.state, preview: { ...preview } };
      }
      this.emit();
    } catch {
      if (!this.disposed && version === this.previewVersion) {
        this.state = { ...this.state, preview: null };
        this.emit();
      }
    }
  }
  async cancel(): Promise<void> {
    if (!this.state.run || this.disposed) return;
    const runId = this.state.run.id;
    const version = ++this.requestVersion;
    try {
      const value = await fetchWithAuth<unknown>(`/ai/runs/${runId}/cancel`, {
        tenantId: this.scope.tenantId,
        projectId: this.scope.projectId,
        method: 'POST',
      });
      if (!this.disposed && version === this.requestVersion) this.accept(value, runId);
    } catch {
      if (!this.disposed && version === this.requestVersion) {
        this.state = { run: null, unavailable: true };
        this.emit();
      }
    }
  }
  dispose(): void {
    if (this.disposed) return;
    this.disposed = true;
    this.runId = null;
    this.confirmed = null;
    this.requestVersion++;
    this.previewVersion++;
    clearInterval(this.timer);
    this.unsubscribe();
    this.state = { run: null, unavailable: false };
  }
}
