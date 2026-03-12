<!-- Section: RequirementMetaPanel — Requirements > Global -->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDateTime } from '@sddp/shell';
  import type { RequirementDetail, Requirement } from '../../types';
  import { REQUIREMENT_LEVEL_STYLES } from '../../types';

  interface RelatedSpec {
    id: string;
    code: string;
    title: string;
    status: string;
  }

  interface Props {
    requirement: RequirementDetail | null;
    relatedSpecs?: RelatedSpec[];
    siblingRequirements?: Requirement[];
    onSpecClick?: (specId: string) => void;
    onRequirementClick?: (requirementId: string) => void;
    class?: string;
  }

  let {
    requirement,
    relatedSpecs = [],
    siblingRequirements = [],
    onSpecClick,
    onRequirementClick,
    class: className = '',
  }: Props = $props();

  // Status transition actions
  const transitionActions = $derived(() => {
    if (!requirement) return [];
    const actions: { status: string; label: string; icon: string }[] = [];

    switch (requirement.status) {
      case 'Draft':
        actions.push({ status: 'InReview', label: 'Submit for Review', icon: 'eye' });
        break;
      case 'InReview':
        actions.push({ status: 'Approved', label: 'Approve', icon: 'check' });
        actions.push({ status: 'Draft', label: 'Return to Draft', icon: 'edit' });
        break;
      case 'Approved':
        actions.push({ status: 'Deprecated', label: 'Deprecate', icon: 'archive' });
        break;
    }
    return actions;
  });
</script>

<div class="requirement-meta-panel flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  {#if requirement}
    <!-- Quick Info -->
    <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
      <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
        Quick Info
      </h3>
      <div class="space-y-2 text-sm">
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Code</span>
          <span class="font-mono">{requirement.code}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Version</span>
          <span>{requirement.version}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Children</span>
          <span>{requirement.childrenCount}</span>
        </div>
      </div>
    </div>

    <!-- Status Actions -->
    {#if transitionActions().length > 0}
      <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
          Actions
        </h3>
        <div class="space-y-2">
          {#each transitionActions() as action (action.status)}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-2 text-sm rounded
                     bg-[var(--color-bg-tertiary)]
                     text-[var(--color-text-primary)]
                     hover:bg-[var(--color-bg-tertiary)]/80"
            >
              <Icon name={action.icon} size="xs" />
              <span>{action.label}</span>
            </Button>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Related Specs -->
    <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
      <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
        Related Specs
      </h3>
      {#if relatedSpecs.length > 0}
        <div class="space-y-1">
          {#each relatedSpecs as spec (spec.id)}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 p-2 text-sm rounded text-left
                     hover:bg-[var(--color-bg-tertiary)]"
              onclick={() => onSpecClick?.(spec.id)}
            >
              <Icon name="file-code" size="xs" class="text-[var(--color-text-tertiary)]" />
              <div class="flex-1 min-w-0">
                <div class="font-mono text-xs text-[var(--color-text-tertiary)]">
                  {spec.code}
                </div>
                <div class="truncate">{spec.title}</div>
              </div>
            </Button>
          {/each}
        </div>
      {:else}
        <p class="text-sm text-[var(--color-text-tertiary)] italic">
          No related specs
        </p>
      {/if}
      <Button
        variant="unstyled"
        class="mt-2 w-full flex items-center justify-center gap-1 px-2 py-1.5 text-xs rounded
               text-[var(--color-accent-primary)]
               hover:bg-[var(--color-bg-tertiary)]"
      >
        <Icon name="link" size="xs" />
        <span>Link Spec</span>
      </Button>
    </div>

    <!-- Sibling Requirements -->
    {#if siblingRequirements.length > 0}
      <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
          Sibling Requirements
        </h3>
        <div class="space-y-1 max-h-48 overflow-y-auto">
          {#each siblingRequirements as sibling (sibling.id)}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 p-2 text-sm rounded text-left
                     {sibling.id === requirement.id ? 'bg-[var(--color-accent-primary)]/10' : 'hover:bg-[var(--color-bg-tertiary)]'}"
              onclick={() => onRequirementClick?.(sibling.id)}
            >
              <span
                class="w-5 h-5 flex items-center justify-center text-xs font-semibold rounded
                       {REQUIREMENT_LEVEL_STYLES[sibling.level].bgColor}
                       {REQUIREMENT_LEVEL_STYLES[sibling.level].textColor}"
              >
                {sibling.level}
              </span>
              <div class="flex-1 min-w-0">
                <div class="font-mono text-xs text-[var(--color-text-tertiary)]">
                  {sibling.code}
                </div>
                <div class="truncate">{sibling.title}</div>
              </div>
            </Button>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Activity Log -->
    <div class="panel-section flex-1 p-3 overflow-y-auto">
      <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
        Recent Activity
      </h3>
      <div class="space-y-2 text-sm">
        <div class="flex gap-2">
          <Icon name="git-commit" size="xs" class="mt-0.5 text-[var(--color-text-tertiary)]" />
          <div>
            <div class="text-[var(--color-text-primary)]">Created</div>
            <div class="text-xs text-[var(--color-text-tertiary)]">
              {formatDateTime(requirement.createdAt)}
            </div>
          </div>
        </div>
        {#if requirement.updatedAt !== requirement.createdAt}
          <div class="flex gap-2">
            <Icon name="edit" size="xs" class="mt-0.5 text-[var(--color-text-tertiary)]" />
            <div>
              <div class="text-[var(--color-text-primary)]">Last Updated</div>
              <div class="text-xs text-[var(--color-text-tertiary)]">
                {formatDateTime(requirement.updatedAt)}
              </div>
            </div>
          </div>
        {/if}
      </div>
    </div>
  {:else}
    <!-- Empty State -->
    <div class="flex-1 flex items-center justify-center p-4">
      <div class="text-center">
        <Icon name="file-text" size="lg" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a requirement</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a requirement to view details.
        </p>
      </div>
    </div>
  {/if}
</div>
