<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import Avatar from './Avatar.svelte';

  let {
    name,
    profile,
    showName = false,
  }: {
    name: string;
    profile?: { name: string | null; avatarUrl: string | null } | null;
    showName?: boolean;
  } = $props();
  const popoverId = $props.id();
  const nameId = `${popoverId}-name`;
  const profileName = $derived(profile?.name || name);
  let open = $state(false);
</script>

{#if profile}
  <Button
    variant="unstyled"
    class="inline-flex min-w-0 max-w-full items-center gap-1.5 rounded-sm align-middle hover:bg-[var(--color-bg-hover)] focus-visible:outline-2 focus-visible:outline-[var(--color-accent-primary)]"
    title={profileName}
    aria-label={`Open profile for ${profileName}`}
    aria-haspopup="dialog"
    aria-expanded={open}
    aria-controls={popoverId}
    popovertarget={popoverId}
  >
    <Avatar name={profileName} avatarUrl={profile.avatarUrl} size="xs" />
    {#if showName}<span class="profile-name">{profileName}</span>{/if}
  </Button>
  <div
    id={popoverId}
    class="profile-popover"
    popover="auto"
    role="dialog"
    aria-labelledby={nameId}
    ontoggle={(event) => (open = event.newState === 'open')}
  >
    <div class="profile-summary">
      <Avatar name={profileName} avatarUrl={profile.avatarUrl} size="md" />
      <h2 id={nameId}>{profileName}</h2>
      <Button
        variant="unstyled"
        class="grid size-6 place-items-center rounded-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] focus-visible:outline-2 focus-visible:outline-[var(--color-accent-primary)]"
        aria-label="Close profile"
        popovertarget={popoverId}
        popovertargetaction="hide"
        autofocus
      >
        <Icon name="x" size="xs" />
      </Button>
    </div>
  </div>
{:else}
  <span class="profile-name" title={name}>{name}</span>
{/if}

<style>
  .profile-name {
    display: inline-block;
    min-width: 0;
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    vertical-align: middle;
    white-space: nowrap;
  }
  .profile-popover {
    inline-size: min(15rem, calc(100vw - 1rem));
    max-block-size: calc(100vh - 1rem);
    padding: var(--space-3);
    border: 1px solid var(--color-border-primary);
    border-radius: var(--radius-md);
    background: var(--color-bg-primary);
    color: var(--color-text-primary);
    box-shadow: var(--shadow-lg);
    overflow: auto;
  }
  @supports (position-area: block-end span-inline-end) {
    .profile-popover {
      inset: auto;
      position-area: block-end span-inline-end;
      position-try-fallbacks:
        flip-inline,
        flip-block,
        flip-block flip-inline;
      position-visibility: anchors-visible;
      align-self: start;
      justify-self: start;
      margin: var(--space-1) 0;
    }
  }
  .profile-summary {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    align-items: center;
    gap: var(--space-3);
  }
  .profile-summary h2 {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    line-height: 1.35;
    text-align: start;
    white-space: normal;
    overflow-wrap: anywhere;
  }
</style>
