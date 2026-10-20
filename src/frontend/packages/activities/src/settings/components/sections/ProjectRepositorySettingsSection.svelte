<script lang="ts">
  import { untrack } from 'svelte';
  import { Button, IconButton, Input, Badge, Spinner, Select } from '@sddp/ui';
  import {
    Modal,
    toast,
    getTabState,
    updateTabState,
    registerTabCloseGuard,
    formatDateByPreference,
    RouterService,
  } from '@sddp/shell';
  import type { GitConnections } from '../../services/GitConnectionService';
  import { SettingGroupHeader, SettingItem } from '../idioms';
  import { getProjectCapabilities } from '../../../shared/state/projectCapabilities';
  import {
    createProjectRepositoryService,
    repositoryDraft,
    type RepositoryDraft,
    type ProjectRepositorySettings,
  } from '../../services/ProjectRepositoryService';

  let {
    tenantId,
    projectId,
    tabId = '',
  }: { tenantId: string; projectId: string; tabId?: string } = $props();
  type SavedState = { scope: string; data: ProjectRepositorySettings; draft: RepositoryDraft };
  let data = $state<ProjectRepositorySettings | null>(null);
  let draft = $state<RepositoryDraft>({
    repoUrl: '',
    repoBranch: 'main',
    artifactRootPath: '',
    syncIntervalMinutes: 60,
  });
  let connections = $state<GitConnections>({ allowsPrivateRepositories: false, items: [] });
  let loading = $state(true);
  let busy = $state(false);
  let canManage = $state(false);
  let error = $state('');
  let stale = $state(false);
  let showConfirm = $state(false);
  let generation = 0;
  const scope = $derived(`${tenantId}/${projectId}`);
  const service = $derived(createProjectRepositoryService(tenantId, projectId));
  const stateKey = $derived(tabId || `settings-project-${projectId}-general`);
  const dirty = $derived(!!data && JSON.stringify(draft) !== JSON.stringify(repositoryDraft(data)));
  const editable = $derived(canManage && !data?.isReadOnly && !loading && !busy && !stale);
  const coordinatesLocked = $derived(!!data?.confirmedAt || (data?.relationshipCount ?? 0) > 0);
  const status = $derived(
    data?.confirmedAt
      ? 'Confirmed'
      : data?.canConfirm
        ? 'Connection checked'
        : data?.repoUrl
          ? 'Draft'
          : 'Not connected'
  );

  $effect(() => {
    const key = scope;
    const tabKey = stateKey;
    untrack(() => {
      const saved = getTabState<{ repository?: SavedState }>(tabKey)?.repository;
      data = saved?.scope === key ? saved.data : null;
      draft =
        saved?.scope === key
          ? { ...saved.draft }
          : { repoUrl: '', repoBranch: 'main', artifactRootPath: '', syncIntervalMinutes: 60 };
      void load(false);
    });
    return () => {
      generation++;
    };
  });
  $effect(() => {
    if (data && !loading)
      updateTabState(stateKey, { repository: { scope, data: { ...data }, draft: { ...draft } } });
  });
  $effect(() =>
    registerTabCloseGuard(tabId, {
      isDirty: () => dirty,
      describe: () => 'Git repository settings',
      save: () => perform('save'),
    })
  );

  async function load(discard: boolean) {
    const request = ++generation;
    const key = scope;
    loading = true;
    busy = false;
    error = '';
    showConfirm = false;
    try {
      const [current, authority] = await Promise.all([
        service.get(),
        getProjectCapabilities(projectId, tenantId),
      ]);
      if (request !== generation || key !== scope) return;
      canManage = authority.canWrite && authority.permissions.includes('project:update');
      connections = canManage
        ? await service.connections()
        : { allowsPrivateRepositories: false, items: [] };
      if (request !== generation || key !== scope) return;
      stale = !discard && dirty && data?.version !== current.version;
      if (!dirty || discard) draft = repositoryDraft(current);
      if (stale)
        error =
          'Repository settings changed in another tab. Reload to review the current configuration.';
      data = current;
    } catch (err) {
      if (request !== generation || key !== scope) return;
      canManage = false;
      error = err instanceof Error ? err.message : 'Unable to load repository settings.';
    } finally {
      if (request === generation && key === scope) loading = false;
    }
  }

  async function perform(action: 'save' | 'check' | 'confirm'): Promise<boolean> {
    if (!data || !editable) return false;
    const request = ++generation;
    const key = scope;
    const api = service;
    const expectedVersion = data.version;
    busy = true;
    error = '';
    try {
      const result =
        action === 'save'
          ? await api.save(
              { ...draft, syncIntervalMinutes: Number(draft.syncIntervalMinutes) },
              expectedVersion
            )
          : action === 'check'
            ? await api.check(expectedVersion)
            : await api.confirm(expectedVersion);
      if (request !== generation || key !== scope) return false;
      data = result;
      draft = repositoryDraft(result);
      showConfirm = false;
      toast.success(
        action === 'save'
          ? 'Repository settings saved'
          : action === 'check'
            ? 'Repository connection verified'
            : 'Repository confirmed'
      );
      return true;
    } catch (err) {
      if (request === generation && key === scope) {
        error = err instanceof Error ? err.message : 'Repository request failed.';
        showConfirm = false;
      }
      return false;
    } finally {
      if (request === generation && key === scope) busy = false;
    }
  }
