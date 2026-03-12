<!-- Section: TimelinePanel -->
<!--
  TimelinePanel Component
  Timeline event list content for the "Timeline" sidebar panel.
  Used inside SidebarPanels where the panel header is managed externally.
-->
<script lang="ts">
  import type { TimelineEvent } from '../../types';
  import TimelineEventItem from '../idioms/TimelineEventItem.svelte';

  interface Props {
    events: TimelineEvent[];
  }

  let { events }: Props = $props();
</script>

<div>
  {#if events.length === 0}
    <div class="px-2 py-2 text-center text-sm text-[var(--color-text-tertiary)]">
      No activity yet
    </div>
  {:else}
    {#each events as event (event.id)}
      <TimelineEventItem {event} />
    {/each}
  {/if}
</div>
