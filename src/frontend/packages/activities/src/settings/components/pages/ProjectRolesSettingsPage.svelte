<!-- Activity: Settings > Nav: Project Roles (settings-project-{id}-roles) -->
<script lang="ts">
  import ProjectRolesSettingsSection from '../sections/ProjectRolesSettingsSection.svelte';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId = '', projectId, tabId = '' }: Props = $props();
</script>

<ProjectRolesSettingsSection {tenantId} {projectId} {tabId} />
