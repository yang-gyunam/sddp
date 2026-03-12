<!-- Activity: Settings > Nav: System Config (settings-system-config) -->
<script lang="ts">
  import SystemConfigSettingsSection from '../sections/SystemConfigSettingsSection.svelte';

  interface Props {
    tenantId?: string;
    tabId?: string;
  }

  let { tenantId = '', tabId = '' }: Props = $props();
</script>

<SystemConfigSettingsSection {tenantId} {tabId} />
