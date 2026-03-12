<!-- Activity: Settings > Nav: Project Integrations (settings-project-{id}-integrations) -->
<script lang="ts">
  import ProjectIntegrationsSettingsSection from '../sections/ProjectIntegrationsSettingsSection.svelte';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId = '', projectId, tabId = '' }: Props = $props();
</script>

<ProjectIntegrationsSettingsSection {tenantId} {projectId} {tabId} />
