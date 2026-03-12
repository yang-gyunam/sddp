<!-- Activity: Settings > Nav: Profile (settings-profile) -->
<script lang="ts">
  import ProfileSettingsSection from '../sections/ProfileSettingsSection.svelte';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();
</script>

<ProfileSettingsSection {tabId} />
