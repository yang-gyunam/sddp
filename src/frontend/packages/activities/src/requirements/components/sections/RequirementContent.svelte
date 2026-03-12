<!-- Section: RequirementContent — Requirements > Global -->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDate } from '@sddp/shell';
  import type { RequirementDetail } from '../../types';
  import { REQUIREMENT_LEVEL_STYLES, REQUIREMENT_STATUS_STYLES } from '../../types';
  import {
    RequirementLevelBadge,
    RequirementStatusBadge,
    RequirementCard,
  } from '../..';
  import LinkedEntityCard from '../../../shared/components/idioms/LinkedEntityCard.svelte';

  interface Props {
    requirement: RequirementDetail | null;
    loading?: boolean;
    onEdit?: () => void;
    onTransition?: (status: string) => void;
    class?: string;
  }

  let {
    requirement,
    loading = false,
    onEdit,
    onTransition: _onTransition,
    class: className = '',
  }: Props = $props();

  const isEditable = $derived(requirement?.status === 'Draft');

  // Conversation display info
  const conversationIcon = $derived(
    requirement?.conversationType === 'Channel' ? 'hash'
    : requirement?.conversationType === 'Forum' ? 'clipboard-list'
    : 'message-circle'
  );
  const conversationLabel = $derived.by(() => {
    if (!requirement?.conversationName) return requirement?.conversationId?.slice(0, 8) ?? '';
    const prefix = requirement.conversationType === 'Channel' ? '#' : '';
    const name = `${prefix}${requirement.conversationName}`;
    return requirement.conversationDescription
      ? `${name} — ${requirement.conversationDescription}`
      : name;
  });
</script>

<div class="requirement-content flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if loading}
    <!-- Loading State -->
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Icon name="loading" size="xl" class="animate-spin text-[var(--color-text-tertiary)]" />
        <p class="mt-2 text-sm text-[var(--color-text-tertiary)]">Loading requirement...</p>
      </div>
    </div>
  {:else if requirement}
    <!-- Header -->
    <div class="content-header flex-shrink-0 p-4 border-b border-[var(--color-border-primary)]">
      <div class="flex items-start justify-between gap-4">
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 mb-2">
            <span class="text-sm font-mono text-[var(--color-text-tertiary)]">
              {requirement.code}
            </span>
            <span class="text-xs text-[var(--color-text-tertiary)]">
              v{requirement.version}
            </span>
          </div>
          <h1 class="text-xl font-semibold text-[var(--color-text-primary)]">
            {requirement.title}
          </h1>
        </div>

        <div class="flex items-center gap-2">
          <RequirementLevelBadge level={requirement.level} />
          <RequirementStatusBadge status={requirement.status} />
        </div>
      </div>

      <!-- Actions -->
      <div class="flex items-center gap-2 mt-4">
        {#if isEditable}
          <Button
            variant="primary"
            size="sm"
            onclick={() => onEdit?.()}
          >
            <Icon name="edit" size="xs" />
            <span>Edit</span>
          </Button>
        {/if}

        <Button
          variant="secondary"
          size="sm"
        >
          <Icon name="git-branch" size="xs" />
          <span>View History</span>
        </Button>
      </div>
    </div>

    <!-- Content Body -->
    <div class="content-body flex-1 overflow-y-auto p-4">
      <!-- Description -->
      <section class="mb-6">
        <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
          <Icon name="note" size="xs" />
          Description
        </h2>
        <div class="prose prose-sm dark:prose-invert max-w-none
                    text-[var(--color-text-primary)]
                    bg-[var(--color-bg-secondary)]
                    border border-[var(--color-border-primary)]
                    rounded p-3">
          {requirement.description || 'No description provided.'}
        </div>
      </section>

      <!-- Metadata -->
      <section class="mb-6">
        <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
          <Icon name="info" size="xs" />
          Metadata
        </h2>
        <div class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span class="text-[var(--color-text-tertiary)]">Level:</span>
            <span class="ml-2 font-medium">{REQUIREMENT_LEVEL_STYLES[requirement.level].label}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Status:</span>
            <span class="ml-2 font-medium">{REQUIREMENT_STATUS_STYLES[requirement.status].label}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Created:</span>
            <span class="ml-2">{formatDate(requirement.createdAt)}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Updated:</span>
            <span class="ml-2">{formatDate(requirement.updatedAt)}</span>
          </div>
          {#if requirement.parentCode}
            <div class="col-span-2">
              <span class="text-[var(--color-text-tertiary)]">Parent:</span>
              <span class="ml-2 font-mono text-[var(--color-accent-primary)]">
                {requirement.parentCode}
              </span>
            </div>
          {/if}
        </div>
      </section>

      <!-- Children Requirements -->
      {#if requirement.children && requirement.children.length > 0}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="list-tree" size="xs" />
            Child Requirements ({requirement.children.length})
          </h2>
          <div class="space-y-2">
            {#each requirement.children as child (child.id)}
              <RequirementCard
                level={child.level}
                title={child.title}
                code={child.code}
              />
            {/each}
          </div>
        </section>
      {/if}

      <!-- Related Conversation -->
      {#if requirement.conversationId}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="comment-discussion" size="xs" />
            Source Conversation
          </h2>
          <LinkedEntityCard
            icon={conversationIcon}
            label={conversationLabel}
          />
        </section>
      {/if}
    </div>
  {:else}
    <!-- Empty State -->
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Icon name="file-text" size="xl" class="mx-auto mb-3 text-[var(--color-text-tertiary)] opacity-50" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a requirement</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a requirement from the sidebar to view details.
        </p>
      </div>
    </div>
  {/if}
</div>
