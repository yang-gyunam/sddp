<!--
  ArtifactActivity Component
  Screen ID: ACT-ART-001
  Main activity container for Artifacts
-->
<script lang="ts">
  import { GlobalArtifactsPage } from './pages';
  import { getAuthState, subscribeAuth } from '@sddp/shell/auth';

  interface Props {
    class?: string;
  }

  let { class: className = '' }: Props = $props();

  // Get tenant context from auth state (reactive)
  let tenantId = $state(getAuthState().user?.tenantId || '');
  const projectId = '';

  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      tenantId = state.user?.tenantId || '';
    });
    return unsubscribe;
  });
</script>

<div class="artifact-activity h-full {className}">
  <GlobalArtifactsPage {tenantId} {projectId} class="h-full" />
</div>
