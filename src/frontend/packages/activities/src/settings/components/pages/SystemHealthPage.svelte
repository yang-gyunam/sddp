<!-- Activity: Settings > Nav: System Health (settings-system-health) -->
<script lang="ts">
  import SystemHealthSection from '../sections/SystemHealthSection.svelte';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();
</script>

<SystemHealthSection {tabId} />
