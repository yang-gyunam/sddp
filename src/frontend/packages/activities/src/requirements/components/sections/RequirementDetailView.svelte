<!-- Section: RequirementDetailView — Projects > Requirements -->
<script lang="ts">
  import { Icon, IconButton, Button, Spinner } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import ReferencesSection from '../../../shared/components/idioms/ReferencesSection.svelte';
  import type { ReferenceItem } from '../../../shared/components/idioms/ReferencesSection.svelte';
  import MetadataSection from '../../../shared/components/idioms/MetadataSection.svelte';
  import type { MetadataItem } from '../../../shared/components/idioms/MetadataSection.svelte';
  import { untrack } from 'svelte';
  import { CollapsibleGroup, Dropdown, formatDateTime } from '@sddp/shell';
  import RequirementLevelBadge from '../idioms/RequirementLevelBadge.svelte';
  import RequirementCard from '../idioms/RequirementCard.svelte';
  import type { FieldAuthor } from '../../../shared/types';
  import { PriorityBadge } from '@sddp/shell';
  import type {
    RequirementDetail as RequirementDetailType,
    Requirement,
    RequirementStatus,
  } from '../../types';
  import { isEditable, VALID_STATUS_TRANSITIONS, REQUIREMENT_STATUS_STYLES } from '../../types';
  import { getFieldAuthors } from '../../services';

  interface Props {
    requirement: RequirementDetailType;
    loading?: boolean;
    onEdit?: () => void;
    onStatusChange?: (newStatus: RequirementStatus) => void;
    onChildSelect?: (child: Requirement) => void;
    onParentSelect?: (parentId: string) => void;
    onConversationClick?: (conversationId: string) => void;
    onPromoteToSpec?: () => void;
    onClose?: () => void;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    /** When true, hides all action buttons (edit, status change, etc.) */
    readonly?: boolean;
    class?: string;
  }

  let {
    requirement,
    loading = false,
    onEdit,
    onStatusChange,
    onChildSelect,
    onParentSelect,
    onConversationClick,
    onPromoteToSpec,
    onClose,
    showHeader = true,
    readonly: isReadOnly = false,
    class: className = '',
  }: Props = $props();

  // Field authors (who last modified each field)
  let fieldAuthors = $state<FieldAuthor[]>([]);

  async function loadFieldAuthors(): Promise<void> {
    try {
      fieldAuthors = await getFieldAuthors(requirement.tenantId, requirement.projectId, requirement.id);
    } catch {
      fieldAuthors = [];
    }
  }

  function getFieldAuthor(fieldName: string): FieldAuthor | null {
    return fieldAuthors.find((a) => a.fieldName.toLowerCase() === fieldName.toLowerCase()) ?? null;
  }

  // Load field authors on mount / requirement change
  let prevRequirementId = $state<string | null>(null);
  $effect(() => {
    if (!requirement.id) return;
    if (requirement.id === prevRequirementId) return;
    prevRequirementId = requirement.id;
    untrack(() => loadFieldAuthors().catch((err) => console.warn('[RequirementDetail] loadFieldAuthors failed:', err)));
  });

  // CollapsibleGroup expanded states (always expanded)
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);
  let metadataExpanded = $state(true);

  const canEdit = $derived(isEditable(requirement.status));
  const canPromote = $derived(
    requirement.status === 'Approved' && requirement.linkedSpecs.length === 0
  );
  const availableTransitions = $derived(VALID_STATUS_TRANSITIONS[requirement.status]);

  // Build reference items for ReferencesSection
  const referenceItems = $derived.by(() => {
    const items: ReferenceItem[] = [];
    // Source Conversation
    if (requirement.conversationId) {
      const icon = requirement.conversationType === 'Channel' ? 'hash'
        : requirement.conversationType === 'Forum' ? 'clipboard-list'
        : 'message-circle';
      const prefix = requirement.conversationType === 'Channel' ? '#' : '';
      const name = requirement.conversationName
        ? `${prefix}${requirement.conversationName}`
        : (requirement.conversationId ?? '');
      const value = requirement.conversationDescription
        ? `${name} — ${requirement.conversationDescription}`
        : name;
      items.push({
        icon,
        label: 'Source Conversation',
        value,
        sublabel: requirement.conversationType ?? undefined,
        onClick: () => onConversationClick?.(requirement.conversationId!),
        fieldAuthor: getFieldAuthor('ConversationId'),
      });
    }
    // Parent Requirement
    if (requirement.parentId && requirement.parentLevel) {
      items.push({
        icon: 'git-branch',
        label: 'Parent Requirement',
        value: requirement.parentTitle || requirement.parentCode || requirement.parentId,
        sublabel: requirement.parentCode || undefined,
        onClick: () => onParentSelect?.(requirement.parentId!),
      });
    }
    return items;
  });

  // Build metadata items for MetadataSection
  const metadataItems = $derived.by(() => {
    const items: MetadataItem[] = [];
    const statusStyle = REQUIREMENT_STATUS_STYLES[requirement.status];
    // Row 1: Status | Owner
    items.push({ label: 'Status', value: statusStyle.label, class: statusStyle.textColor });
    items.push({ label: 'Owner', value: requirement.owner?.name || 'Unassigned', type: requirement.owner?.name ? 'person' : 'text', avatarUrl: requirement.owner?.avatarUrl });
    // Row 2: Version | Valid From
    items.push({ label: 'Version', value: requirement.version });
    if (requirement.validFrom) {
      items.push({ label: 'Valid From', value: formatDateTime(requirement.validFrom, { month: 'short' }) });
    }
    // Row 3: Created By | Created
    if (requirement.createdBy?.name) {
      items.push({ label: 'Created By', value: requirement.createdBy.name, type: 'person', avatarUrl: requirement.createdBy.avatarUrl });
    }
    if (requirement.createdAt) {
      items.push({ label: 'Created', value: formatDateTime(requirement.createdAt, { month: 'short' }) });
    }
    // Row 4: Updated By | Updated
    if (requirement.updatedBy?.name) {
      items.push({ label: 'Updated By', value: requirement.updatedBy.name, type: 'person', avatarUrl: requirement.updatedBy.avatarUrl });
    }
    if (requirement.updatedAt) {
      items.push({ label: 'Updated', value: formatDateTime(requirement.updatedAt, { month: 'short' }) });
    }
    // Valid To (if exists)
    if (requirement.validTo) {
      items.push({ label: 'Valid To', value: formatDateTime(requirement.validTo, { month: 'short' }) });
    }
    return items;
  });

