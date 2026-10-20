<script lang="ts">
  import { untrack } from 'svelte';
  import { Button, Input, Select, Switch, Badge, Spinner } from '@sddp/ui';
  import { createConfirmation, formatDateByPreference, toast } from '@sddp/shell';
  import { SettingGroupHeader, SettingItem } from '../idioms';
  import { getProjects } from '../../../projects/services/ProjectService';
  import {
    createGitConnectionService,
    type GitConnections,
  } from '../../services/GitConnectionService';
  let { tenantId }: { tenantId: string } = $props();
  let data = $state<GitConnections>({ allowsPrivateRepositories: false, items: [] });
  let projects = $state<{ id: string; name: string }[]>([]);
  let selectedId = $state('');
  let name = $state('');
  let projectId = $state('');
  let provider = $state('GitHub');
  let serverUrl = $state('https://github.com');
  let username = $state('');
  let token = $state('');
  let removeToken = $state(false);
  let loading = $state(true);
  let busy = $state(false);
  let error = $state('');
  let generation = 0;
  const selected = $derived(data.items.find((x) => x.id === selectedId));
  const service = $derived(createGitConnectionService(tenantId));
  const { confirmAction } = createConfirmation(() => `${tenantId}/${selectedId}`);
  $effect(() => {
    const tenant = tenantId;
    untrack(() => {
      busy = false;
      select('');
      void load(tenant);
    });
    return () => {
      generation++;
      token = '';
    };
  });
  function select(id: string) {
    selectedId = id;
    const row = data.items.find((x) => x.id === id);
    name = row?.name ?? '';
    projectId = row?.projectId ?? '';
    provider = row?.provider ?? 'GitHub';
    serverUrl = row?.serverUrl ?? 'https://github.com';
    username = row?.username ?? '';
    token = '';
    removeToken = false;
    error = '';
  }
  async function load(tenant = tenantId) {
    const request = ++generation;
    loading = true;
    // Reload discards the draft, including credentials, even if the request fails.
    select(selectedId);
    try {
      const [list, available] = await Promise.all([
        createGitConnectionService(tenant).list(),
        getProjects(tenant),
      ]);
      if (request !== generation) return;
      data = list;
      projects = available;
      // Keep the editable fields and optimistic-concurrency version on the same snapshot.
      select(list.items.some((x) => x.id === selectedId) ? selectedId : '');
    } catch (e) {
      if (request === generation)
        error = e instanceof Error ? e.message : 'Unable to load Git connections.';
    } finally {
      if (request === generation) loading = false;
    }
  }
  async function save() {
    if (busy) return;
    const request = ++generation;
    const api = service;
    const current = selected;
    const replacement = token || undefined;
    token = '';
    busy = true;
    error = '';
    try {
      const row = current
        ? await api.update(current.id, {
            name,
            username,
            token: replacement,
            removeToken,
            expectedVersion: current.version,
          })
        : await api.create({ name, projectId, provider, serverUrl, username, token: replacement });
      if (request !== generation) return;
      data = { ...data, items: [...data.items.filter((x) => x.id !== row.id), row] };
      select(row.id);
      toast.success('Git connection saved');
    } catch (e) {
      if (request === generation)
        error = e instanceof Error ? e.message : 'Unable to save Git connection.';
    } finally {
      if (request === generation) busy = false;
    }
  }
  async function remove() {
    if (!selected || busy) return;
    const row = selected;
    if (
      !(await confirmAction(
        `Remove "${row.name}"? Connections selected by a project cannot be removed.`,
        { title: 'Remove Git connection', confirmLabel: 'Remove', variant: 'danger' }
      ))
    )
      return;
    const request = ++generation;
    busy = true;
    error = '';
    try {
      await service.remove(row.id, row.version);
      if (request !== generation) return;
      data = { ...data, items: data.items.filter((x) => x.id !== row.id) };
      select('');
    } catch (e) {
      if (request === generation)
        error = e instanceof Error ? e.message : 'Unable to remove connection.';
    } finally {
      if (request === generation) busy = false;
    }
  }
</script>

