<script lang="ts">
  import { Button, Icon, IconButton } from '@sddp/ui';
  import { Dropdown } from '@sddp/shell';

  interface Props {
    displayName: string;
    prepare?: () => void | Promise<void>;
    prepareLabel?: string;
    remove?: () => void | Promise<void>;
  }

  let { displayName, prepare, prepareLabel = 'Prepare history', remove }: Props = $props();
</script>

<Dropdown position="bottom-right">
  {#snippet trigger({ toggle, open })}
    <IconButton
      icon="more-vertical"
      variant="ghost"
      size="sm"
      title={`Actions for ${displayName}`}
      ariaHasPopup
      ariaExpanded={open}
      onclick={toggle}
      class="{open
        ? 'opacity-100 pointer-events-auto'
        : 'opacity-0 pointer-events-none'} group-hover/participant:opacity-100 group-hover/participant:pointer-events-auto focus-visible:opacity-100 focus-visible:pointer-events-auto [@media(hover:none)]:opacity-100 [@media(hover:none)]:pointer-events-auto"
    />
  {/snippet}
  <div class="min-w-[200px]">
    {#if prepare}
      <Button
        role="menuitem"
        variant="unstyled"
        onclick={() => prepare?.()}
        class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-left text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]"
      >
        <Icon name="refresh-cw" size="xs" />
        {prepareLabel}
      </Button>
    {/if}
    {#if remove}
      <Button
        role="menuitem"
        variant="unstyled"
        onclick={() => remove?.()}
        class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-left text-[var(--color-error-500)] hover:bg-[var(--color-bg-tertiary)]"
      >
        <Icon name="x" size="xs" />
        Remove from channel
      </Button>
    {/if}
  </div>
</Dropdown>
