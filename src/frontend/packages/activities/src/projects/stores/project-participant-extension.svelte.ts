export interface ExtensionMember {
  id: string;
  displayName: string;
  role: string;
  participationId?: string;
  isActive: boolean;
  history?: {
    status: 'unprepared' | 'queued' | 'running' | 'ready' | 'failed';
    sourceCount: number;
    processedCount: number;
    errorCode: string | null;
  };
}

interface ParticipantScope {
  tenantId: string;
  projectId: string;
  conversationId: string;
  userId: string;
}

/** Free edition has no additional project participant provider. */
export function createProjectParticipantExtension(_scope: () => ParticipantScope) {
  return {
    get participants(): ExtensionMember[] {
      return [];
    },
    get canManage() {
      return false;
    },
    get loading() {
      return false;
    },
    get error() {
      return '';
    },
    candidates(_query: string): ExtensionMember[] {
      return [];
    },
    async refresh(): Promise<void> {},
    async retryPreparation(_memberId: string): Promise<void> {},
    async add(_memberId: string): Promise<boolean> {
      return false;
    },
    async remove(_memberId: string): Promise<boolean> {
      return false;
    },
  };
}
