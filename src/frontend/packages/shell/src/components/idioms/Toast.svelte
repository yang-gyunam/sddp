<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { toastStore, type ToastType, type ToastAction } from '../../stores/toast.store';

  interface Props {
    id: string;
    type?: ToastType;
    title?: string;
    message: string;
    duration?: number;
    closable?: boolean;
    actions?: ToastAction[];
    onclose?: (id: string) => void;
  }

  let {
    id,
    type = 'info',
    title,
    message,
    duration = 0,
    closable = true,
    actions = [],
    onclose,
  }: Props = $props();

  let visible = $state(true);
  let timerId: ReturnType<typeof setTimeout> | null = null;
  let actionInProgress = $state<number | null>(null);
  let contentClick = $state<(() => void) | undefined>(undefined);

  const typeConfig: Record<ToastType, { icon: string; iconColor: string; barColor: string }> = {
    info: {
      icon: 'info',
      iconColor: 'text-[var(--color-info-500)]',
      barColor: 'bg-[var(--color-info-500)]',
    },
    success: {
      icon: 'check-circle',
      iconColor: 'text-[var(--color-success-500)]',
      barColor: 'bg-[var(--color-success-500)]',
    },
    warning: {
      icon: 'alert-triangle',
      iconColor: 'text-[var(--color-warning-500)]',
      barColor: 'bg-[var(--color-warning-500)]',
    },
    error: {
      icon: 'alert-circle',
      iconColor: 'text-[var(--color-error-500)]',
      barColor: 'bg-[var(--color-error-500)]',
    },
  };

  const config = $derived(typeConfig[type]);

  function close() {
    visible = false;
    if (timerId) {
      clearTimeout(timerId);
      timerId = null;
    }
    setTimeout(() => {
      onclose?.(id);
    }, 200);
  }

  async function handleAction(action: ToastAction, index: number): Promise<void> {
    if (actionInProgress !== null) {
      return;
    }

    actionInProgress = index;
    try {
      await action.onClick?.();
      if (action.dismissOnClick ?? true) {
        close();
      }
    } catch (error) {
      console.error('Toast action failed:', error);
    } finally {
      actionInProgress = null;
    }
  }

  function handleContentClick() {
    contentClick?.();
    close();
  }

  $effect(() => {
    const unsubscribe = toastStore.subscribe((state) => {
      contentClick = state.toasts.find((toast) => toast.id === id)?.onContentClick;
    });
    return unsubscribe;
  });

  $effect(() => {
    if (duration > 0) {
      timerId = setTimeout(() => close(), duration);
      return () => {
        if (timerId) clearTimeout(timerId);
      };
    }
  });
</script>

{#if visible}
  <div
    class="flex overflow-hidden rounded-lg border border-[var(--color-border)]
      shadow-lg bg-[var(--color-bg-secondary)]
      transition-all duration-200 {visible ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-1'}"
    role="alert"
    aria-live="polite"
  >
    <!-- Left accent bar -->
    <div class="w-1 flex-shrink-0 {config.barColor}"></div>

    <div class="flex items-start gap-2.5 px-3 py-2.5 flex-1 min-w-0">
      <Icon name={config.icon} size="sm" class="flex-shrink-0 mt-0.5 {config.iconColor}" />
      <div
        class="flex-1 min-w-0 {contentClick ? 'cursor-pointer' : ''}"
        onclick={() => { if (contentClick) { handleContentClick(); } }}
        onkeydown={(e) => { if (contentClick && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); handleContentClick(); } }}
        role={contentClick ? 'button' : undefined}
        tabindex={contentClick ? 0 : undefined}
      >
        {#if title}
          <p class="text-[13px] font-semibold text-[var(--color-text-primary)] leading-tight">{title}</p>
          <p class="text-xs text-[var(--color-text-secondary)] mt-1 leading-relaxed line-clamp-3">{message}</p>
        {:else}
          <p class="text-[13px] text-[var(--color-text-primary)] leading-tight line-clamp-2">{message}</p>
        {/if}
        {#if actions.length > 0}
          <div class="mt-2.5 flex items-center gap-2">
            {#each actions as action, index (`${action.label}-${index}`)}
              <Button
                variant={action.variant ?? 'secondary'}
                size="sm"
                class="h-7 px-3 text-xs"
                disabled={actionInProgress !== null}
                onclick={() => {
                  void handleAction(action, index);
                }}
              >
                {action.label}
              </Button>
            {/each}
          </div>
        {/if}
      </div>
      {#if closable}
        <Button
          variant="unstyled"
          class="flex-shrink-0 w-5 h-5 flex items-center justify-center rounded
            text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]
            hover:bg-[var(--color-bg-tertiary)] transition-colors"
          onclick={close}
          aria-label="Close"
        >
          <Icon name="x" size="xs" />
        </Button>
      {/if}
    </div>
  </div>
{/if}
