<!--
  TabBar Component
  Reusable tab navigation control with underline and pill variants
-->
<script lang="ts">
  import type { TabItem, TabBarSize, TabBarVariant } from '../types';
  import Icon from './Icon.svelte';

  interface Props {
    /** Tab items to display */
    tabs: TabItem[];
    /** Currently active tab ID */
    activeTab: string;
    /** Size variant */
    size?: TabBarSize;
    /** Visual variant: 'underline' (border-bottom indicator) or 'pill' (rounded background) */
    variant?: TabBarVariant;
    /** Callback when tab changes */
    onchange?: (tabId: string) => void;
    /** Additional CSS classes */
    class?: string;
  }

  let {
    tabs,
    activeTab,
    size = 'sm',
    variant = 'underline',
    onchange,
    class: className = '',
  }: Props = $props();

  function handleTabClick(tabId: string) {
    if (tabId !== activeTab) {
      onchange?.(tabId);
    }
  }

  const sizeClasses: Record<TabBarSize, string> = {
    sm: 'text-xs',
    md: 'text-sm',
  };
</script>

{#if variant === 'underline'}
  <div class="flex items-center {className}">
    {#each tabs as tab (tab.id)}
      <button
        type="button"
        onclick={() => handleTabClick(tab.id)}
        disabled={tab.disabled}
        class="h-9 px-3 font-medium transition-colors flex items-center gap-1.5 border-b-2 -mb-px
          {sizeClasses[size]}
          {tab.id === activeTab
            ? 'text-[var(--color-text-primary)] border-[var(--color-accent-primary)]'
            : 'text-[var(--color-text-tertiary)] border-transparent hover:text-[var(--color-text-secondary)]'}
          {tab.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}"
      >
        {#if tab.icon}
          <Icon name={tab.icon} size="xs" />
        {/if}
        <span>{tab.label}</span>
        {#if tab.badge != null && tab.badge > 0}
          <span
            class="px-1.5 py-0.5 text-xs bg-[var(--color-accent-primary)] text-[var(--color-text-on-accent)] rounded-full min-w-[1.25rem] text-center"
          >
            {tab.badge > 99 ? '99+' : tab.badge}
          </span>
        {/if}
      </button>
    {/each}
  </div>
{:else}
  <!-- Pill variant -->
  <div class="flex items-center gap-1 p-1 rounded-lg bg-[var(--color-surface-100)] {className}">
    {#each tabs as tab (tab.id)}
      <button
        type="button"
        onclick={() => handleTabClick(tab.id)}
        disabled={tab.disabled}
        class="px-3 py-1.5 rounded-md font-medium transition-colors flex items-center gap-1.5
          {sizeClasses[size]}
          {tab.id === activeTab
            ? 'bg-[var(--color-bg-primary)] text-[var(--color-text-primary)] shadow-sm'
            : 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)]'}
          {tab.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}"
      >
        {#if tab.icon}
          <Icon name={tab.icon} size="xs" />
        {/if}
        <span>{tab.label}</span>
        {#if tab.badge != null && tab.badge > 0}
          <span
            class="px-1.5 py-0.5 text-xs bg-[var(--color-accent-primary)] text-[var(--color-text-on-accent)] rounded-full min-w-[1.25rem] text-center"
          >
            {tab.badge > 99 ? '99+' : tab.badge}
          </span>
        {/if}
      </button>
    {/each}
  </div>
{/if}
