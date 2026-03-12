<!-- Activity: Conversations > Nav: Create -->
<script lang="ts">
  import { toast, getAuthState, subscribeAuth, getTabState, setTabState } from '@sddp/shell';
  import { tabActions } from '@sddp/shell/stores';
  import { getConversationService } from '../../services/ConversationService';
  import { ConversationCreatePanel } from '../sections';

  interface Props {
    createType?: 'channel' | 'forum';
    /** Called after successful creation with the new conversation's id, type, and name */
    onCreated?: (id: string, type: 'Channel' | 'Forum', name: string) => void;
    tabId?: string;
    class?: string;
  }

  let { createType = 'channel', onCreated, tabId = '', class: className = '' }: Props = $props();

  let authState = $state(getAuthState());
  $effect(() => {
    const unsub = subscribeAuth((s) => { authState = s; });
    return unsub;
  });

  const tenantId = $derived(authState.user?.tenantId || '');

  let createName = $state('');
  let createDescription = $state('');
  let isCreating = $state(false);
  let createError = $state<string | null>(null);

  // Tab State Persistence
  interface CreateTabState {
    createName: string;
    createDescription: string;
  }

  const tabStateKey = $derived(tabId || 'conversation-create');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<CreateTabState>(tabStateKey);
    if (saved) {
      createName = saved.createName ?? '';
      createDescription = saved.createDescription ?? '';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<CreateTabState>(tabStateKey, { createName, createDescription });
  });

  const isValid = $derived(createName.trim().length > 0);
  const icon = $derived(createType === 'forum' ? 'list' : 'hash');
  const title = $derived(createType === 'forum' ? 'New Forum' : 'New Channel');

  function handleCancel() {
    // Close the current tab
    if (tabId) {
      tabActions.closeTab(tabId);
    }
  }

  async function handleSubmit() {
    if (!createName.trim()) {
      createError = 'Name is required.';
      return;
    }
    isCreating = true;
    createError = null;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      const convType = createType === 'forum' ? 'Forum' : 'Channel';
      const created = await service.createGlobalConversation({
        name: createName.trim(),
        conversationType: convType,
        visibility: 'Public',
        description: createDescription.trim() || undefined,
      });
      toast.success(`${convType} created`);
      handleCancel();
      onCreated?.(created.id, convType as 'Channel' | 'Forum', created.name);
    } catch (err) {
      createError = err instanceof Error ? err.message : 'Failed to create.';
    } finally {
      isCreating = false;
    }
  }
</script>

{#if createType === 'forum'}
  <ConversationCreatePanel
    {icon}
    {title}
    bind:name={createName}
    bind:description={createDescription}
    nameLabel="Topic Title"
    namePlaceholder="e.g., API contract discussion"
    showDescription={false}
    loading={isCreating}
    error={createError}
    isValid={isValid}
    onSubmit={handleSubmit}
    onCancel={handleCancel}
    class={className}
  />
{:else}
  <ConversationCreatePanel
    {icon}
    {title}
    bind:name={createName}
    bind:description={createDescription}
    namePlaceholder="e.g., general"
    descriptionLabel="Topic"
    descriptionPlaceholder="What is this conversation for?"
    loading={isCreating}
    error={createError}
    isValid={isValid}
    onSubmit={handleSubmit}
    onCancel={handleCancel}
    class={className}
  />
{/if}
