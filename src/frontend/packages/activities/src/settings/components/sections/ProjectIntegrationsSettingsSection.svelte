<!-- Section: Settings > ProjectIntegrationsSettingsSection -->
<script lang="ts">
  /**
   * Project Integrations Settings Page
   * Git repository connection and webhook management
   */

  import { untrack } from 'svelte';
  import { config as appConfig, toast, SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState } from '@sddp/shell';
  import { Icon, IconButton, Switch, Input, CheckboxList, Button, Spinner } from '@sddp/ui';
  import type { ProjectIntegration, Webhook } from '../../types';
  import { SettingGroupHeader } from '../idioms';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId: _tenantId = '', projectId: _projectId, tabId = '' }: Props = $props();

  interface IntegrationsTabState {
    integrations: ProjectIntegration[];
    webhooks: Webhook[];
    newWebhookUrl: string;
    newWebhookEvents: string[];
  }

  const tabStateKey = $derived(tabId || `settings-project-${_projectId}-integrations`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);
  const projectIntegrationsEnabled = appConfig.get('enableProjectIntegrations');

  let integrations = $state<ProjectIntegration[]>([]);
  let webhooks = $state<Webhook[]>([]);
  let loading = $state(false);

  let newWebhookUrl = $state('');
  let newWebhookEvents = $state<string[]>([]);

  const availableEvents = [
    'spec.created',
    'spec.updated',
    'spec.approved',
    'requirement.created',
    'requirement.updated',
    'conversation.message',
  ];

  const availableEventOptions = availableEvents.map((e) => ({ value: e, label: e }));

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    if (!projectIntegrationsEnabled) {
      integrations = [];
      webhooks = [];
      newWebhookUrl = '';
      newWebhookEvents = [];
      restoredKey = tabStateKey;
      isRestored = true;
      return;
    }

    const saved = getTabState<IntegrationsTabState>(tabStateKey);
    if (saved?.integrations && saved.integrations.length > 0) {
      integrations = saved.integrations;
      webhooks = saved.webhooks ?? [];
      newWebhookUrl = saved.newWebhookUrl ?? '';
      newWebhookEvents = saved.newWebhookEvents ?? [];
    } else {
      untrack(() => loadData());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<IntegrationsTabState>(tabStateKey, {
      integrations: JSON.parse(JSON.stringify(integrations)),
      webhooks: JSON.parse(JSON.stringify(webhooks)),
      newWebhookUrl,
      newWebhookEvents: [...newWebhookEvents],
    });
  });

  async function loadData() {
    if (!projectIntegrationsEnabled) {
      loading = false;
      integrations = [];
      webhooks = [];
      return;
    }

    loading = true;
    integrations = [
      { type: 'git', enabled: false, config: {} },
      // Jira, Slack
      // { type: 'jira', enabled: false, config: {} },
      // { type: 'slack', enabled: true, config: { channel: '#sddp-notifications' } },
    ];
    webhooks = [
      {
        id: '1',
        url: 'https://example.com/webhook',
        events: ['spec.created', 'spec.approved'],
        enabled: true,
      },
    ];
    loading = false;
  }

  async function toggleIntegration(type: string) {
    if (!projectIntegrationsEnabled) {
      toast.info('Project integrations are coming soon.');
      return;
    }

    integrations = integrations.map((i) =>
      i.type === type ? { ...i, enabled: !i.enabled } : i
    );
    toast.info('Integration toggle not yet implemented (backend API pending)');
  }

  async function addWebhook() {
    if (!projectIntegrationsEnabled) {
      toast.info('Project integrations are coming soon.');
      return;
    }

    if (!newWebhookUrl.trim() || newWebhookEvents.length === 0) return;

    const webhook: Webhook = {
      id: Date.now().toString(),
      url: newWebhookUrl.trim(),
      events: [...newWebhookEvents],
      enabled: true,
    };

    webhooks = [...webhooks, webhook];
    newWebhookUrl = '';
    newWebhookEvents = [];
    toast.info('Webhook added locally (backend API pending)');
  }

  async function toggleWebhook(id: string) {
    if (!projectIntegrationsEnabled) {
      toast.info('Project integrations are coming soon.');
      return;
    }

    webhooks = webhooks.map((w) => (w.id === id ? { ...w, enabled: !w.enabled } : w));
    toast.info('Webhook toggle not yet implemented (backend API pending)');
  }

  async function deleteWebhook(id: string) {
    if (!projectIntegrationsEnabled) {
      toast.info('Project integrations are coming soon.');
      return;
    }

    if (!confirm('Delete this webhook?')) return;
    webhooks = webhooks.filter((w) => w.id !== id);
    toast.info('Webhook deleted locally (backend API pending)');
  }

