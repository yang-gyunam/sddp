<script lang="ts">
  import { Icon } from '@sddp/ui';

  let {
    icon = 'lock',
    heading = 'Access Denied',
    message = '',
    class: className = '',
  }: {
    icon?: string;
    heading?: string;
    message?: string;
    class?: string;
  } = $props();
</script>

<div class="h-full flex items-center justify-center p-8 {className}" role="alert" aria-live="polite">
  <div class="text-center space-y-4 max-w-md">
    <div
      class="w-16 h-16 bg-amber-500/20 rounded-lg flex items-center justify-center mx-auto"
    >
      <Icon name={icon} size="lg" class="text-amber-400" />
    </div>
    <div>
      <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-2">
        {heading}
      </h3>
      {#if message}
        <p class="text-sm text-[var(--color-text-secondary)]">
          {message}
        </p>
      {/if}
    </div>
  </div>
</div>
