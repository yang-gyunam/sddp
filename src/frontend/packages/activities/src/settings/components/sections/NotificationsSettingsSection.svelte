<!-- Section: Settings > NotificationsSettingsSection -->
<script lang="ts">
  /**
   * Notifications Settings Page
   * Configure email and browser notification preferences - VS Code flat style
   */

  import { untrack } from 'svelte';
  import { SettingItem, SettingGroupHeader } from '../idioms';
  import { toast, SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState } from '@sddp/shell';
  import { IconButton, Switch, Select, Button, Spinner } from '@sddp/ui';
  import { fetchWithAuth } from '../../../shared/api';
  import type { NotificationSettings } from '../../types';

  interface Props {
    tabId?: string;
  }
  let { tabId = '' }: Props = $props();

  interface NotificationsTabState {
    settings: NotificationSettings | null;
  }

  const tabStateKey = $derived(tabId || 'settings-notifications');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  let settings = $state<NotificationSettings | null>(null);
  let loading = $state(false);
  let saving = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<NotificationsTabState>(tabStateKey);
    if (saved?.settings) {
      settings = saved.settings;
    } else {
      untrack(() => loadSettings());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<NotificationsTabState>(tabStateKey, {
      settings: settings ? JSON.parse(JSON.stringify(settings)) : null,
    });
  });

  async function loadSettings() {
    loading = true;
    try {
      const dto = await fetchWithAuth<NotificationSettings>('/users/me/notifications', {});
      settings = dto;
    } catch {
      settings = getDefaultSettings();
    }
    loading = false;
  }

  function getDefaultSettings(): NotificationSettings {
    return {
      email: {
        mentions: true,
        conversations: true,
        specApprovals: true,
        taskAssignments: true,
        dailyDigest: false,
      },
      browser: {
        enabled: true,
        sound: false,
        preview: true,
      },
      channels: {
        default: 'mentions',
        custom: {},
      },
    };
  }

  async function handleSave() {
    if (!settings) return;

    saving = true;
    try {
      await fetchWithAuth<NotificationSettings>('/users/me/notifications', {
        method: 'PUT',
        body: settings,
      });
      toast.success('Notification settings saved');
    } catch {
      toast.error('Failed to save notification settings');
    }
    saving = false;
  }

  function requestBrowserPermission() {
    if ('Notification' in window) {
      Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
          toast.success('Browser notifications enabled');
        } else {
          toast.warning('Browser notifications blocked');
        }
      });
    } else {
      toast.warning('Browser notifications not supported');
    }
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else}
  <PageHeader title="Notifications" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={loadSettings} />
      <IconButton icon="check" variant="success" size="sm" title="Save" onclick={handleSave} disabled={saving} />
    {/snippet}
  </PageHeader>

  <SingleColumnLayout>
  {#if settings}
    <!-- ================================================================ -->
    <!-- Email Notifications -->
    <!-- ================================================================ -->
    <SettingGroupHeader title="Email Notifications" />

      <fieldset class="contents">
        <legend class="sr-only">Email Notifications</legend>

        <SettingItem
          category="Email"
          title="Mentions"
          description="Receive an email when someone mentions you in a conversation."
        >
          <Switch bind:checked={settings.email.mentions} />
        </SettingItem>

        <SettingItem
          category="Email"
          title="Conversation Updates"
          description="Receive an email for new messages in conversations you're part of."
        >
          <Switch bind:checked={settings.email.conversations} />
        </SettingItem>

        <SettingItem
          category="Email"
          title="Spec Approvals"
          description="Receive an email when specs require your approval or are approved."
        >
          <Switch bind:checked={settings.email.specApprovals} />
        </SettingItem>

        <SettingItem
          category="Email"
          title="Task Assignments"
          description="Receive an email when tasks are assigned to you."
        >
          <Switch bind:checked={settings.email.taskAssignments} />
        </SettingItem>

        <SettingItem
          category="Email"
          title="Daily Digest"
          description="Receive a daily summary of activity across your projects."
        >
          <Switch bind:checked={settings.email.dailyDigest} />
        </SettingItem>
      </fieldset>

      <!-- ================================================================ -->
      <!-- Browser Notifications -->
      <!-- ================================================================ -->
      <SettingGroupHeader title="Browser Notifications" />

      <fieldset class="contents">
        <legend class="sr-only">Browser Notifications</legend>

        <SettingItem
          category="Browser"
          title="Enable Browser Notifications"
          description="Show desktop notifications for real-time events."
        >
          <div class="flex items-center gap-3">
            <Switch bind:checked={settings.browser.enabled} />
            <Button variant="secondary" size="sm" onclick={requestBrowserPermission}>Request Permission</Button>
          </div>
        </SettingItem>

        <SettingItem
          category="Browser"
          title="Notification Sound"
          description="Play a sound when notifications arrive."
        >
          <Switch bind:checked={settings.browser.sound} />
        </SettingItem>

        <SettingItem
          category="Browser"
          title="Show Preview"
          description="Display message content in notification popups."
        >
          <Switch bind:checked={settings.browser.preview} />
        </SettingItem>
      </fieldset>

      <!-- ================================================================ -->
      <!-- Channel Defaults -->
      <!-- ================================================================ -->
      <SettingGroupHeader title="Channel Defaults" />

      <SettingItem
        category="Channels"
        title="Default Notification Level"
        description="Controls the default notification level for all channels."
      >
        <Select
          bind:value={settings.channels.default}
          options={[
            { value: 'all', label: 'All messages' },
            { value: 'mentions', label: 'Mentions only' },
            { value: 'nothing', label: 'Nothing' },
          ]}
        />
      </SettingItem>

  {/if}
  </SingleColumnLayout>
  {/if}
</PageShell>

<style>
  .flex {
    display: flex;
  }

  .items-center {
    align-items: center;
  }

  .gap-3 {
    gap: 0.75rem;
  }
</style>
