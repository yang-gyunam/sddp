<!-- Section: ArtifactContent — Artifacts > Global, Projects > Artifacts -->
<script lang="ts">
  import { Button, Icon, IconButton, Spinner } from '@sddp/ui';
  import { CollapsibleGroup, Dropdown, truncate, formatDateTime, formatFileSize } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import ReferencesSection from '../../../shared/components/idioms/ReferencesSection.svelte';
  import type { ReferenceItem } from '../../../shared/components/idioms/ReferencesSection.svelte';
  import MetadataSection from '../../../shared/components/idioms/MetadataSection.svelte';
  import type { MetadataItem } from '../../../shared/components/idioms/MetadataSection.svelte';
  import type { ArtifactDetail, ArtifactSummary } from '../../types';
  import { ARTIFACT_TYPE_STYLES, ARTIFACT_STATUS_STYLES, getFileName } from '../../types';

  interface Props {
    artifact: (ArtifactDetail | ArtifactSummary) | null;
    loading?: boolean;
    onRegenerate?: () => void;
    onViewSource?: () => void;
    onVerify?: () => void;
    onViewSpec?: () => void;
    onDeactivate?: () => void;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    class?: string;
  }

  let {
    artifact,
    loading = false,
    onRegenerate,
    onViewSource,
    onVerify,
    onViewSpec,
    onDeactivate,
    showHeader = true,
    class: className = '',
  }: Props = $props();

  const typeStyle = $derived(artifact ? ARTIFACT_TYPE_STYLES[artifact.artifactType] : null);
  const statusStyle = $derived(artifact ? (ARTIFACT_STATUS_STYLES[artifact.status] ?? ARTIFACT_STATUS_STYLES.Unverified) : null);
  const fileName = $derived(artifact ? getFileName(artifact.artifactPath) : '');

  // CollapsibleGroup expanded states
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);
  let metadataExpanded = $state(false);

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short' });
  }

  // Build reference items for ReferencesSection
  const referenceItems = $derived.by(() => {
    if (!artifact) return [];
    const items: ReferenceItem[] = [];
    // Source Conversation (ArtifactSummary provides name)
    if (artifact.sourceConversationId && 'sourceConversationName' in artifact && artifact.sourceConversationName) {
      items.push({
        icon: 'hash',
        label: 'Source Conversation',
        value: artifact.sourceConversationName,
      });
    }
    // Source Requirement (ArtifactSummary provides code)
    if (artifact.sourceRequirementId && 'sourceRequirementCode' in artifact && artifact.sourceRequirementCode) {
      items.push({
        icon: 'clipboard-check',
        label: 'Source Requirement',
        value: artifact.sourceRequirementCode,
      });
    }
    // Source Spec (both types have specCode/specTitle; ArtifactDetail also has specId)
    if (artifact.specCode || ('specId' in artifact && artifact.specId)) {
      items.push({
        icon: 'file-code',
        label: 'Source Spec',
        value: artifact.specTitle || artifact.specCode || ('specId' in artifact ? artifact.specId : ''),
        sublabel: artifact.specCode ?? undefined,
        onClick: () => onViewSpec?.(),
      });
    }
    // Source Glossary (ArtifactSummary provides name)
    if (artifact.glossaryTermId && 'glossaryTermName' in artifact && artifact.glossaryTermName) {
      items.push({
        icon: 'book-open',
        label: 'Source Glossary',
        value: artifact.glossaryTermName,
      });
    }
    return items;
  });

  // Build metadata items for MetadataSection
  const metadataItems = $derived.by(() => {
    if (!artifact) return [];
    const items: MetadataItem[] = [];
    items.push({ label: 'Status', value: statusStyle?.label ?? artifact.status, class: statusStyle?.color ?? '' });
    items.push({ label: 'Type', value: typeStyle?.label ?? artifact.artifactType, class: typeStyle?.color ?? '' });
    // Owner (ArtifactSummary)
    if ('owner' in artifact && artifact.owner?.name) {
      items.push({ label: 'Owner', value: artifact.owner.name, type: 'person', avatarUrl: artifact.owner.avatarUrl });
    }
    if (artifact.generatorVersion) {
      items.push({ label: 'Generator', value: artifact.generatorVersion });
    }
    if (artifact.templateVersion) {
      items.push({ label: 'Template', value: artifact.templateVersion });
    }
    if (artifact.contentHash) {
      items.push({ label: 'Content Hash', value: artifact.contentHash });
    }
    if ('fileSize' in artifact && artifact.fileSize) {
      items.push({ label: 'Size', value: formatFileSize(artifact.fileSize) });
    }
    if ('lineCount' in artifact && artifact.lineCount) {
      items.push({ label: 'Lines', value: String(artifact.lineCount) });
    }
    // Created By (ArtifactSummary)
    if ('createdBy' in artifact && artifact.createdBy?.name) {
      items.push({ label: 'Created By', value: artifact.createdBy.name, type: 'person', avatarUrl: artifact.createdBy.avatarUrl });
    }
    if (artifact.createdAt) {
      items.push({ label: 'Created', value: formatDateStr(artifact.createdAt) });
    }
    // Updated By (ArtifactSummary)
    if ('updatedBy' in artifact && artifact.updatedBy?.name) {
      items.push({ label: 'Updated By', value: artifact.updatedBy.name, type: 'person', avatarUrl: artifact.updatedBy.avatarUrl });
    }
    items.push({ label: 'Updated', value: formatDateStr(artifact.updatedAt) });
    return items;
  });
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3 text-[var(--color-text-tertiary)]">
        <Spinner size="lg" />
        <span class="text-sm">Loading artifact...</span>
      </div>
    </div>
  {:else if !artifact}
    <div class="flex items-center justify-center h-full">
      <div class="text-center">
        <Icon name="package" size="xl" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-50" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select an artifact</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose an artifact from the sidebar to view details.
        </p>
      </div>
    </div>
  {:else}
    {#if showHeader}
      <DetailHeader>
        {#snippet leading()}
          <span class={typeStyle?.color}>
            <Icon name={typeStyle?.icon || 'file'} size="md" />
          </span>
        {/snippet}
        <DetailTitle title={fileName}>
          <span class="flex items-center gap-1 text-xs flex-shrink-0 {statusStyle?.color}">
            <Icon name={statusStyle?.icon || 'circle'} size="xs" />
            {statusStyle?.label}
          </span>
        </DetailTitle>
        {#snippet actions()}
          {#if artifact.status === 'Modified' && onRegenerate}
            <IconButton icon="refresh-cw" variant="primary" size="sm" title="Regenerate" onclick={onRegenerate} />
          {/if}
          {#if onVerify}
            <IconButton icon="shield-check" variant="ghost" size="sm" title="Verify" onclick={onVerify} />
          {/if}
          {#if onViewSource}
            <IconButton icon="code" variant="ghost" size="sm" title="View Source" onclick={onViewSource} />
          {/if}
          {#if onDeactivate}
            <Dropdown position="bottom-right">
              {#snippet trigger()}
                <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
              {/snippet}
              <div class="py-1 min-w-[160px]">
                <Button
                  variant="unstyled"
                  onclick={onDeactivate}
                  class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-warning-600)] flex items-center gap-2"
                >
                  <Icon name="lock" size="xs" />
                  Deactivate
                </Button>
              </div>
            </Dropdown>
          {/if}
        {/snippet}
      </DetailHeader>
    {/if}

    <!-- Content -->
    <div class="flex-1 overflow-y-auto p-4">
      <div class="space-y-5">
        <!-- Basic Information -->
        <CollapsibleGroup
          title="Basic Information"
          variant="plain"
          expanded={basicExpanded}
          onToggle={() => basicExpanded = !basicExpanded}
        >
          <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
            <DetailField label="Type">
              <span class="inline-flex items-center gap-1.5 text-sm px-2 py-1 rounded {typeStyle?.bgColor} {typeStyle?.color} border {typeStyle?.borderColor}">
                <Icon name={typeStyle?.icon || 'file'} size="xs" />
                {typeStyle?.label}
              </span>
            </DetailField>

            <DetailField label="Path">
              <code class="block p-3 bg-[var(--color-bg-secondary)] rounded-lg text-sm font-mono
                text-[var(--color-text-secondary)] break-all">
                {artifact.artifactPath}
              </code>
            </DetailField>

            <DetailField label="Entity">
              <p class="text-sm text-[var(--color-text-secondary)] font-mono">
                {artifact.entityName}
              </p>
            </DetailField>
          </div>
        </CollapsibleGroup>

        <!-- References -->
        {#if referenceItems.length > 0}
          <CollapsibleGroup
            title="References"
            variant="plain"
            badge={referenceItems.length}
            expanded={referencesExpanded}
            onToggle={() => referencesExpanded = !referencesExpanded}
          >
            <div class="pl-5 pt-2 pb-3">
              <ReferencesSection items={referenceItems} title="" />
            </div>
          </CollapsibleGroup>
        {/if}

        <!-- Verification (only Modified/Missing — Unverified is a neutral state, not an alert) -->
        {#if artifact.status === 'Modified' || artifact.status === 'Missing'}
          <section class="p-4 rounded-lg {statusStyle?.bgColor} border {statusStyle?.borderColor}">
            <h3 class="text-sm font-semibold {statusStyle?.color} mb-2 flex items-center gap-2">
              <Icon name={statusStyle?.icon || 'alert-circle'} size="sm" />
              {artifact.status === 'Modified' ? 'File Modified' : 'File Missing'}
            </h3>
            <p class="text-sm text-[var(--color-text-secondary)]">
              {#if artifact.status === 'Modified'}
                This file has been manually modified. The content hash no longer matches the generated version.
              {:else}
                This file is missing from the expected location.
              {/if}
            </p>
            {#if artifact.status === 'Modified' && 'currentHash' in artifact && artifact.currentHash}
              <div class="mt-2 text-xs font-mono text-[var(--color-text-tertiary)]">
                <div>Expected: {truncate(artifact.contentHash ?? '', 16)}</div>
                <div>Current: {truncate(artifact.currentHash, 16)}</div>
              </div>
            {/if}
          </section>
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
    </div>
  {/if}
</div>
