<!-- Section: Settings > ProjectGeneralSettingsSection -->
<script lang="ts">
  /**
   * Project General Settings Page
   * VS Code-style flat settings layout
   */

  import { untrack } from 'svelte';
  import { config as appConfig, toast, SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState, formatDateByPreference } from '@sddp/shell';
  import { Icon, IconButton, Input, Switch, Textarea, Button, Spinner } from '@sddp/ui';
  import type { ProjectSettings, ProjectSnapshot } from '../../types';
  import { type ProjectStatus, PROJECT_STATUS_CONFIG } from '../../../projects/types';
  import { getProjectService } from '../../../projects/services';
  import { resetProjectData } from '../../../projects/services/ProjectService';
  import { getSnapshotService } from '../../services/snapshotService';
  import { getSystemConfigService } from '../../services/SystemConfigService';
  import { getAiStatus } from '../../../ai/services/AiStatusService';
  import { SettingItem, SettingGroupHeader, ConfirmResetModal } from '../idioms';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId = '', projectId, tabId = '' }: Props = $props();

  interface GeneralTabState {
    settings: ProjectSettings | null;
    snapshots: ProjectSnapshot[];
  }

  const projectService = getProjectService();
  const snapshotService = getSnapshotService();
  const systemConfigService = getSystemConfigService();

  let settings = $state<ProjectSettings | null>(null);
  let snapshots = $state<ProjectSnapshot[]>([]);
  let loading = $state(false);
  let saving = $state(false);
  let snapshotLoading = $state(false);
  let error = $state<string | null>(null);
  let busy = $derived(loading || saving || snapshotLoading);
  let snapshotName = $state('');
  let projectStatus = $state<ProjectStatus>('planning');
  let projectCode = $state('');
  let lifecycleLoading = $state(false);
  let showResetModal = $state(false);
  let resetLoading = $state(false);

  // AI feature toggle state
  let aiSystemEnabled = $state(true);
  let aiProjectEnabled = $state(true);
  let aiToggleSaving = $state(false);
  const aiFeatureFlagEnabled = appConfig.get('enableAiFeatures');

  const tabStateKey = $derived(tabId || `settings-project-${projectId}-general`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<GeneralTabState>(tabStateKey);
    if (saved?.settings) {
      settings = saved.settings;
      snapshots = saved.snapshots ?? [];
    } else if (tenantId && projectId) {
      untrack(() => {
        loadSettings();
        loadSnapshots();
        loadAiStatus();
      });
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<GeneralTabState>(tabStateKey, {
      settings: settings ? { ...settings } : null,
      snapshots: [...snapshots],
    });
  });

  async function loadSettings() {
    if (!tenantId || !projectId) {
      error = 'Tenant ID and Project ID are required';
      return;
    }

    loading = true;
    error = null;

    try {
      projectService.setTenantId(tenantId);
      const projectDetail = await projectService.getProjectById(projectId);

      projectStatus = (projectDetail.status as ProjectStatus) || 'planning';
      projectCode = projectDetail.code ?? '';
      settings = {
        id: projectDetail.id,
        name: projectDetail.name,
        description: projectDetail.description || '',
        status: projectDetail.status as ProjectSettings['status'],
      };
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load project settings';
      toast.error('Failed to load project settings');
      settings = null;
    } finally {
      loading = false;
    }
  }

  async function loadSnapshots() {
    if (!tenantId || !projectId) return;

    try {
      snapshots = await snapshotService.getProjectSnapshots(tenantId, projectId);
    } catch {
      // Silent fail — snapshots section will show empty
      snapshots = [];
    }
  }

  async function handleSave() {
    if (!settings || !tenantId) return;
    saving = true;

    try {
      const success = await projectService.updateProject(projectId, {
        name: settings.name,
        description: settings.description,
      }).then(() => true, () => false);
      if (success) toast.success('Project settings saved');
      else toast.error('Failed to save project settings');
    } finally {
      saving = false;
    }
  }

  async function handleCreateSnapshot() {
    if (!tenantId || !projectId) return;
    const name = snapshotName.trim();
    if (!name) {
      toast.warning('Snapshot name is required');
      return;
    }

    snapshotLoading = true;
    try {
      await snapshotService.createProjectSnapshot(tenantId, projectId, { name });
      snapshotName = '';
      toast.success('Snapshot created');
      await loadSnapshots();
    } catch {
      toast.error('Failed to create snapshot');
    } finally {
      snapshotLoading = false;
    }
  }

  async function handleRestore(snapshot: ProjectSnapshot) {
    if (!tenantId || !projectId) return;
    if (!confirm(`Restore from "${snapshot.name}"?\n\nThis will replace ALL current project data with the snapshot data. A safety backup will be created automatically.`)) return;

    snapshotLoading = true;
    try {
      await snapshotService.restoreProjectSnapshot(tenantId, projectId, snapshot.id);
      toast.success('Project data restored');
      await loadSnapshots();
    } catch {
      toast.error('Failed to restore snapshot');
    } finally {
      snapshotLoading = false;
    }
  }

  async function handleDeleteSnapshot(snapshot: ProjectSnapshot) {
    if (!tenantId || !projectId) return;
    if (!confirm(`Delete snapshot "${snapshot.name}"?`)) return;

    try {
      await snapshotService.deleteProjectSnapshot(tenantId, projectId, snapshot.id);
      toast.success('Snapshot deleted');
      await loadSnapshots();
    } catch {
      toast.error('Failed to delete snapshot');
    }
  }

  function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  function tableCountSummary(counts: Record<string, number>): string {
    const total = Object.values(counts).reduce((a, b) => a + b, 0);
    const tables = Object.keys(counts).length;
    return `${total} rows / ${tables} tables`;
  }

  // Lifecycle handlers
  async function handleLifecycleAction(action: 'initialize' | 'conclude' | 'reopen' | 'archive') {
    if (!tenantId || !projectId) return;
    lifecycleLoading = true;
    try {
      projectService.setTenantId(tenantId);
      let result;
      switch (action) {
        case 'initialize': result = await projectService.initializeProject(projectId); break;
        case 'conclude': result = await projectService.concludeProject(projectId); break;
        case 'reopen': result = await projectService.reopenProject(projectId); break;
        case 'archive': result = await projectService.archiveProject(projectId); break;
      }
      projectStatus = (result.status as ProjectStatus) || 'planning';
      toast.success(`Project ${action}d successfully`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : `Failed to ${action} project`);
    } finally {
      lifecycleLoading = false;
    }
  }

  async function handleResetData() {
    if (!tenantId || !projectId || !projectCode) return;
    resetLoading = true;
    try {
      const result = await resetProjectData(tenantId, projectId, `RESET-${projectCode}`);
      showResetModal = false;
      projectStatus = 'planning';
      toast.success(`Project data reset: ${result.totalRowsDeleted} rows deleted`);
      await loadSettings();
      await loadSnapshots();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to reset project data');
    } finally {
      resetLoading = false;
    }
  }

  // AI feature toggle
  async function loadAiStatus() {
    if (!tenantId) return;
    if (!aiFeatureFlagEnabled) {
      aiSystemEnabled = false;
      aiProjectEnabled = false;
      return;
    }

    try {
      const status = await getAiStatus(tenantId, projectId);
      aiSystemEnabled = status.systemEnabled;
      aiProjectEnabled = status.projectEnabled;
    } catch {
      // Default to enabled on error
      aiSystemEnabled = true;
      aiProjectEnabled = true;
    }
  }

  async function handleAiToggle() {
    if (!tenantId || !projectId) return;
    if (!aiFeatureFlagEnabled) {
      toast.info('AI features are coming soon.');
      return;
    }

    aiToggleSaving = true;
    const newValue = !aiProjectEnabled;
    try {
      systemConfigService.setContext(tenantId, projectId);
      await systemConfigService.saveConfigValue('aiAgent', 'enabled', String(newValue));
      aiProjectEnabled = newValue;
      toast.success(newValue ? 'AI features enabled for this project' : 'AI features disabled for this project');
    } catch {
      toast.error('Failed to update AI setting');
    } finally {
      aiToggleSaving = false;
    }
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="error-state">
        <p>{error}</p>
        <Button variant="secondary" onclick={loadSettings}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="General" loading={busy}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={() => { loadSettings(); loadSnapshots(); }} />
      <IconButton icon="check" variant="success" size="sm" title="Save" onclick={handleSave} disabled={saving} />
    {/snippet}
  </PageHeader>

  <SingleColumnLayout>
  {#if settings}
    <!-- Project Information -->
    <SettingGroupHeader title="Project Information" />

    <SettingItem
      category="Project Information"
      title="Project Name"
      description="The display name of this project."
    >
      <Input type="text" bind:value={settings.name} />
    </SettingItem>

    <SettingItem
      category="Project Information"
      title="Description"
      description="Brief description of the project purpose and scope."
    >
      <Textarea bind:value={settings.description} rows={3} />
    </SettingItem>

    <!-- Project Lifecycle -->
    <SettingGroupHeader title="Project Lifecycle" />

    <SettingItem
      category="Project Lifecycle"
      title="Status"
      description={PROJECT_STATUS_CONFIG[projectStatus]?.description ?? ''}
    >
      <div class="lifecycle-section">
        <span class="status-badge {projectStatus}">
          {PROJECT_STATUS_CONFIG[projectStatus]?.label ?? projectStatus}
        </span>
        <div class="lifecycle-actions">
          {#if projectStatus === 'planning'}
            <Button variant="primary" size="sm" onclick={() => handleLifecycleAction('initialize')} disabled={lifecycleLoading}>
              Initialize Project
            </Button>
          {:else if projectStatus === 'active'}
            <Button variant="secondary" size="sm" onclick={() => handleLifecycleAction('conclude')} disabled={lifecycleLoading}>
              Conclude Project
            </Button>
          {:else if projectStatus === 'concluded'}
            <Button variant="primary" size="sm" onclick={() => handleLifecycleAction('reopen')} disabled={lifecycleLoading}>
              Reopen
            </Button>
            <Button variant="secondary" size="sm" onclick={() => handleLifecycleAction('archive')} disabled={lifecycleLoading}>
              Archive
            </Button>
          {:else if projectStatus === 'archived'}
            <span class="archived-notice">This project is archived and read-only.</span>
          {/if}
        </div>
      </div>
    </SettingItem>

    <!-- AI Features -->
    <SettingGroupHeader title="AI Features" />

    <SettingItem
      category="AI Features"
      title="AI Analysis"
      description={!aiFeatureFlagEnabled
        ? 'Coming Soon. AI runtime is temporarily disabled due to hardware constraints in this environment.'
        : aiSystemEnabled
        ? 'Enable AI-powered quality analysis and automated suggestions for this project.'
        : 'AI features are disabled by the system administrator.'}
    >
      <Switch
        checked={aiProjectEnabled}
        disabled={!aiFeatureFlagEnabled || !aiSystemEnabled || aiToggleSaving}
        onchange={handleAiToggle}
      />
    </SettingItem>

    {#if !aiFeatureFlagEnabled}
      <div class="ai-disabled-notice">
        <Icon name="clock" size="sm" />
        <span>Coming Soon: AI features are temporarily unavailable in this environment.</span>
      </div>
    {:else if !aiSystemEnabled}
      <div class="ai-disabled-notice">
        <Icon name="info" size="sm" />
        <span>AI features are disabled system-wide by the administrator. Contact your admin to enable.</span>
      </div>
    {/if}

    <!-- Snapshots -->
    <SettingGroupHeader title="Snapshots" />

    <SettingItem
      category="Snapshots"
      title="Create Snapshot"
      description="Backup all project data (specs, requirements, conversations, tasks, etc.) as a snapshot."
    >
      <div class="snapshot-create">
        <Input
          type="text"
          placeholder="Snapshot name (e.g. Before refactor)"
          bind:value={snapshotName}
          disabled={snapshotLoading}
        />
        <Button
          variant="primary"
          size="sm"
          onclick={handleCreateSnapshot}
          disabled={snapshotLoading || !snapshotName.trim()}
        >
          {#if snapshotLoading}
            Creating...
          {:else}
            Create Snapshot
          {/if}
        </Button>
      </div>
    </SettingItem>

    {#if snapshots.length > 0}
      <div class="snapshot-list">
        {#each snapshots as snapshot (snapshot.id)}
          <div class="snapshot-item">
            <div class="snapshot-info">
              <div class="snapshot-header">
                {#if snapshot.snapshotType === 'pre_restore'}
                  <span class="snapshot-badge badge-auto">auto</span>
                {/if}
                <span class="snapshot-name">{snapshot.name}</span>
              </div>
              <div class="snapshot-meta">
                <span>{formatDateByPreference(snapshot.createdAt)}</span>
                <span class="meta-sep">&middot;</span>
                <span>{formatSize(snapshot.dataSizeBytes)}</span>
                <span class="meta-sep">&middot;</span>
                <span>{tableCountSummary(snapshot.tableCounts)}</span>
              </div>
            </div>
            <div class="snapshot-actions">
              <Button
                variant="outline"
                size="sm"
                onclick={() => handleRestore(snapshot)}
                disabled={snapshotLoading}
                title="Restore from this snapshot"
              >
                Restore
              </Button>
              <IconButton
                icon="trash"
                variant="danger"
                size="sm"
                onclick={() => handleDeleteSnapshot(snapshot)}
                disabled={snapshotLoading}
                title="Delete this snapshot"
              />
            </div>
          </div>
        {/each}
      </div>
    {:else}
      <p class="snapshot-empty">No snapshots yet.</p>
    {/if}
    <!-- Danger Zone -->
    <SettingGroupHeader title="Danger Zone" />

    <SettingItem
      category="Danger Zone"
      title="Reset Project Data"
      description="Delete all specs, requirements, conversations, tasks, and other data. A backup snapshot will be created automatically before reset."
    >
      <Button variant="danger" size="sm" onclick={() => showResetModal = true} disabled={projectStatus === 'archived'}>
        Reset Data
      </Button>
    </SettingItem>
  {/if}
  </SingleColumnLayout>
  {/if}
</PageShell>

<ConfirmResetModal
  open={showResetModal}
  title="Reset Project Data"
  description="This will permanently delete all project data including specs, requirements, conversations, tasks, glossary terms, and more. A backup snapshot will be created before the reset."
  confirmationCode={`RESET-${projectCode}`}
  loading={resetLoading}
  onConfirm={handleResetData}
  onCancel={() => { showResetModal = false; }}
/>

<style>
  .error-state {
    padding: 4rem;
    text-align: center;
    color: var(--color-error-600, #dc2626);
  }

  .error-state p {
    margin-bottom: 1rem;
  }

  /* Snapshot create */
  .snapshot-create {
    display: flex;
    gap: 0.5rem;
    align-items: center;
  }

  /* Snapshot list */
  .snapshot-list {
    margin-top: 0.5rem;
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 6px;
    overflow: hidden;
  }

  .snapshot-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.625rem 0.875rem;
    border-bottom: 1px solid var(--color-border-secondary, #f3f4f6);
  }

  .snapshot-item:last-child {
    border-bottom: none;
  }

  .snapshot-info {
    flex: 1;
    min-width: 0;
  }

  .snapshot-header {
    display: flex;
    align-items: center;
    gap: 0.375rem;
  }

  .snapshot-name {
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--color-text-primary, #111);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .snapshot-badge {
    padding: 0.0625rem 0.375rem;
    border-radius: 3px;
    font-size: 0.625rem;
    font-weight: 600;
    text-transform: uppercase;
    flex-shrink: 0;
  }

  .badge-auto {
    background: var(--color-warning-100, #fef3c7);
    color: var(--color-warning-700, #a16207);
  }

  .snapshot-meta {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.6875rem;
    color: var(--color-text-tertiary, #9ca3af);
    margin-top: 0.125rem;
  }

  .meta-sep {
    color: var(--color-border-primary, #e5e7eb);
  }

  .snapshot-actions {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    flex-shrink: 0;
    margin-left: 0.5rem;
  }


  .snapshot-empty {
    margin-top: 0.25rem;
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
  }

  /* Lifecycle */
  .lifecycle-section {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .status-badge {
    padding: 0.25rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.025em;
  }

  .status-badge.planning {
    background: var(--color-info-100, #dbeafe);
    color: var(--color-info-700, #1d4ed8);
  }

  .status-badge.active {
    background: var(--color-success-100, #dcfce7);
    color: var(--color-success-700, #15803d);
  }

  .status-badge.concluded {
    background: var(--color-warning-100, #fef3c7);
    color: var(--color-warning-700, #a16207);
  }

  .status-badge.archived {
    background: var(--color-neutral-100, #f3f4f6);
    color: var(--color-neutral-600, #4b5563);
  }

  .lifecycle-actions {
    display: flex;
    gap: 0.5rem;
  }

  .archived-notice {
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
    font-style: italic;
  }

  /* AI disabled notice */
  .ai-disabled-notice {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.625rem 0.875rem;
    margin-top: 0.25rem;
    font-size: 0.8125rem;
    color: var(--color-warning-700, #a16207);
    background: var(--color-warning-50, #fffbeb);
    border: 1px solid var(--color-warning-200, #fde68a);
    border-radius: 6px;
  }

</style>
