<!--
  InlineCategoryInput Component
  Inline input for creating a new task category in the sidebar
-->
<script lang="ts">
  import { Input, IconButton } from '@sddp/ui';

  interface Props {
    onSubmit: (name: string) => void;
    onCancel: () => void;
  }

  let { onSubmit, onCancel }: Props = $props();

  let value = $state('');
  let containerEl = $state<HTMLDivElement | null>(null);

  const isValid = $derived(value.trim().length > 0);

  function handleSubmit() {
    if (isValid) {
      onSubmit(value.trim());
      value = '';
    }
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSubmit();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      onCancel();
    }
  }

  $effect(() => {
    // Autofocus once the container is mounted
    const input = containerEl?.querySelector('input');
    if (input) input.focus();
  });
</script>

<div class="flex items-center gap-1 px-2 py-1" bind:this={containerEl}>
  <div class="flex-1 min-w-0">
    <Input
      type="text"
      placeholder="Category name..."
      bind:value
      size="sm"
      variant="flat"
      class="w-full text-sm"
      onkeydown={handleKeydown}
    />
  </div>
  <IconButton
    icon="check"
    size="sm"
    variant={isValid ? 'success' : 'ghost'}
    title="Create"
    disabled={!isValid}
    onclick={handleSubmit}
  />
  <IconButton
    icon="x"
    size="sm"
    variant="ghost"
    title="Cancel"
    onclick={onCancel}
  />
</div>
