<!--
  LinkedEntityCard Idiom
  Clickable card for linked entities (Conversation, Requirement, Spec, etc.).
  Pattern: [Icon] Label [external-link]
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';

  interface Props {
    /** Leading icon name */
    icon: string;
    /** CSS class for the leading icon */
    iconClass?: string;
    /** Primary display text */
    label: string;
    /** Optional secondary text (e.g. entity type) */
    sublabel?: string;
    onclick?: () => void;
    class?: string;
  }

  let {
    icon,
    iconClass = 'text-[var(--color-accent-primary)]',
    label,
    sublabel,
    onclick,
    class: className = '',
  }: Props = $props();
</script>

<Button
  variant="unstyled"
  {onclick}
  class="w-full text-left px-3 py-2 rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] hover:bg-[var(--color-surface-200)] transition-colors {className}"
>
  <div class="flex items-center gap-2">
    <Icon name={icon} size="sm" class={iconClass} />
    <div class="flex-1 min-w-0">
      <span class="text-sm text-[var(--color-text-primary)] truncate block">{label}</span>
      {#if sublabel}
        <span class="text-xs text-[var(--color-text-tertiary)] capitalize">{sublabel}</span>
      {/if}
    </div>
    <Icon name="external-link" size="xs" class="text-[var(--color-text-muted)] flex-shrink-0" />
  </div>
</Button>
