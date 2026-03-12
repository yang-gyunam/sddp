<!-- Section: SystemDashboardPanel -->
<script lang="ts">
  /**
   * System Dashboard Panel
   * Sidebar panel for system dashboard items (admin only)
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
    { id: 'dashboard-system-stats', label: 'System Stats', icon: 'bar-chart', section: 'system-stats' },
    { id: 'dashboard-audit-logs', label: 'Audit Logs', icon: 'history', section: 'audit-logs' },
    { id: 'dashboard-health', label: 'Health Check', icon: 'shield-check', section: 'health' },
  ];

  let {
    items = defaultItems,
    selectedId = null,
    onSelect = () => {}
  }: Props = $props();
</script>

<div class="px-1">
  {#each items as item (item.id)}
    {@const isSelected = selectedId === item.id}
    <Button
      variant="unstyled"
      class="w-full flex items-center gap-2 px-2 py-1.5 text-left text-sm transition-colors border rounded
        {isSelected
          ? 'border-[var(--color-accent-primary)]/50 bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
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
