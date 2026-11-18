<script lang="ts">
  // Free edition supplies no additional member instruction panel.
  let {
    scope: _scope,
    displayName: _displayName,
    canRead: _canRead,
    canManage: _canManage,
    isCurrent: _isCurrent,
    onClose: _onClose,
  }: {
    scope: {
      tenantId: string;
      projectId: string;
      conversationId: string;
      memberId: string;
      userId: string;
      requesterId: string;
    };
    displayName: string;
    canRead: boolean;
    canManage: boolean;
    isCurrent: () => boolean;
    onClose: () => void;
  } = $props();
</script>
