<!-- Activity: Settings > Nav: Project General (settings-project-{id}-general) -->
<script lang="ts">
  import ProjectGeneralSettingsSection from '../sections/ProjectGeneralSettingsSection.svelte';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId = '', projectId, tabId = '' }: Props = $props();
</script>

<ProjectGeneralSettingsSection {tenantId} {projectId} {tabId} />