</script>

<SettingGroupHeader title="Git Repository" />
{#if loading}
  <Spinner size="sm" />
{:else}
  <SettingItem
    category="Git Repository"
    title="Connection"
    description="Save a draft and check the connection. Confirm it before saving an Artifact or file–Spec relationship."
  >
    <div class="repository-status">
      <Badge>{status}</Badge>
      {#if data?.confirmedAt}<span>Confirmed {formatDateByPreference(data.confirmedAt)}</span>{/if}
    </div>
    {#if data?.confirmedAt}
      <p class="repository-note">
        URL, branch and artifact root are locked. New commits continue to sync. Repository migration
        requires a separate review.
      </p>
    {:else if (data?.relationshipCount ?? 0) > 0}
      <p class="repository-note">
        {data?.relationshipCount} existing file paths depend on these settings. Connection checks will
        verify their paths before confirmation.
      </p>
    {/if}
  </SettingItem>

  {#if canManage}
    <SettingItem
      category="Git Repository"
      title="Git connection"
      description="Select an administrator-managed connection allowed for this project, or connect to a public GitHub repository directly."
    >
      <Select
        label="Project Git connection"
        placeholder=""
        value={draft.gitConnectionId ?? ''}
        options={[
          { value: '', label: 'Public GitHub — no authentication' },
          ...connections.items.map((x) => ({
            value: x.id,
            label: `${x.name}${x.hasToken ? ' · Token registered' : ''}`,
          })),
        ]}
        onchange={(e) => {
          draft.gitConnectionId = e || null;
        }}
        disabled={!editable}
      />
      <p class="repository-note">
        {connections.allowsPrivateRepositories
          ? 'Public and private repositories are supported. Ask an administrator to register or replace credentials in System Config → Git Connections.'
          : 'Free edition supports public repositories only.'}
      </p>
    </SettingItem>
  {/if}
  {#if data?.visibility}<p class="repository-note">Verified visibility: {data.visibility}</p>{/if}
  <SettingItem
    category="Git Repository"
    title="Repository URL"
    description="Clone URL for the selected Git server. Never include credentials in this URL."
  >
    <label class="sr-only" for={`${stateKey}-repository-url`}> Repository URL</label>
    <Input
      id={`${stateKey}-repository-url`}
      bind:value={draft.repoUrl}
      placeholder="https://github.com/organization/repository.git"
      disabled={!editable || (!!data?.repoUrl && coordinatesLocked)}
      maxlength={2048}
    />
  </SettingItem>
  <SettingItem
    category="Git Repository"
    title="Branch"
    description="The branch used for file browsing and synchronization."
  >
    <label class="sr-only" for={`${stateKey}-repository-branch`}> Repository branch</label>
    <Input
      id={`${stateKey}-repository-branch`}
      bind:value={draft.repoBranch}
      disabled={!editable || coordinatesLocked}
      maxlength={255}
    />
  </SettingItem>
  <SettingItem
    category="Git Repository"
    title="Artifact root"
    description="Directory relative to the repository root. Leave empty to use the whole repository."
  >
    <label class="sr-only" for={`${stateKey}-repository-root`}> Artifact root path</label>
    <Input
      id={`${stateKey}-repository-root`}
      bind:value={draft.artifactRootPath}
      placeholder="Repository root"
      disabled={!editable || coordinatesLocked}
      maxlength={1024}
    />
  </SettingItem>
  <SettingItem
    category="Git Repository"
    title="Sync interval"
    description="Minimum minutes between synchronization attempts when Files or Artifacts are accessed."
  >
    <label class="sr-only" for={`${stateKey}-repository-interval`}> Sync interval (minutes)</label>
    <Input
      id={`${stateKey}-repository-interval`}
      type="number"
      bind:value={draft.syncIntervalMinutes}
      min={1}
      max={10080}
      disabled={!editable}
    />
  </SettingItem>
  <div class="repository-footer">
    {#if error}<p class="repository-error" role="alert">{error}</p>{/if}
    <div class="repository-actions">
      <Button size="sm" variant="secondary" disabled={busy} onclick={() => load(true)}
        >{dirty ? 'Discard draft and reload' : 'Reload settings'}</Button
      >
      {#if canManage && !data?.isReadOnly}
        <IconButton
          size="sm"
          variant="success"
          icon="check"
          title="Save repository"
          disabled={!editable || !dirty || !draft.repoUrl.trim()}
          onclick={() => perform('save')}
        />
        <Button
          size="sm"
          variant="secondary"
          disabled={!editable || dirty || !data?.repoUrl}
          onclick={() => perform('check')}>{busy ? 'Working…' : 'Check connection'}</Button
        >
        {#if !data?.confirmedAt}
          <Button
            size="sm"
            variant="primary"
            disabled={!editable || dirty || !data?.canConfirm}
            onclick={() => {
              showConfirm = true;
            }}>Confirm repository</Button
          >
        {/if}
      {/if}
      {#if data?.repoUrl}<Button
          size="sm"
          variant="ghost"
          onclick={() => RouterService.navigate(`/project/${projectId}/files`)}>Browse files</Button
        >{/if}
    </div>
    {#if dirty}<p class="repository-note">Save your changes before checking the connection.</p>{/if}
    {#if !canManage}<p class="repository-note">
        Project update permission is required to manage this connection.
      </p>{/if}
  </div>
{/if}

<Modal
  open={showConfirm}
  title="Confirm Git repository"
  size="md"
  closable={!busy}
  onClose={() => {
    showConfirm = false;
  }}
>
  <p>
    File relationships will use this repository. After confirmation, its URL, branch and artifact
    root are locked.
  </p>
  <dl class="repository-summary">
    <dt>URL</dt>
    <dd>{data?.repoUrl}</dd>
    <dt>Branch</dt>
    <dd>{data?.repoBranch}</dd>
    <dt>Artifact root</dt>
    <dd>{data?.artifactRootPath || 'Repository root'}</dd>
  </dl>
  <p>The sync interval and selected Git connection can still be changed.</p>
  {#snippet footer()}
    <Button
      variant="secondary"
      disabled={busy}
      onclick={() => {
        showConfirm = false;
      }}>Cancel</Button
    >
    <Button variant="primary" disabled={busy} onclick={() => perform('confirm')}
      >Confirm and lock settings</Button
    >
  {/snippet}
</Modal>

<style>
  .repository-footer {
    /* Match the bottom spacing of the surrounding SettingItem controls. */
    padding-bottom: 0.75rem;
  }
  .repository-status,
  .repository-actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
  }
  .repository-status,
  .repository-note {
    font-size: 0.8125rem;
    color: var(--color-text-secondary);
  }
  .repository-note {
    margin-top: 0.5rem;
  }
  .repository-error {
    color: var(--color-error-500);
    margin: 0.75rem 0;
    overflow-wrap: anywhere;
  }
  .repository-summary {
    margin: 1rem 0;
    font-size: 0.875rem;
  }
  .repository-summary dt {
    color: var(--color-text-secondary);
  }
  .repository-summary dd {
    margin: 0 0 0.5rem;
    overflow-wrap: anywhere;
  }
</style>
