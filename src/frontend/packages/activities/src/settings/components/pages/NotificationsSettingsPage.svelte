<!-- Activity: Settings > Nav: Notifications (settings-notifications) -->
<script lang="ts">
  import NotificationsSettingsSection from '../sections/NotificationsSettingsSection.svelte';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();
</script>

<NotificationsSettingsSection {tabId} />
