<script lang="ts">
  import { onDestroy } from 'svelte';
  import { Button, Icon, IconButton } from '@sddp/ui';
  import { toast } from '@sddp/shell';
  import { appSounds, stopReading, toggleReading } from '../../stores/app-sounds';

  let {
    getText,
    menuOwner,
    onselect,
  }: {
    getText: () => string;
    menuOwner?: string;
    onselect?: () => void;
  } = $props();

  const buttonId = $props.id();
  const id = $derived(menuOwner ?? buttonId);
  const playing = $derived($appSounds.reading === id);
  const title = $derived(playing ? 'Stop reading' : 'Read aloud');
  const icon = $derived(playing ? 'square' : 'play');

  function activate(event: MouseEvent) {
    event.stopPropagation();
    toggleReading(id, getText, (message) => toast.error(message));
    onselect?.();
  }

  onDestroy(() => {
    if (!menuOwner) stopReading(buttonId);
  });
</script>

{#if $appSounds.enabled}
  {#if menuOwner}
    <Button
      variant="unstyled"
      class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]"
      onclick={activate}
    >
      <Icon name={icon} size="xs" />
      {title}
    </Button>
  {:else}
    <IconButton {icon} {title} size="sm" onclick={activate} />
  {/if}
{/if}
