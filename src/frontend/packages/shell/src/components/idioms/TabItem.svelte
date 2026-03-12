<!--
  TabItem Component
  Individual tab with icon, title, dirty state, and close button
-->
<script lang="ts">
  import { Icon, IconButton, Button } from '@sddp/ui';

  interface Props {
    id?: string;
    title: string;
    meta?: string;
    icon?: string;
    active?: boolean;
    localActive?: boolean;
    dirty?: boolean;
    closable?: boolean;
    disabled?: boolean;
    draggable?: boolean;
    isDragging?: boolean;
    class?: string;
    onSelect?: () => void;
    onClose?: () => void;
    ondragstart?: (event: DragEvent) => void;
    ondragover?: (event: DragEvent) => void;
    ondragleave?: (event: DragEvent) => void;
    ondrop?: (event: DragEvent) => void;
    ondragend?: (event: DragEvent) => void;
    oncontextmenu?: (event: MouseEvent) => void;
    onmousedown?: (event: MouseEvent) => void;
  }

  let {
    id,
    title,
    meta,
    icon,
    active = false,
    localActive = false,
    dirty = false,
    closable = true,
    disabled = false,
    draggable = false,
    isDragging = false,
    class: className = '',
    onSelect,
    onClose,
    ondragstart,
    ondragover,
    ondragleave,
    ondrop,
    ondragend,
    oncontextmenu,
    onmousedown,
  }: Props = $props();

  // Compute combined active state
  const isActive = $derived(active || localActive);
  const isPrimaryActive = $derived(localActive);

  function handleClick() {
    if (!disabled) {
      onSelect?.();
    }
  }

  function handleClose(event: MouseEvent) {
    event.stopPropagation();
    if (!disabled) {
      onClose?.();
    }
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleClick();
    } else if ((event.key === 'Delete' || event.key === 'Backspace') && closable && !disabled) {
      event.preventDefault();
      onClose?.();
    }
  }

  function handleMiddleClick(event: MouseEvent) {
    if (event.button === 1 && closable && !disabled) {
      event.preventDefault();
      onClose?.();
    }
  }
</script>

<div
  class="group relative inline-flex items-center h-full px-3 pt-2 pb-1.5 text-sm cursor-pointer select-none min-w-0 transition-all duration-150
    {isActive
      ? 'text-[var(--color-text-primary)] bg-[var(--color-bg-primary)] after:absolute after:bottom-0 after:left-0 after:right-0 after:h-0.5 after:bg-[var(--color-accent-primary)]'
      : 'text-[var(--color-text-secondary)] bg-[color-mix(in_srgb,var(--color-bg-secondary),var(--color-bg-primary))] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}
    {disabled ? 'opacity-50 cursor-not-allowed' : ''}
    {isDragging ? 'opacity-60 scale-95 shadow-lg z-10' : ''}
    {draggable && !disabled ? 'cursor-grab active:cursor-grabbing' : ''}
    {className}"
  role="tab"
  tabindex={disabled ? -1 : 0}
  aria-selected={isActive}
  aria-label="{title}{meta ? ` - ${meta}` : ''}{dirty ? ' (unsaved)' : ''}"
  title="{title}{meta ? ` - ${meta}` : ''}"
  draggable={draggable && !disabled}
  onclick={handleClick}
  onkeydown={handleKeydown}
  onauxclick={handleMiddleClick}
  {oncontextmenu}
  {ondragstart}
  {ondragover}
  {ondragleave}
  {ondrop}
  {ondragend}
  {onmousedown}
  data-tab-id={id}
>
  <!-- Icon -->
  {#if icon}
    <Icon name={icon} size="sm" class="mr-2 flex-shrink-0" />
  {/if}

  <!-- Title -->
  <span class="truncate max-w-32 {isPrimaryActive ? 'font-semibold' : ''}">
    {title}
  </span>

  <!-- Dirty indicator -->
  {#if dirty && !closable}
    <div class="ml-1.5 w-2 h-2 bg-[var(--color-text-primary)] rounded-full flex-shrink-0"></div>
  {/if}

  <!-- Close button or dirty indicator -->
  {#if closable}
    {#if dirty}
      <Button
        variant="unstyled"
        onclick={handleClose}
        class="ml-2 p-0.5 rounded-sm flex items-center justify-center transition-all duration-150 opacity-100 hover:bg-[var(--color-text-primary)]/10"
        disabled={disabled}
        title="Close {title}"
      >
        <div class="w-2 h-2 bg-[var(--color-text-primary)] rounded-full"></div>
      </Button>
    {:else}
      <IconButton
        icon="x"
        size="sm"
        onclick={handleClose}
        disabled={disabled}
        title="Close {title}"
        class="ml-2 {isActive
          ? 'opacity-60 hover:opacity-100'
          : 'opacity-0 group-hover:opacity-100'}"
      />
    {/if}
  {/if}
</div>
