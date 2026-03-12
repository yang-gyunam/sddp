<!--
  InviteMembersModal Component
  Modal for searching and inviting users to a conversation via SignalR toast.
  Single-click: select a user to immediately send an invitation.
-->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SearchField, Spinner } from '@sddp/ui';
  import { Modal, Avatar, EmptyState, UserListItem, toast } from '@sddp/shell';
  import { getConversationService, type InvitableUser } from '../../services/ConversationService';

  interface Props {
    open: boolean;
    tenantId: string;
    conversationId: string;
    conversationName: string;
    onClose: () => void;
    onSendInvitation: (userId: string, displayName: string) => Promise<void>;
  }

  let {
    open,
    tenantId,
    conversationId,
    conversationName,
    onClose,
    onSendInvitation,
  }: Props = $props();

  let searchQuery = $state('');
  let users = $state<InvitableUser[]>([]);
  let loading = $state(false);
  let sendingUserId = $state<string | null>(null);

  async function loadUsers(search?: string): Promise<void> {
    loading = true;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      users = await service.getInvitableUsers(conversationId, search);
    } catch {
      users = [];
    } finally {
      loading = false;
    }
  }

  $effect(() => {
    if (open) {
      searchQuery = '';
      sendingUserId = null;
      untrack(() => loadUsers());
    }
  });

  function handleSearch(query: string): void {
    loadUsers(query || undefined);
  }

  async function handleSelectUser(user: InvitableUser): Promise<void> {
    if (sendingUserId) return;
    sendingUserId = user.id;
    try {
      await onSendInvitation(user.id, user.displayName);
      toast.success(`Invitation sent to ${user.displayName}`);
      // Remove invited user from list
      users = users.filter((u) => u.id !== user.id);
    } catch {
      toast.error(`Failed to send invitation to ${user.displayName}`);
    } finally {
      sendingUserId = null;
    }
  }
</script>

<Modal
  {open}
  title="Invite to {conversationName}"
  size="md"
  onClose={onClose}
>
  <SearchField
    bind:value={searchQuery}
    placeholder="Search users by name or email..."
    onSearch={handleSearch}
    class="mb-3"
  />

  <div class="space-y-1 max-h-[300px] overflow-y-auto">
    {#if loading}
      <div class="flex justify-center py-4"><Spinner size="sm" /></div>
    {:else if users.length === 0}
      <EmptyState icon="circle-user-round" heading="No users found" iconSize="lg" class="py-4" />
    {:else}
      {#each users as user (user.id)}
        <UserListItem
          user={{ displayName: user.displayName, email: user.email, isAi: user.isAi }}
          onclick={() => handleSelectUser(user)}
          density="compact"
          separator="none"
          disabled={sendingUserId === user.id}
        >
          {#snippet leading()}
            <Avatar name={user.displayName} isAI={user.isAi} size="sm" />
          {/snippet}
          {#snippet trailing()}
            {#if sendingUserId === user.id}
              <Spinner size="sm" />
            {/if}
          {/snippet}
        </UserListItem>
      {/each}
    {/if}
  </div>
</Modal>