</script>

<div class="flex flex-col h-full {className}">
  {#if showHeader}
    <!-- Header -->
    <DetailHeader>
      {#snippet leading()}
        <RequirementLevelBadge level={requirement.level} size="md" />
      {/snippet}
      <DetailTitle title={requirement.title} code={requirement.code} />
      {#snippet actions()}
        {#if !isReadOnly && canEdit && onEdit}
          <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={onEdit} />
        {/if}
        {#if onClose}
          <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={onClose} />
        {/if}
        <!-- More actions (rightmost) -->
        {#if !isReadOnly && (availableTransitions.length > 0 || (canPromote && onPromoteToSpec))}
          <Dropdown position="bottom-right">
            {#snippet trigger()}
              <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
            {/snippet}
            <div class="py-1 min-w-[160px]">
              {#if availableTransitions.length > 0}
                <div class="px-3 py-1 text-[0.625rem] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
                  Transition to
                </div>
                {#each availableTransitions as status (status)}
                  <Button
                    variant="unstyled"
                    onclick={() => onStatusChange?.(status)}
                    class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors {REQUIREMENT_STATUS_STYLES[status].textColor}"
                  >
                    {REQUIREMENT_STATUS_STYLES[status].label}
                  </Button>
                {/each}
              {/if}
              {#if canPromote && onPromoteToSpec}
                {#if availableTransitions.length > 0}
                  <div class="my-1 border-t border-[var(--color-border-secondary)]"></div>
                {/if}
                <Button
                  variant="unstyled"
                  onclick={onPromoteToSpec}
                  class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-text-primary)] flex items-center gap-2"
                >
                  <Icon name="file-signature" size="xs" />
                  Promote to Spec
                </Button>
              {/if}
            </div>
          </Dropdown>
        {/if}
      {/snippet}
    </DetailHeader>
  {/if}

  <!-- Content -->
  <div class="flex-1 overflow-y-auto p-4">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <Spinner size="lg" />
      </div>
    {:else}
      <!-- Content zone -->
      <div class="space-y-5">
        <!-- Basic Information -->
        <CollapsibleGroup
          title="Basic Information"
          variant="plain"
          expanded={basicExpanded}
          onToggle={() => basicExpanded = !basicExpanded}
        >
          <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
            <DetailField label="Description" author={getFieldAuthor('Description')}>
              <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
                {requirement.description || 'No description provided.'}
              </div>
            </DetailField>
            <DetailField label="Priority" author={getFieldAuthor('Priority')}>
              <PriorityBadge priority={requirement.priority} />
            </DetailField>
          </div>
        </CollapsibleGroup>

        <!-- References (Source Conversation, Parent Requirement, Child Requirements) -->
        {#if referenceItems.length > 0 || (requirement.children && requirement.children.length > 0)}
          <CollapsibleGroup
            title="References"
            variant="plain"
            badge={referenceItems.length + (requirement.children?.length ?? 0)}
            expanded={referencesExpanded}
            onToggle={() => referencesExpanded = !referencesExpanded}
          >
            <div class="pl-5 pt-2 pb-3 space-y-4">
              {#if referenceItems.length > 0}
                <ReferencesSection items={referenceItems} title="" />
              {/if}

              {#if requirement.children && requirement.children.length > 0}
                <DetailField label="Child Requirements ({requirement.children.length})">
                  <ul class="space-y-2">
                    {#each requirement.children as child (child.id)}
                      <li>
                        <RequirementCard
                          level={child.level}
                          title={child.title}
                          code={child.code}
                          onclick={() => onChildSelect?.(child)}
                        />
                      </li>
                    {/each}
                  </ul>
                </DetailField>
              {/if}
            </div>
          </CollapsibleGroup>
        {/if}

        <!-- Metadata -->
        <CollapsibleGroup
          title="Metadata"
          variant="plain"
          expanded={metadataExpanded}
          onToggle={() => metadataExpanded = !metadataExpanded}
        >
          <div class="pl-5 pt-2 pb-3">
            <MetadataSection items={metadataItems} />
          </div>
        </CollapsibleGroup>
      </div>
    {/if}
  </div>
</div>
