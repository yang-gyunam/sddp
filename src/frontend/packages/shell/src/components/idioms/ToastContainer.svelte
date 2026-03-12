<script lang="ts">
  import { toastStore } from '../../stores/toast.store';
  import Toast from './Toast.svelte';

  interface Props {
    position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center' | 'bottom-center';
    maxVisible?: number;
  }

  let {
    position = 'bottom-right',
    maxVisible = 5,
  }: Props = $props();

  let toasts = $state<typeof $toastStore.toasts>([]);

  const positionClasses: Record<string, string> = {
    'top-right': 'top-4 right-4',
    'top-left': 'top-4 left-4',
    'bottom-right': 'bottom-4 right-4',
    'bottom-left': 'bottom-4 left-4',
    'top-center': 'top-4 left-1/2 -translate-x-1/2',
    'bottom-center': 'bottom-4 left-1/2 -translate-x-1/2',
  };

  $effect(() => {
    const unsubscribe = toastStore.subscribe((state) => {
      toasts = state.toasts.slice(-maxVisible);
    });
    return unsubscribe;
  });

  function handleClose(id: string) {
    toastStore.removeToastData(id);
  }
</script>

<section
  class="fixed z-[9999] flex flex-col gap-2 w-[340px] {positionClasses[position]}"
  role="region"
  aria-live="polite"
  aria-label="Notifications"
>
  {#each toasts as toast (toast.id)}
    <Toast
      id={toast.id}
      type={toast.type}
      title={toast.title}
      message={toast.message}
      duration={toast.duration}
      closable={toast.closable}
      actions={toast.actions}
      onclose={handleClose}
    />
  {/each}
</section>
