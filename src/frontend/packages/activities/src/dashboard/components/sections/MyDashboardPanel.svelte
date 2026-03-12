<!-- Section: MyDashboardPanel -->
<script lang="ts">
  /**
   * My Dashboard Panel
   * Sidebar panel for personal dashboard items
   */
  import { Button, Icon } from '@sddp/ui';

  export interface DashboardMenuItem {
    id: string;
    label: string;
    icon: string;
    section: string;
  }

  interface Props {
    items?: DashboardMenuItem[];
    selectedId?: string | null;
    onSelect?: (item: DashboardMenuItem) => void;
  }

  const defaultItems: DashboardMenuItem[] = [
    { id: 'dashboard-overview', label: 'Overview', icon: 'layout-dashboard', section: 'overview' },
    { id: 'dashboard-my-tasks', label: 'My Tasks', icon: 'check-square', section: 'my-tasks' },
    { id: 'dashboard-recent', label: 'Recent Activity', icon: 'clock', section: 'recent' },
    { id: 'dashboard-notifications', label: 'Notifications', icon: 'bell', section: 'notifications' },
  ];

  let {
    items = defaultItems,
    selectedId = null,
    onSelect = () => {}
  }: Props = $props();
</script>

<div>
  {#each items as item (item.id)}
    {@const isSelected = selectedId === item.id}
    <Button
      variant="unstyled"
      class="w-full flex items-center gap-2 px-2 py-1.5 text-left text-sm transition-colors rounded border
        {isSelected
          ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
          : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
      onclick={() => onSelect(item)}
    >
      <Icon
        name={item.icon}
        size="sm"
        class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
      />
      <span class="truncate">{item.label}</span>
    </Button>
  {/each}
</div>
