<!-- Section: Settings > SystemConfigSettingsSection -->
<script lang="ts">
  /**
   * System Config Settings Page
   * Admin page for system-wide configuration — SidebarDetailLayout
   * Left sidebar: config category list
   * Right main: settings for selected category (per-item auto-save)
   */

  import { untrack } from 'svelte';
  import { SettingItem, ConfirmResetModal } from '../idioms';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import { config as appConfig, formatNumber, formatPercent, toast, getAuthState, subscribeAuth, getTabState, setTabState, PageHeader, PageShell, SidebarDetailLayout, Dropdown, RichListItem } from '@sddp/shell';
  import { Icon, IconButton, Input, Switch, Select, Button, Spinner } from '@sddp/ui';
  import type { SystemConfig } from '../../types';
  import { getSystemConfigService } from '../../services';
  import { resetTenantData } from '../../../projects/services/ProjectService';

  interface Props {
    tenantId?: string;
    tabId?: string;
  }

  let { tenantId = '', tabId = '' }: Props = $props();

  const configService = getSystemConfigService();
  const aiFeatureFlagEnabled = appConfig.get('enableAiFeatures');
  const systemConfigAuthEnabled = appConfig.get('enableSystemConfigAuthentication');
  const systemConfigPerformanceEnabled = appConfig.get('enableSystemConfigPerformance');

  // Active config group
  type ConfigGroup = 'general' | 'auth' | 'storage' | 'performance' | 'aiAgent';

  let config = $state<SystemConfig | null>(null);
  let loading = $state(false);
  let saving = $state(false);
  let dirtyGroups = $state(new Set<ConfigGroup>());
  let error = $state<string | null>(null);
  let activeGroup = $state<ConfigGroup>('general');
  let showTenantResetModal = $state(false);
  let tenantResetLoading = $state(false);

  // Get tenantId from auth state if not provided
  let authState = $state(getAuthState());
  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      authState = state;
    });
    return unsubscribe;
  });

  const effectiveTenantId = $derived(tenantId || authState.user?.tenantId || '');

  const configGroups: { id: ConfigGroup; label: string; icon: string; desc: string; count: number }[] = [
    { id: 'general', label: 'General', icon: 'settings', desc: 'Basic system settings', count: 3 },
    ...(systemConfigAuthEnabled
      ? [{ id: 'auth' as const, label: 'Authentication', icon: 'lock', desc: 'Security and login settings', count: 4 }]
      : []),
    { id: 'storage', label: 'Storage', icon: 'hard-drive', desc: 'Database and file storage', count: 2 },
    ...(systemConfigPerformanceEnabled
      ? [{ id: 'performance' as const, label: 'Performance', icon: 'zap', desc: 'Caching and optimization', count: 3 }]
      : []),
    { id: 'aiAgent', label: 'AI Agent', icon: 'cpu', desc: 'AI model configuration', count: 4 },
  ];

  let searchQuery = $state('');

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    searchQuery = target.value;
  }

  const filteredGroups = $derived(
    configGroups.filter(g =>
      !searchQuery || g.label.toLowerCase().includes(searchQuery.toLowerCase()) || g.desc.toLowerCase().includes(searchQuery.toLowerCase())
    )
  );

  const activeGroupInfo = $derived(configGroups.find(g => g.id === activeGroup));

  $effect(() => {
    if (configGroups.some(g => g.id === activeGroup)) return;
    activeGroup = configGroups[0]?.id ?? 'general';
  });

  // Tab State Persistence
  interface ConfigTabState {
    activeGroup: ConfigGroup;
    searchQuery: string;
  }

  const tabStateKey = $derived(tabId || 'settings-system-config');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ConfigTabState>(tabStateKey);
    if (saved) {
      activeGroup = saved.activeGroup ?? 'general';
      searchQuery = saved.searchQuery ?? '';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<ConfigTabState>(tabStateKey, {
      activeGroup,
      searchQuery,
    });
  });

  // Load config when tenantId changes (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!effectiveTenantId) return;
    if (effectiveTenantId === prevLoadKey) return;
    prevLoadKey = effectiveTenantId;
    untrack(() => loadConfig());
  });


  async function loadConfig() {
    loading = true;
    error = null;

    try {
      configService.setContext(effectiveTenantId);
      config = await configService.getConfig();
      dirtyGroups = new Set();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load configuration';
      toast.error('Failed to load system configuration');
      config = {
        general: { siteName: 'SDDP', siteUrl: 'https://sddp.example.com', adminEmail: 'admin@example.com' },
        auth: { sessionTimeout: 30, strongPassword: true, twoFactorAuth: false, ssoEnabled: false },
        storage: { database: 'PostgreSQL', storageUsed: 0, storageLimit: 0 },
        performance: { cacheEnabled: true, cdnEnabled: false, compressionEnabled: true },
        aiAgent: { enabled: aiFeatureFlagEnabled, provider: 'Ollama', model: 'llama3.2', endpoint: 'http://localhost:11434' },
      };
    } finally {
      loading = false;
    }
  }

  function markDirty() {
    dirtyGroups = new Set([...dirtyGroups, activeGroup]);
  }

  const isActiveGroupDirty = $derived(dirtyGroups.has(activeGroup));
  const isActiveGroupDisabled = $derived(
    (activeGroup === 'auth' && !systemConfigAuthEnabled) ||
    (activeGroup === 'performance' && !systemConfigPerformanceEnabled) ||
    (activeGroup === 'aiAgent' && !aiFeatureFlagEnabled)
  );

  /** Save config for the active group */
  async function handleSaveGroup() {
    if (!config) return;
    if (activeGroup === 'auth' && !systemConfigAuthEnabled) {
      toast.info('Authentication settings are coming soon.');
      return;
    }
    if (activeGroup === 'performance' && !systemConfigPerformanceEnabled) {
      toast.info('Performance settings are coming soon.');
      return;
    }
    if (activeGroup === 'aiAgent' && !aiFeatureFlagEnabled) {
      toast.info('AI features are coming soon.');
      return;
    }

    saving = true;
    try {
      configService.setContext(effectiveTenantId);
      config = await configService.saveConfig(config);
      dirtyGroups = new Set();
      const label = activeGroupInfo?.label ?? activeGroup;
      toast.success(`${label} settings saved`);
    } catch {
      toast.error('Failed to save configuration');
    } finally {
      saving = false;
    }
  }

  function formatStorage(gb: number): string {
    return `${formatNumber(gb, { maximumFractionDigits: 1 })} GB`;
  }

  const tenantIdPrefix = $derived(effectiveTenantId ? effectiveTenantId.substring(0, 8) : '');

  async function handleTenantReset() {
    if (!effectiveTenantId) return;
    tenantResetLoading = true;
    try {
      const result = await resetTenantData(effectiveTenantId, `RESET-TENANT-${tenantIdPrefix}`);
      showTenantResetModal = false;
      toast.success(`Tenant data reset: ${result.projectsReset} projects reset, ${result.totalRowsDeleted} rows deleted`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to reset tenant data');
    } finally {
      tenantResetLoading = false;
    }
  }

  const hasStorageLimit = $derived(config ? config.storage.storageLimit > 0 : false);
  const storagePercent = $derived(
    config && hasStorageLimit
      ? Math.min(100, Math.round((config.storage.storageUsed / config.storage.storageLimit) * 100))
      : 0
  );
  const storagePercentLabel = $derived(
    formatPercent(storagePercent / 100, { maximumFractionDigits: 0 })
  );
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if !config && error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="p-12 text-center text-sm text-[var(--color-error-600)]">
        <p class="mb-4">{error}</p>
        <Button variant="secondary" onclick={loadConfig}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Config" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={loadConfig} disabled={loading || saving} />
    {/snippet}
  </PageHeader>

  {#if config}
    <SidebarDetailLayout
      showRightPanel={false}
      sidebarWidth={240}
      minSidebarWidth={200}
      maxSidebarWidth={360}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full min-h-0 bg-[var(--color-bg-primary)]">
          <!-- Search -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="flex items-center gap-1 w-full">
              <div class="relative flex-1">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  type="text"
                  placeholder="Search settings..."
                  value={searchQuery}
                  oninput={handleSearchInput}
                  autocomplete="off"
                  variant="flat"
                  class="pl-8 w-full"
                  size="sm"
                />
              </div>

              <!-- Filter Dropdown -->
              <Dropdown position="bottom-right">
                {#snippet trigger()}
                  <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
                {/snippet}
                <div class="py-1 min-w-[180px]">
                  <Button
                    variant="unstyled"
                    class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left hover:bg-[var(--color-bg-tertiary)] transition-colors"
                    onclick={() => {}}
                  >
                    <Icon name="settings" size="sm" class="text-[var(--color-text-tertiary)]" />
                    <span class="flex-1">All Groups</span>
                    <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                  </Button>
                </div>
              </Dropdown>
            </div>
          </div>

          <!-- Category List -->
          <div class="flex-1 overflow-y-auto pb-1">
            {#each filteredGroups as group (group.id)}
              <RichListItem
                selected={activeGroup === group.id}
                title={group.label}
                description={group.desc}
                leadingIcon={group.icon}
                leadingIconClass={activeGroup === group.id
                  ? 'text-[var(--color-accent-primary)]'
                  : 'text-[var(--color-text-tertiary)]'}
                density="compact"
                onclick={() => (activeGroup = group.id)}
              >
                {#snippet trailing()}
                  <span class="ml-auto flex-shrink-0 text-xs text-[var(--color-text-tertiary)]">{group.count}</span>
                {/snippet}
              </RichListItem>
            {/each}
          </div>

        </div>
      {/snippet}

      <!-- Main Content: Settings for active group -->
      <div class="config-detail">
        <DetailHeader>
          {#snippet leading()}
            <Icon name={activeGroupInfo?.icon ?? 'settings'} size="md" class="text-[var(--color-accent-primary)]" />
          {/snippet}
          <span class="text-sm font-semibold text-[var(--color-text-primary)]">{activeGroupInfo?.label ?? ''}</span>
          <span class="text-xs text-[var(--color-text-tertiary)]">{activeGroupInfo?.desc ?? ''}</span>
          {#snippet actions()}
            <IconButton
              icon="check"
              variant={isActiveGroupDirty ? 'success' : 'ghost'}
              title="Save"
              onclick={handleSaveGroup}
              disabled={!isActiveGroupDirty || saving || isActiveGroupDisabled}
            />
          {/snippet}
        </DetailHeader>

        <div class="config-content">
        {#if activeGroup === 'general'}

          <SettingItem category="General" title="Site Name" description="Readonly system identifier">
            <Input type="text" bind:value={config.general.siteName} disabled />
          </SettingItem>

          <SettingItem category="General" title="Site URL" description="Readonly system endpoint">
            <Input type="url" bind:value={config.general.siteUrl} disabled />
          </SettingItem>

          <SettingItem category="General" title="Admin Email">
            <Input type="email" bind:value={config.general.adminEmail} onchange={markDirty} />
          </SettingItem>

          <SettingItem
            category="General"
            title="Reset All Tenant Data"
            description="Reset all projects in this tenant. Each project will have a backup snapshot created, then all data (specs, requirements, conversations, etc.) will be deleted."
          >
            <Button variant="danger" size="sm" onclick={() => showTenantResetModal = true}>
              Reset Tenant
            </Button>
          </SettingItem>

        {:else if activeGroup === 'auth'}

          <SettingItem category="Authentication" title="Session Timeout (minutes)">
            <Input type="number" bind:value={config.auth.sessionTimeout} min="5" max="1440" onchange={markDirty} />
          </SettingItem>

          <SettingItem
            category="Authentication"
            title="Require Strong Passwords"
            description="Minimum 8 characters with mixed case, numbers, and symbols"
          >
            <Switch bind:checked={config.auth.strongPassword} onchange={() => markDirty()} />
          </SettingItem>

          <SettingItem
            category="Authentication"
            title="Two-Factor Authentication"
            description="Require 2FA for all users"
          >
            <Switch bind:checked={config.auth.twoFactorAuth} onchange={() => markDirty()} />
          </SettingItem>

          <SettingItem
            category="Authentication"
            title="SSO (Single Sign-On)"
            description="Enable SAML/OAuth authentication"
          >
            <Switch bind:checked={config.auth.ssoEnabled} onchange={() => markDirty()} />
          </SettingItem>

        {:else if activeGroup === 'storage'}

          <SettingItem category="Storage" title="Database">
            <span class="info-value">{config.storage.database}</span>
          </SettingItem>

          <SettingItem category="Storage" title="Storage Usage">
            <div class="storage-usage">
              <div class="storage-header">
                <span>
                  {formatStorage(config.storage.storageUsed)} /
                  {hasStorageLimit ? formatStorage(config.storage.storageLimit) : 'N/A'}
                </span>
              </div>
              {#if hasStorageLimit}
                <div class="storage-bar">
                  <div class="storage-fill" style="width: {storagePercent}%"></div>
                </div>
                <span class="storage-percent">{storagePercentLabel} used</span>
              {:else}
                <span class="storage-percent">No hard quota configured for this database.</span>
              {/if}
            </div>
          </SettingItem>

        {:else if activeGroup === 'performance'}

          <SettingItem
            category="Performance"
            title="Enable Caching"
            description="Cache frequently accessed data"
          >
            <Switch bind:checked={config.performance.cacheEnabled} onchange={() => markDirty()} />
          </SettingItem>

          <SettingItem
            category="Performance"
            title="CDN"
            description="Serve static assets from CDN"
          >
            <Switch bind:checked={config.performance.cdnEnabled} onchange={() => markDirty()} />
          </SettingItem>

          <SettingItem
            category="Performance"
            title="Response Compression"
            description="Compress API responses"
          >
            <Switch bind:checked={config.performance.compressionEnabled} onchange={() => markDirty()} />
          </SettingItem>

        {:else if activeGroup === 'aiAgent'}

          <SettingItem
            category="AI Agent"
            title="AI Features Enabled"
            description={aiFeatureFlagEnabled
              ? 'Enable AI-assisted analysis, suggestions, and automated insights across all projects.'
              : 'Coming Soon. AI runtime is temporarily disabled due to hardware constraints in this environment.'}
          >
            <Switch
              bind:checked={config.aiAgent.enabled}
              onchange={() => markDirty()}
              disabled={!aiFeatureFlagEnabled}
            />
          </SettingItem>

          <SettingItem category="AI Agent" title="Provider">
            <Select
              bind:value={config.aiAgent.provider}
              options={[
                { value: 'Ollama', label: 'Ollama (Local)' },
                { value: 'OpenAI', label: 'OpenAI' },
                { value: 'Azure', label: 'Azure OpenAI' },
              ]}
              onchange={() => markDirty()}
              disabled={!aiFeatureFlagEnabled || !config.aiAgent.enabled}
            />
          </SettingItem>

          <SettingItem category="AI Agent" title="Model">
            <Input
              type="text"
              bind:value={config.aiAgent.model}
              onchange={markDirty}
              disabled={!aiFeatureFlagEnabled || !config.aiAgent.enabled}
            />
          </SettingItem>

          <SettingItem category="AI Agent" title="Endpoint">
            <Input
              type="url"
              bind:value={config.aiAgent.endpoint}
              onchange={markDirty}
              disabled={!aiFeatureFlagEnabled || !config.aiAgent.enabled}
            />
          </SettingItem>

          {#if !aiFeatureFlagEnabled}
            <div class="ai-disabled-notice">
              <Icon name="clock" size="sm" />
              <span>Coming Soon: AI features are temporarily unavailable in this environment.</span>
            </div>
          {/if}
        {/if}
        </div>
      </div>
    </SidebarDetailLayout>
  {/if}
  {/if}
</PageShell>

<ConfirmResetModal
  open={showTenantResetModal}
  title="Reset All Tenant Data"
  description="This will permanently delete ALL data across ALL projects in this tenant. Each project will have a backup snapshot created automatically before the reset. All projects will be reset to Planning status."
  confirmationCode={`RESET-TENANT-${tenantIdPrefix}`}
  loading={tenantResetLoading}
  onConfirm={handleTenantReset}
  onCancel={() => { showTenantResetModal = false; }}
/>

<style>
  .config-detail {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow: hidden;
  }

  .config-content {
    flex: 1;
    padding: 0 1rem 1rem;
    overflow-y: auto;
  }

  .info-value {
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--color-text-primary, #111);
  }

  .storage-usage {
    width: 100%;
  }

  .storage-header {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
  }

  .storage-bar {
    height: 8px;
    background: var(--color-bg-tertiary, #f3f4f6);
    border-radius: 4px;
    overflow: hidden;
  }

  .storage-fill {
    height: 100%;
    background: var(--color-accent-primary, #3b82f6);
    border-radius: 4px;
    transition: width 0.3s;
  }

  .storage-percent {
    display: block;
    margin-top: 0.5rem;
    font-size: 0.8125rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .ai-disabled-notice {
    margin: 0.25rem 0 0;
    padding: 0.625rem 0.875rem;
    border-radius: 6px;
    border: 1px solid var(--color-warning-200, #fde68a);
    background: var(--color-warning-50, #fffbeb);
    color: var(--color-warning-700, #a16207);
    font-size: 0.8125rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

</style>
