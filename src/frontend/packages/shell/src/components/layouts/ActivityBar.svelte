<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import type { ActivityBarItem } from '../../types';

  interface Props {
    items?: ActivityBarItem[];
    activeItem?: string;
    onItemClick?: (id: string) => void;
    bottomItems?: ActivityBarItem[];
  }

  let {
    items = [],
    activeItem = '',
    onItemClick,
    bottomItems = [],
  }: Props = $props();

  function handleClick(id: string) {
    onItemClick?.(id);
  }

</script>

<div
  class="flex flex-col w-[var(--activity-bar-width)] h-full bg-[var(--color-bg-secondary)] border-r border-[var(--color-border)]"
  role="tablist"
  aria-label="Activity Bar"
>
  <!-- Top items -->
  <div class="flex flex-col flex-1">
    {#each items as item (item.id)}
      <Button
        variant="unstyled"
        role="tab"
        aria-selected={activeItem === item.id}
        aria-label={item.label}
        onclick={() => handleClick(item.id)}
        class="relative flex items-center justify-center w-full h-12 transition-colors cursor-pointer
          {activeItem === item.id
          ? 'text-[var(--color-text-primary)] border-l-2 border-[var(--color-accent-primary)] bg-[var(--color-bg-tertiary)]/50'
          : 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)] border-l-2 border-transparent'}
        "
        title={item.label}
      >
        <Icon name={item.icon} size="lg" />
        {#if item.badge && item.badge > 0}
          <span
            class="absolute top-2 right-2 min-w-[18px] h-[18px] px-1 text-xs font-medium rounded-full bg-[var(--color-accent-primary)] text-white flex items-center justify-center"
          >
            {item.badge > 99 ? '99+' : item.badge}
          </span>
        {/if}
      </Button>
    {/each}
  </div>

  <!-- Bottom items -->
  <div class="flex flex-col">
    {#each bottomItems as item (item.id)}
      <Button
        variant="unstyled"
        role="tab"
        aria-selected={activeItem === item.id}
        aria-label={item.label}
        onclick={() => handleClick(item.id)}
        class="relative flex items-center justify-center w-full h-12 transition-colors cursor-pointer
          {activeItem === item.id
          ? 'text-[var(--color-text-primary)] border-l-2 border-[var(--color-accent-primary)] bg-[var(--color-bg-tertiary)]/50'
          : 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)] border-l-2 border-transparent'}
        "
        title={item.label}
      >
        <Icon name={item.icon} size="lg" />
        {#if item.badge && item.badge > 0}
          <span
            class="absolute top-2 right-2 min-w-[18px] h-[18px] px-1 text-xs font-medium rounded-full bg-[var(--color-accent-primary)] text-white flex items-center justify-center"
          >
            {item.badge > 99 ? '99+' : item.badge}
          </span>
        {/if}
      </Button>
    {/each}
  </div>
</div>
