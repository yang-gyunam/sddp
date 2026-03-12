<!-- Section: ActivityLog — Dashboard > My/System, Projects > Dashboard -->
<script lang="ts">
  /**
   * Activity Log
   * Display list of recent activities
   */

  import type { ActivityLogEntry } from '../../types';
  import { formatRelativeTime } from '@sddp/shell';
  import { Button } from '@sddp/ui';

  interface Props {
    activities?: ActivityLogEntry[];
    selectedActivityId?: string | null;
    onActivityClick?: (activity: ActivityLogEntry) => void;
  }
  let { activities = [], selectedActivityId = null, onActivityClick = undefined }: Props = $props();

  function handleClick(activity: ActivityLogEntry) {
    if (onActivityClick) {
      onActivityClick(activity);
    }
  }
</script>

<div class="flex flex-col gap-2">
  {#each activities as activity (activity.id)}
    {@const isSelected = selectedActivityId === activity.id}
    <Button
      variant="unstyled"
      class="w-full text-left px-3 py-3 text-sm rounded transition-all border
        {isSelected
          ? 'border-[var(--color-accent-primary)]/50 bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
          : 'border-[var(--color-border)] bg-[var(--color-bg-secondary)] hover:border-[var(--color-accent-primary)]/30'}"
      onclick={() => handleClick(activity)}
    >
      <span class="text-[var(--color-text-tertiary)] mr-2">[{formatRelativeTime(activity.timestamp, undefined, { locale: 'en' })}]</span>
      <span class="font-semibold text-[var(--color-text-primary)] mr-1">{activity.userName}</span>
      <span class="text-[var(--color-text-secondary)] mr-1">{activity.action}</span>
      <span class="text-[var(--color-accent-primary)] mr-1">{activity.entityTitle}</span>
      {#if activity.projectName}
        <span class="text-[var(--color-text-tertiary)]">in {activity.projectName}</span>
      {/if}
    </Button>
  {/each}

  {#if activities.length === 0}
    <div class="py-8 text-center text-[var(--color-text-tertiary)]">No recent activities</div>
  {/if}
</div>
