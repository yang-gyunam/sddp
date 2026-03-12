<!-- Activity: Settings > Nav: Preferences (settings-preferences) -->
<script lang="ts">
  import PreferencesSettingsSection from '../sections/PreferencesSettingsSection.svelte';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();
</script>

<PreferencesSettingsSection {tabId} />
