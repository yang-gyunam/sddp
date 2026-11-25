<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Button, Icon } from '@sddp/ui';
  import { playClickSound } from '../../stores/app-sounds';

  let {
    controls,
    disabled,
    loading = false,
    collapseShortcutHint = false,
    onsend,
  }: {
    controls: Snippet;
    disabled: boolean;
    loading?: boolean;
    collapseShortcutHint?: boolean;
    onsend?: () => void;
  } = $props();
</script>

<div class="@container grid grid-cols-[minmax(0,1fr)_auto] items-end gap-2 px-2 pb-2">
  <div class="flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1 self-center">
    {@render controls()}
    <span
      class="ml-auto text-[0.625rem] whitespace-nowrap text-[var(--color-text-secondary)] {collapseShortcutHint
        ? 'hidden @min-[16rem]:inline'
        : ''}"
    >
      Enter · Shift+Enter
    </span>
  </div>
  <Button
    type={onsend ? 'button' : 'submit'}
    variant="primary"
    size="icon"
    title="Send (Enter)"
    aria-label="Send (Enter)"
    {disabled}
    {loading}
    onclick={() => {
      playClickSound();
      onsend?.();
    }}
  >
    {#if !loading}<Icon name="arrow-up" size="sm" />{/if}
  </Button>
</div>
