<!-- Section: ConversationCreatePanel — Conversations > Global, Projects > Conversations -->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Icon, Input, Textarea, IconButton } from '@sddp/ui';

  interface Props {
    /** Header icon (hash, lock, list, message-square, etc.) */
    icon: string;
    /** Header title (New Channel, New Forum, etc.) */
    title: string;
    /** Name field value (bindable) */
    name: string;
    /** Description field value (bindable) */
    description: string;
    /** Name field label (default: 'Name') */
    nameLabel?: string;
    /** Name placeholder */
    namePlaceholder?: string;
    /** Description placeholder */
    descriptionPlaceholder?: string;
    /** Description label (default: 'Description') */
    descriptionLabel?: string;
    /** Whether to show the Name field (default: true) */
    showName?: boolean;
    /** Whether to show the Description field (default: true) */
    showDescription?: boolean;
    /** Submitting state */
    loading?: boolean;
    /** Error message */
    error?: string | null;
    /** Form validity (controls check button variant) */
    isValid?: boolean;
    /** Submit callback */
    onSubmit?: () => void;
    /** Cancel callback */
    onCancel?: () => void;
    /** Additional form fields (participant selection, etc.) */
    children?: Snippet;
    class?: string;
  }

  let {
    icon,
    title,
    name = $bindable(''),
    description = $bindable(''),
    nameLabel = 'Name',
    namePlaceholder = '',
    descriptionPlaceholder = '',
    descriptionLabel = 'Description',
    showName = true,
    showDescription = true,
    loading = false,
    error = null,
    isValid = false,
    onSubmit,
    onCancel,
    children,
    class: className = '',
  }: Props = $props();
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  <!-- Header: icon + title + check/x -->
  <div class="flex items-center justify-between min-h-12 px-4 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center">
      <Icon
        name={icon}
        size="md"
        class="text-[var(--color-text-tertiary)] mr-2"
      />
      <h2 class="text-sm font-semibold text-[var(--color-text-primary)]">
        {title}
      </h2>
    </div>
    <div class="flex items-center gap-1">
      <IconButton
        icon="check"
        variant={isValid ? 'success' : 'ghost'}
        onclick={onSubmit}
        disabled={loading || !isValid}
        title="Create"
      />
      <IconButton
        icon="x"
        variant="ghost"
        onclick={onCancel}
        disabled={loading}
        title="Cancel"
      />
    </div>
  </div>

  <!-- Body -->
  <div class="flex-1 overflow-auto p-6">
    <div class="max-w-xl mx-auto space-y-4">
      {#if showName}
        <Input
          label={nameLabel}
          required
          bind:value={name}
          placeholder={namePlaceholder}
        />
      {/if}

      {#if showDescription}
        <Textarea
          label={descriptionLabel}
          bind:value={description}
          placeholder={descriptionPlaceholder}
          rows={3}
        />
      {/if}

      {#if children}
        {@render children()}
      {/if}

      {#if error}
        <p class="text-sm text-red-500">{error}</p>
      {/if}
    </div>
  </div>
</div>
