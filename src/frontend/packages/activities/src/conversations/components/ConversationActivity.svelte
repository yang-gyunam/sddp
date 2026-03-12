<!--
  ConversationActivity Component
  Screen ID: ACT-CONV-001
  Main activity container for Conversations
  (Renamed from DiscussionActivity.svelte)
-->
<script lang="ts">
  import { GlobalConversationsPage } from './pages';
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

<div class="conversation-activity h-full {className}">
  <GlobalConversationsPage {tenantId} {projectId} class="h-full" />
</div>
