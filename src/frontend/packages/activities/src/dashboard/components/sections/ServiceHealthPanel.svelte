<!-- Settings and Dashboard share this adapter to the authenticated service health API. -->
<script lang="ts">
  import { StatusMonitor } from '@sddp/shell/status';
  import { getDashboardService } from '../../services/DashboardService';
  import { authStore } from '@sddp/shell/auth/stores';
  import { isApiAccessFailure } from '../../../shared/api/fetchWithAuth';
  const service = getDashboardService();
  const read = (signal: AbortSignal) => service.getSystemHealth(signal);
  const sourceKey = $derived(JSON.stringify([$authStore.user?.tenantId, $authStore.user?.id]));
  const symbols: Record<string, string> = {
    api: 'server',
    web: 'globe',
    database: 'database',
    redis: 'layers',
    git: 'git-branch',
    speech: 'mic',
    telemetry: 'bar-chart',
    inference: 'cpu',
    embeddings: 'cpu',
    decision: 'cpu',
    diagnostics: 'heart',
  };
  const groupOrder = ['Application', 'Data', 'Integrations', 'Observability', 'Instances'];
</script>

<StatusMonitor
  title="Service health"
  {read}
  {sourceKey}
  clearHistoryOnError={isApiAccessFailure}
  {groupOrder}
  icons={symbols}
/>