</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else}
  <PageHeader title="Integrations" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={loadData} />
    {/snippet}
  </PageHeader>

  <SingleColumnLayout>
    {#if !projectIntegrationsEnabled}
      <SettingGroupHeader title="Connected Services" />
      <p class="section-desc">
        Coming Soon. Project integration management is disabled in this environment.
      </p>
      <div class="coming-soon-panel">
        <Icon name="clock-3" size="sm" />
        <span>Enable `VITE_ENABLE_PROJECT_INTEGRATIONS=true` to expose this screen.</span>
      </div>
    {:else}
      <!-- Connected Services -->
      <SettingGroupHeader title="Connected Services" />

      {#each integrations as integration (integration.type)}
        <div class="integration-row">
          <div class="integration-info">
            <div class="integration-icon">
              <Icon name="git-branch" size="sm" />
            </div>
            <div>
              <div class="integration-name">Git Repository</div>
              <div class="integration-desc">Connect a Git repository for version control and Spec Drift detection.</div>
            </div>
          </div>
          <Switch checked={integration.enabled} onchange={() => toggleIntegration(integration.type)} />
        </div>
      {/each}

      <!-- Webhooks -->
      <SettingGroupHeader title="Webhooks" />
      <p class="section-desc">Receive HTTP notifications when events occur in this project.</p>

      {#if webhooks.length > 0}
        <div class="webhooks-list">
          {#each webhooks as webhook (webhook.id)}
            <div class="webhook-item">
              <div class="webhook-info">
                <span class="webhook-url">{webhook.url}</span>
                <div class="webhook-events">
                  {#each webhook.events as event (event)}
                    <span class="event-tag">{event}</span>
                  {/each}
                </div>
              </div>
              <div class="webhook-actions">
                <Switch checked={webhook.enabled} size="sm" onchange={() => toggleWebhook(webhook.id)} />
                <IconButton icon="trash" variant="danger" size="sm" title="Delete" onclick={() => deleteWebhook(webhook.id)} />
              </div>
            </div>
          {/each}
        </div>
      {:else}
        <p class="empty-state">No webhooks configured.</p>
      {/if}

      <!-- Add Webhook -->
      <div class="add-webhook-section">
        <div class="add-webhook-header">Add Webhook</div>
        <div class="add-webhook-form">
          <div class="form-field">
            <span class="field-label">URL</span>
            <Input
              type="url"
              placeholder="https://example.com/webhook"
              bind:value={newWebhookUrl}
            />
          </div>
          <fieldset class="contents">
            <legend class="sr-only">Webhook Events</legend>
            <div class="form-field">
              <span class="field-label">Events</span>
              <CheckboxList
                options={availableEventOptions}
                bind:selected={newWebhookEvents}
                orientation="horizontal"
                size="sm"
              />
            </div>
          </fieldset>
          <Button
            variant="primary"
            size="sm"
            onclick={addWebhook}
            disabled={!newWebhookUrl.trim() || newWebhookEvents.length === 0}
            class="btn-align-end"
          >
            Add Webhook
          </Button>
        </div>
      </div>
    {/if}
  </SingleColumnLayout>
  {/if}
</PageShell>

<style>
  .coming-soon-panel {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.625rem 0.875rem;
    border: 1px solid var(--color-warning-200, #fde68a);
    border-radius: 6px;
    background: var(--color-warning-50, #fffbeb);
    color: var(--color-warning-700, #a16207);
    font-size: 0.8125rem;
  }

  /* Integration row */
  .integration-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 0;
    border-bottom: 1px solid var(--color-border-secondary, #f3f4f6);
  }

  .integration-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .integration-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    border-radius: 6px;
    background: var(--color-bg-tertiary, #f3f4f6);
    color: var(--color-text-secondary, #6b7280);
    flex-shrink: 0;
  }

  .integration-name {
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
  }

  .integration-desc {
    font-size: 0.75rem;
    color: var(--color-text-tertiary, #9ca3af);
    margin-top: 0.0625rem;
  }


  /* Section desc */
  .section-desc {
    font-size: 0.75rem;
    color: var(--color-text-tertiary, #9ca3af);
    margin: -0.25rem 0 0.75rem;
  }

  /* Webhooks list */
  .webhooks-list {
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 6px;
    overflow: hidden;
    margin-bottom: 1rem;
  }

  .webhook-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid var(--color-border-secondary, #f3f4f6);
  }

  .webhook-item:last-child {
    border-bottom: none;
  }

  .webhook-info {
    flex: 1;
    min-width: 0;
  }

  .webhook-url {
    font-family: monospace;
    font-size: 0.75rem;
    color: var(--color-text-primary, #111);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    display: block;
  }

  .webhook-events {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem;
    margin-top: 0.25rem;
  }

  .event-tag {
    padding: 0.0625rem 0.375rem;
    background: var(--color-bg-tertiary, #f3f4f6);
    border-radius: 3px;
    font-size: 0.625rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .webhook-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-shrink: 0;
    margin-left: 0.75rem;
  }


  .empty-state {
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
    margin-bottom: 1rem;
  }

  /* Add Webhook */
  .add-webhook-section {
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 6px;
    overflow: hidden;
  }

  .add-webhook-header {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-text-secondary, #6b7280);
    padding: 0.5rem 0.75rem;
    background: var(--color-bg-secondary, #f9fafb);
    border-bottom: 1px solid var(--color-border-primary, #e5e7eb);
  }

  .add-webhook-form {
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .form-field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .field-label {
    font-size: 0.75rem;
    font-weight: 500;
    color: var(--color-text-secondary, #6b7280);
  }

  .add-webhook-form :global(.btn-align-end) {
    align-self: flex-end;
  }
</style>
