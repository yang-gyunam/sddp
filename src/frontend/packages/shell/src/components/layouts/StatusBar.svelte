<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button } from '@sddp/ui';

  interface StatusItem {
    id: string;
    text: string;
    icon?: string;
    tooltip?: string;
    onClick?: () => void;
    position?: 'left' | 'right';
    variant?: 'default' | 'error' | 'warning' | 'info';
  }

  interface Props {
    items?: StatusItem[];
    backgroundColor?: string;
    children?: import('svelte').Snippet;
  }

  let { items = [], backgroundColor, children }: Props = $props();

  const problemIds = ['errors', 'warnings', 'infos'];
  const problemItems = $derived(items.filter((item) => problemIds.includes(item.id)));
  const leftItems = $derived(items.filter((item) => item.position !== 'right' && !problemIds.includes(item.id)));
  const rightItems = $derived(items.filter((item) => item.position === 'right'));
  const statusIconStyle = 'stroke-width: 1.9';

  // Flash animation: detect count changes and trigger background flash
  let prevTexts = $state<Record<string, string>>({});
  let flashCounters = $state<Record<string, number>>({});

  const flashColors: Record<string, string> = {
    errors: 'var(--color-flash-bg, rgba(255, 255, 255, 0.16))',
    warnings: 'var(--color-flash-bg, rgba(255, 255, 255, 0.16))',
    infos: 'var(--color-flash-bg, rgba(255, 255, 255, 0.16))',
  };

  $effect(() => {
    const currentItems = problemItems;
    untrack(() => {
      const prev = prevTexts;
      const next: Record<string, string> = {};
      const updates: Record<string, number> = {};
      let hasChanges = false;

      for (const item of currentItems) {
        next[item.id] = item.text;
        if (prev[item.id] !== undefined && prev[item.id] !== item.text) {
          updates[item.id] = (flashCounters[item.id] ?? 0) + 1;
          hasChanges = true;
        }
      }

      prevTexts = next;
      if (hasChanges) {
        flashCounters = { ...flashCounters, ...updates };
      }
    });
  });

  function getFlashStyle(itemId: string): string {
    const count = flashCounters[itemId] ?? 0;
    if (count === 0) return '';
    return `animation: status-flash 1s ease-out; --flash-bg: ${flashColors[itemId] ?? 'transparent'}`;
  }

  function getItemClass(item: StatusItem, hasClick: boolean): string {
    const base = 'flex items-center gap-1 px-1.5 py-0.5 rounded transition-colors';
    const hover = hasClick ? 'hover:bg-[var(--color-neutral-50)]/10 cursor-pointer' : '';
    return `${base} ${hover} text-[var(--color-text-on-accent)]`;
  }
</script>

<div
  class="flex items-center justify-between h-[var(--status-bar-height)] px-2 text-xs border-t border-[var(--color-border)]"
  style={backgroundColor
    ? `background-color: ${backgroundColor}`
    : 'background-color: var(--color-accent-primary-700, #4338ca)'}
>
  <!-- Left items -->
  <div class="flex items-center gap-3">
    {#each leftItems as item (item.id)}
      {#if item.onClick}
        <Button
          variant="unstyled"
          onclick={item.onClick}
          class={getItemClass(item, true)}
          title={item.tooltip ?? item.text}
        >
          {#if item.icon}
            <Icon name={item.icon} size="xs" style={statusIconStyle} />
          {/if}
          <span>{item.text}</span>
        </Button>
      {:else}
        <span
          class={getItemClass(item, false)}
          title={item.tooltip ?? item.text}
        >
          {#if item.icon}
            <Icon name={item.icon} size="xs" style={statusIconStyle} />
          {/if}
          <span>{item.text}</span>
        </span>
      {/if}
    {/each}

    <!-- Problems group (errors + warnings + infos) with flash animation on count change -->
    {#if problemItems.length > 0}
      <div class="flex items-center gap-1">
        {#each problemItems as item (item.id)}
          {#key flashCounters[item.id] ?? 0}
            {#if item.onClick}
              <Button
                variant="unstyled"
                onclick={item.onClick}
                class={getItemClass(item, true)}
                title={item.tooltip ?? item.text}
              >
                {#if item.icon}
                  <Icon name={item.icon} size="xs" style={statusIconStyle} />
                {/if}
                <span>{item.text}</span>
              </Button>
            {:else}
              <span
                class={getItemClass(item, false)}
                style={getFlashStyle(item.id)}
                title={item.tooltip ?? item.text}
              >
                {#if item.icon}
                  <Icon name={item.icon} size="xs" style={statusIconStyle} />
                {/if}
                <span>{item.text}</span>
              </span>
            {/if}
          {/key}
        {/each}
      </div>
    {/if}

    {#if children}
      {@render children()}
    {/if}
  </div>

  <!-- Right items -->
  <div class="flex items-center gap-3">
    {#each rightItems as item (item.id)}
      {#if item.onClick}
        <Button
          variant="unstyled"
          onclick={item.onClick}
          class={getItemClass(item, true)}
          title={item.tooltip ?? item.text}
        >
          {#if item.icon}
            <Icon name={item.icon} size="xs" style={statusIconStyle} />
          {/if}
          <span>{item.text}</span>
        </Button>
      {:else}
        <span
          class={getItemClass(item, false)}
          title={item.tooltip ?? item.text}
        >
          {#if item.icon}
            <Icon name={item.icon} size="xs" style={statusIconStyle} />
          {/if}
          <span>{item.text}</span>
        </span>
      {/if}
    {/each}
  </div>
</div>

<style>
  @keyframes status-flash {
    from {
      background-color: var(--flash-bg);
    }
    to {
      background-color: transparent;
    }
  }
</style>
