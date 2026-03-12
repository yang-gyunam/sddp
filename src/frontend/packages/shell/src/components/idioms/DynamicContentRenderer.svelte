<!--
  Dynamic Content Renderer Component
  Handles dynamic loading and rendering of tab content based on component reference
-->
<script lang="ts">
  import type { Tab } from '../../types/layout.types';
  import { Icon, Spinner, Button } from '@sddp/ui';

  interface DynamicContentRendererProps {
    tab: Tab;
    groupId?: string;
    class?: string;
  }

  let {
    tab,
    groupId = '',
    class: className = '',
  }: DynamicContentRendererProps = $props();

  // Loading and error states
  let isLoading = $state(true);
  let loadError = $state<Error | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- dynamic component requires any for Svelte rendering
  let resolvedComponent = $state<any>(null);

  // Track current tab to avoid unnecessary reloads
  let currentTabId = $state<string | null>(null);

  // Resolve component when tab changes
  $effect(() => {
    if (!tab) return;

    // Skip if same tab and already loaded
    if (currentTabId === tab.id && resolvedComponent) {
      return;
    }

    currentTabId = tab.id;
    isLoading = true;
    loadError = null;
    resolvedComponent = null;

    resolveComponent();
  });

  async function resolveComponent() {
    try {
      const component = tab.component;

      if (!component) {
        // No component specified - show placeholder
        isLoading = false;
        return;
      }

      // If component is already a Svelte component (constructor)
      if (typeof component === 'function') {
        resolvedComponent = component;
        isLoading = false;
        return;
      }

      // If component is a promise (lazy import)
      if (component instanceof Promise) {
        const module = await component;
        resolvedComponent = module.default || module;
        isLoading = false;
        return;
      }

      // If component is a module with default export
      if (typeof component === 'object' && 'default' in component) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any -- dynamic module shape
        resolvedComponent = (component as any).default;
        isLoading = false;
        return;
      }

      // Unknown component type - show placeholder
      isLoading = false;
    } catch (error) {
      loadError = error as Error;
      isLoading = false;
      console.error('[DynamicContentRenderer] Failed to load component:', error);
    }
  }

  function handleRetry() {
    loadError = null;
    isLoading = true;
    resolveComponent();
  }
</script>

<div class="h-full w-full {className}" aria-busy={isLoading ? 'true' : undefined}>
  {#if isLoading}
    <!-- Loading State -->
    <div class="h-full flex items-center justify-center" role="status" aria-live="polite">
      <Spinner size="lg" />
    </div>
  {:else if loadError}
    <!-- Error State -->
    <div class="h-full flex items-center justify-center p-8" role="alert" aria-live="polite">
      <div class="text-center space-y-4 max-w-md">
        <div
          class="w-16 h-16 bg-red-500/20 rounded-lg flex items-center justify-center mx-auto"
        >
          <Icon name="alert-triangle" size="lg" class="text-red-400" />
        </div>
        <div>
          <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-2">
            Failed to Load Content
          </h3>
          <p class="text-sm text-[var(--color-text-secondary)] mb-4">
            Error loading "{tab.title || 'this tab'}".
          </p>
          <details class="text-left">
            <summary
              class="cursor-pointer text-sm text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)]"
            >
              Show error details
            </summary>
            <div
              class="mt-2 p-3 bg-[var(--color-bg-secondary)] rounded text-xs font-mono text-red-400 overflow-auto max-h-32"
            >
              {loadError.message}
            </div>
          </details>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onclick={handleRetry}
        >
          Try Again
        </Button>
      </div>
    </div>
  {:else if resolvedComponent}
    <!-- Dynamically Rendered Component -->
    {@const DynamicComponent = resolvedComponent}
    <div class="h-full tab-content-area">
      <DynamicComponent
        title={tab.title || ''}
        tabId={tab.id}
        {groupId}
        {...tab.props}
      />
    </div>
  {:else}
    <!-- Default Placeholder (no component specified) -->
    <div class="h-full overflow-auto">
      <div class="min-h-full flex items-center justify-center p-8">
        <div class="text-center space-y-4 max-w-lg">
          <div
            class="w-16 h-16 mx-auto bg-[var(--color-bg-secondary)] rounded-xl flex items-center justify-center"
          >
            {#if tab.icon}
              <Icon name={tab.icon} size="lg" class="text-[var(--color-text-tertiary)]" />
            {:else}
              <Icon name="file" size="lg" class="text-[var(--color-text-tertiary)]" />
            {/if}
          </div>
          <div>
            <h3 class="text-xl font-semibold text-[var(--color-text-primary)] mb-3">
              {tab.title || 'Untitled Tab'}
            </h3>
            <p class="text-[var(--color-text-secondary)] mb-4 leading-relaxed">
              This tab is ready to display content.
            </p>
            {#if tab.path}
              <div
                class="inline-flex items-center gap-2 text-sm text-[var(--color-text-tertiary)] bg-[var(--color-bg-secondary)] px-3 py-2 rounded-lg"
              >
                <Icon name="link" size="sm" />
                <span>Path: {tab.path}</span>
              </div>
            {/if}
          </div>

          <!-- Development Info -->
          {#if import.meta.env.DEV}
            <div
              class="mt-6 p-4 bg-[var(--color-bg-secondary)]/50 border border-[var(--color-border)] rounded-lg text-left"
            >
              <h4
                class="text-sm font-medium text-[var(--color-text-primary)] mb-2 flex items-center"
              >
                <Icon name="info" size="sm" class="mr-2 text-blue-400" />
                Development Information
              </h4>
              <div class="text-xs text-[var(--color-text-secondary)] space-y-1">
                <div>
                  Tab ID: <code class="bg-[var(--color-bg-tertiary)] px-1 rounded"
                    >{tab.id}</code
                  >
                </div>
                <div>
                  Component: <code class="bg-[var(--color-bg-tertiary)] px-1 rounded"
                    >{tab.component ? 'Specified' : 'None'}</code
                  >
                </div>
                <div>
                  Group ID: <code class="bg-[var(--color-bg-tertiary)] px-1 rounded"
                    >{groupId || 'Default'}</code
                  >
                </div>
              </div>
            </div>
          {/if}

          {#if tab.dirty}
            <div
              class="flex items-center justify-center gap-2 text-sm text-orange-400 bg-orange-400/10 px-4 py-2 rounded-lg"
            >
              <div class="w-2 h-2 bg-orange-400 rounded-full animate-pulse"></div>
              <span>Unsaved changes</span>
            </div>
          {/if}
        </div>
      </div>
    </div>
  {/if}
</div>
