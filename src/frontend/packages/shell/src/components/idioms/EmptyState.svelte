<!--
  EmptyState Component
  Consistent empty state display with icon, heading, subtext, and optional action
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';

  interface Props {
    /** Icon name to display */
    icon: string;
    /** Main heading text */
    heading: string;
    /** Secondary descriptive text */
    subtext?: string;
    /** Optional action button */
    action?: {
      label: string;
      onclick: () => void;
    };
    /** Icon size */
    iconSize?: 'sm' | 'md' | 'lg' | 'xl';
    /** Additional CSS classes */
    class?: string;
  }

  let {
    icon,
    heading,
    subtext,
    action,
    iconSize = 'xl',
    class: className = '',
  }: Props = $props();
</script>

<div class="flex flex-col items-center justify-center h-full p-8 text-center text-[var(--color-text-tertiary)] {className}">
  <Icon name={icon} size={iconSize} class="mb-4 opacity-50" />
  <p class="text-sm text-[var(--color-text-secondary)]">{heading}</p>
  {#if subtext}
    <p class="text-xs mt-2 text-[var(--color-text-muted)]">{subtext}</p>
  {/if}
  {#if action}
    <Button
      variant="primary"
      size="sm"
      onclick={action.onclick}
      class="mt-4"
    >
      {action.label}
    </Button>
  {/if}
</div>
