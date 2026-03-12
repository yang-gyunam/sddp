<!--
  ChangeToggle Component
  Toggle button to show/hide change highlights in spec content
-->
<script lang="ts">
  import { Button, Icon } from '@sddp/ui';

  interface Props {
    enabled: boolean;
    onToggle: (enabled: boolean) => void;
    changesCount?: number;
    class?: string;
  }

  let { enabled, onToggle, changesCount = 0, class: className = '' }: Props = $props();

  function handleClick() {
    onToggle(!enabled);
  }
</script>

<Button
  variant={enabled ? 'primary' : 'ghost'}
  size="sm"
  onclick={handleClick}
  class={className}
>
  <Icon name="git-compare" size="sm" />
  <span class="ml-1">{enabled ? 'Hide' : 'Show'} Changes</span>
  {#if changesCount > 0}
    <span
      class="ml-1.5 px-1.5 py-0.5 text-xs font-medium rounded-full {enabled
        ? 'bg-white/20 text-white'
        : 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'}"
    >
      {changesCount}
    </span>
  {/if}
</Button>
