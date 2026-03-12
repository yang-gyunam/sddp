<!--
  TaskActivity Component
  Root component for Tasks Activity
  Provides task management with Kanban board and effort tracking
-->
<script lang="ts">
  import MyTasksPage from './pages/MyTasksPage.svelte';
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

<div class="task-activity h-full {className}">
  <MyTasksPage {tenantId} {projectId} />
</div>

<style>
  .task-activity {
    display: flex;
    flex-direction: column;
  }
</style>
