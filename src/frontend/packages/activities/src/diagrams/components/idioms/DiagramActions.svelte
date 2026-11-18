<script lang="ts">
  import { IconButton } from '@sddp/ui';
  let {
    saveTitle = 'Save diagram',
    canSave,
    dirty,
    busy,
    comparing,
    hasDiagram,
    onSave,
    onHistory,
    onDownload,
    canUndo = false,
    canRedo = false,
    onUndo,
    onRedo,
  }: {
    saveTitle?: string;
    canSave: boolean;
    dirty: boolean;
    busy: boolean;
    comparing: boolean;
    hasDiagram: boolean;
    onSave: () => void;
    onHistory: () => void;
    onDownload: () => void;
    canUndo?: boolean;
    canRedo?: boolean;
    onUndo?: () => void;
    onRedo?: () => void;
  } = $props();
</script>

<div class="flex items-center gap-1" role="group" aria-label="Diagram actions">
  {#if canSave && onUndo && onRedo}
    <IconButton
      icon="undo-2"
      source="lucide"
      size="sm"
      title="Undo"
      disabled={!canUndo || busy || comparing}
      onclick={onUndo}
    />
    <IconButton
      icon="redo-2"
      source="lucide"
      size="sm"
      title="Redo"
      disabled={!canRedo || busy || comparing}
      onclick={onRedo}
    />
  {/if}
  {#if canSave}
    <IconButton
      icon="check"
      size="sm"
      variant={dirty ? 'success' : 'ghost'}
      title={saveTitle}
      disabled={busy || !dirty || comparing}
      loading={busy}
      onclick={onSave}
    />
  {/if}
  {#if hasDiagram}
    <IconButton
      icon="history"
      size="sm"
      variant="ghost"
      title="History and compare"
      disabled={busy}
      onclick={onHistory}
    />
  {/if}
  <IconButton
    icon="download"
    size="sm"
    variant="ghost"
    title="Download draft (JSON)"
    disabled={busy}
    onclick={onDownload}
  />
</div>
