<!--
  ErrorBoundary Component
  Catches and displays errors in child components
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  

  interface Props {
    fallback?: import('svelte').Snippet<[{ error: Error; reset: () => void }]>;
    onError?: (error: Error, errorInfo: { componentStack?: string }) => void;
    children: import('svelte').Snippet;
  }

  let { fallback, onError, children }: Props = $props();

  let hasError = $state(false);
  let error: Error | null = $state(null);
  let errorInfo = $state<{ componentStack?: string }>({});

  // Reset error state
  function reset() {
    hasError = false;
    error = null;
    errorInfo = {};
  }

  // Handle uncaught errors
  function handleError(event: ErrorEvent) {
    const err = event.error || new Error(event.message);
    hasError = true;
    error = err;
    errorInfo = { componentStack: event.filename ? `at ${event.filename}:${event.lineno}:${event.colno}` : undefined };
    onError?.(err, errorInfo);
    event.preventDefault();
  }

  // Handle unhandled promise rejections
  function handleRejection(event: PromiseRejectionEvent) {
    const err = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
    hasError = true;
    error = err;
    errorInfo = { componentStack: 'Unhandled Promise Rejection' };
    onError?.(err, errorInfo);
    event.preventDefault();
  }

  $effect(() => {
    window.addEventListener('error', handleError);
    window.addEventListener('unhandledrejection', handleRejection);
    return () => {
      window.removeEventListener('error', handleError);
      window.removeEventListener('unhandledrejection', handleRejection);
    };
  });
</script>

{#if hasError && error}
  {#if fallback}
    {@render fallback({ error, reset })}
  {:else}
    <!-- Default Error UI -->
    <div class="flex flex-col items-center justify-center min-h-[200px] p-8 text-center bg-[var(--color-error-50)] dark:bg-[var(--color-error-900)]/10 border border-[var(--color-error-200)] dark:border-[var(--color-error-800)] rounded-lg m-4">
      <div class="w-12 h-12 rounded-full bg-[var(--color-error-100)] dark:bg-[var(--color-error-900)]/30 flex items-center justify-center mb-4">
        <Icon name="alert-triangle" size="lg" class="text-[var(--color-error-500)]" />
      </div>

      <h2 class="text-lg font-semibold text-[var(--color-error-700)] dark:text-[var(--color-error-400)] mb-2">
        Something went wrong
      </h2>

      <p class="text-sm text-[var(--color-error-600)] dark:text-[var(--color-error-300)] mb-4 max-w-md">
        {error.message || 'An unexpected error occurred'}
      </p>

      {#if errorInfo.componentStack}
        <details class="mb-4 w-full max-w-md text-left">
          <summary class="cursor-pointer text-sm text-[var(--color-error-500)] hover:text-[var(--color-error-600)]">
            Show error details
          </summary>
          <pre class="mt-2 p-3 bg-[var(--color-bg-tertiary)] rounded text-xs text-[var(--color-text-secondary)] overflow-auto max-h-32">
{error.stack || errorInfo.componentStack}
          </pre>
        </details>
      {/if}

      <div class="flex gap-3">
        <Button variant="outline" onclick={reset}>
          Try Again
        </Button>
        <Button variant="primary" onclick={() => window.location.reload()}>
          Reload Page
        </Button>
      </div>
    </div>
  {/if}
{:else}
  {@render children()}
{/if}
