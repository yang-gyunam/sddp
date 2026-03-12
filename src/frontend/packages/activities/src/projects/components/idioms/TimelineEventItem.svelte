<!--
  TimelineEvent Component
  Single event item in the timeline (compact single-line format)
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDateByPreference } from '@sddp/shell';
  import type { TimelineEvent as TimelineEventType } from '../../types';
  import { TIMELINE_EVENT_TYPE_STYLES } from '../../types';

  interface Props {
    event: TimelineEventType;
    onClick?: (event: TimelineEventType) => void;
    class?: string;
  }

  let { event, onClick, class: className = '' }: Props = $props();

  const eventStyle = $derived(TIMELINE_EVENT_TYPE_STYLES[event.type]);

  // Format: "John created Spec SPEC-AUTH-001"
  const entityLabel = $derived(event.entityName || event.entityTitle || event.entityType);
  const eventText = $derived(
    `${event.actorName} ${eventStyle.label.toLowerCase()} ${entityLabel}`
  );

  // Smart time format:, 10, 1, 14:21
  const formattedTime = $derived(formatDateByPreference(event.timestamp));

  function handleClick() {
    onClick?.(event);
  }


</script>

<Button
  variant="unstyled"
  tabindex={onClick ? undefined : -1}
  aria-label={eventText}
  class="
    w-full text-left flex items-center gap-1.5 px-2 py-1.5 rounded overflow-hidden
    hover:bg-[var(--color-bg-tertiary)] transition-colors
    {onClick ? 'cursor-pointer' : ''}
    {className}
  "
  onclick={handleClick}
>
  <!-- Event icon -->
  <Icon name={eventStyle.icon} size="xs" class="{eventStyle.color} flex-shrink-0" />

  <!-- Event text (truncated, full text in tooltip) -->
  <span class="flex-1 min-w-0 text-xs text-[var(--color-text-secondary)] truncate" title={eventText}>
    {eventText}
  </span>

  <!-- Time (right-aligned) -->
  <span class="text-[0.625rem] text-[var(--color-text-tertiary)] flex-shrink-0 tabular-nums">
    {formattedTime}
  </span>
</Button>
