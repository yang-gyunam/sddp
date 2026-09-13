<script lang="ts">
  import type { LazyComponentReference } from '@sddp/shell/types';
  import { resolveComponentAsync } from '@sddp/shell/utils';
  import { Button, Icon, Spinner } from '@sddp/ui';

  interface Props {
    reference: LazyComponentReference;
    componentProps?: Record<string, unknown>;
  }

  let { reference, componentProps = {} }: Props = $props();
  let loadedReferenceId = $state<string | null>(null);
  let isLoading = $state(true);
  let loadError = $state<Error | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- runtime Svelte component
  let resolvedComponent = $state<any>(null);

  $effect(() => {
    if (loadedReferenceId === reference.id) return;
    void loadComponent(reference);
  });

  async function loadComponent(target: LazyComponentReference) {
    loadedReferenceId = target.id;
    isLoading = true;
    loadError = null;
    resolvedComponent = null;

    const result = await resolveComponentAsync(target);
    if (loadedReferenceId !== target.id) return;

    resolvedComponent = result.component;
    loadError = result.error;
    isLoading = false;
  }

  function retry() {
    void loadComponent(reference);
  }
</script>

{#if isLoading}
  <div class="flex min-h-20 items-center justify-center" role="status" aria-live="polite">
    <Spinner size="md" />
  </div>
{:else if loadError}
  <div
    class="flex min-h-32 flex-col items-center justify-center gap-3 p-4 text-center"
    role="alert"
  >
    <Icon name="alert-triangle" size="md" class="text-[var(--color-error)]" />
    <p class="text-sm text-[var(--color-text-secondary)]">Failed to load this activity.</p>
    <Button variant="secondary" size="sm" onclick={retry}>Try Again</Button>
  </div>
{:else if resolvedComponent}
  {@const ActivityComponent = resolvedComponent}
  <ActivityComponent {...componentProps} />
{/if}