<SettingGroupHeader title="Git Connections" />
<p class="mb-3 text-sm text-[var(--color-text-secondary)]">
  {data.allowsPrivateRepositories
    ? 'Public and private repositories are supported.'
    : 'Free edition supports public repositories only. Authentication tokens are unavailable.'}
</p>
{#if loading}<Spinner size="sm" />{:else}
  <SettingItem
    category="Git Connections"
    title="Connection"
    description="Each connection can be used only by its allowed project."
  >
    <Select
      label="Git connection"
      placeholder=""
      value={selectedId}
      options={[
        { value: '', label: 'New connection' },
        ...data.items.map((x) => ({ value: x.id, label: x.name })),
      ]}
      onchange={(e) => select(e)}
      disabled={busy}
    />
  </SettingItem>
  <SettingItem category="Git Connections" title="Name"
    ><Input
      label="Connection name"
      bind:value={name}
      disabled={busy}
      maxlength={100}
    /></SettingItem
  >
  <SettingItem
    category="Git Connections"
    title="Allowed project"
    description="Choose the project that may use this connection and its credential."
  >
    <Select
      label="Allowed project"
      bind:value={projectId}
      options={projects.map((x) => ({ value: x.id, label: x.name }))}
      disabled={busy || !!selected}
    />
  </SettingItem>
  <SettingItem category="Git Connections" title="Provider">
    <Select
      label="Git provider"
      bind:value={provider}
      options={[
        { value: 'GitHub', label: 'GitHub' },
        { value: 'Gitea', label: 'Gitea' },
      ]}
      disabled={busy || !!selected}
    />
  </SettingItem>
  <SettingItem
    category="Git Connections"
    title="Server URL"
    description="HTTPS server origin. Internal Gitea may use the server address configured by your operator."
  >
    <Input
      label="Git server URL"
      bind:value={serverUrl}
      disabled={busy || !!selected}
      maxlength={2048}
    />
  </SettingItem>
  <SettingGroupHeader title="Authentication" />
  <SettingItem category="Authentication" title="Credential status">
    <Badge>{selected?.hasToken ? 'Token registered' : 'No token'}</Badge>
    {#if selected?.checkedAt}<p class="mt-2 text-sm text-[var(--color-text-secondary)]">
        Last verified {formatDateByPreference(selected.checkedAt)}
      </p>{/if}
  </SettingItem>
  {#if data.allowsPrivateRepositories}
    <SettingItem category="Authentication" title="Git username"
      ><Input
        label="Git username"
        bind:value={username}
        disabled={busy}
        maxlength={200}
        autocomplete="off"
      /></SettingItem
    >
    <SettingItem
      category="Authentication"
      title={selected?.hasToken ? 'Replace token' : 'Access token'}
      description="Use a read-only token. A replacement is verified before it replaces the saved credential. Leave blank to keep it."
    >
      <Input
        label="Access token"
        type="password"
        bind:value={token}
        disabled={busy || removeToken}
        maxlength={4096}
        autocomplete="new-password"
      />
    </SettingItem>
    {#if selected?.hasToken}<SettingItem
        category="Authentication"
        title="Remove token"
        description="Allowed only when the selected repository remains accessible without authentication."
        ><Switch
          label="Remove saved token"
          bind:checked={removeToken}
          disabled={busy}
        /></SettingItem
      >{/if}
  {/if}
  <div class="flex flex-wrap gap-2 pb-3">
    <Button
      size="sm"
      disabled={busy || !name.trim() || !projectId || !serverUrl.trim()}
      onclick={save}>{busy ? 'Checking…' : 'Save connection'}</Button
    >
    <Button size="sm" variant="secondary" disabled={busy} onclick={() => load()}
      >Reload connections</Button
    >
    {#if selected}<Button size="sm" variant="danger" disabled={busy} onclick={remove}
        >Remove connection</Button
      >{/if}
  </div>
  <p class="pb-3 text-sm text-[var(--color-text-secondary)]">
    Reload connections discards unsaved changes, including any entered token.
  </p>
{/if}
{#if error}<p role="alert" class="text-sm text-[var(--color-error-500)]">{error}</p>{/if}
