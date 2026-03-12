<!--
  SpecItem Component
  Single spec item in the sidebar list
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { formatPercent, ListItem } from '@sddp/shell';
  import type { SpecSummary } from '../../types';
  import { SPEC_STATUS_STYLES } from '../../types';

  interface Props {
    spec: SpecSummary;
    selected?: boolean;
    onSelect?: (id: string) => void;
    class?: string;
  }

  let {
    spec,
    selected = false,
    onSelect,
    class: className = '',
  }: Props = $props();

  const statusStyle = $derived(SPEC_STATUS_STYLES[spec.status]);
  const signOffLabel = $derived(
    spec.signOffProgress !== undefined
      ? formatPercent(spec.signOffProgress / 100, { maximumFractionDigits: 0 })
      : ''
  );

  function handleClick() {
    onSelect?.(spec.id);
  }
</script>

<ListItem {selected} onclick={handleClick} role="treeitem"
  aria-selected={selected} class="group {className}">
  <!-- Status Icon -->
  <Icon
    name={statusStyle.icon}
    size="xs"
    class="flex-shrink-0 {statusStyle.textColor}"
    title={statusStyle.label}
  />

  <!-- Spec Info -->
  <div class="flex-1 min-w-0">
    <div class="flex items-center gap-2">
      <span class="text-xs font-mono text-[var(--vscode-descriptionForeground)]">
        {spec.code}
      </span>
      {#if spec.version}
        <span class="text-xs text-[var(--vscode-descriptionForeground)] opacity-70">
          v{spec.version}
        </span>
      {/if}
    </div>
    <div class="truncate text-sm">
      {spec.title}
    </div>
  </div>

  <!-- Sign-off Progress (for InReview) -->
  {#if spec.status === 'InReview' && spec.signOffProgress !== undefined}
    <div
      class="flex-shrink-0 w-8 h-8 relative"
      title="Sign-off progress: {signOffLabel}"
    >
      <svg class="w-8 h-8 -rotate-90" viewBox="0 0 32 32">
        <circle
          cx="16"
          cy="16"
          r="12"
          fill="none"
          stroke="var(--vscode-progressBar-background)"
          stroke-width="3"
          opacity="0.3"
        />
        <circle
          cx="16"
          cy="16"
          r="12"
          fill="none"
          stroke="var(--vscode-progressBar-background)"
          stroke-width="3"
          stroke-dasharray="{spec.signOffProgress * 0.754} 75.4"
          stroke-linecap="round"
        />
      </svg>
      <span class="absolute inset-0 flex items-center justify-center text-[0.625rem] font-medium">
        {signOffLabel}
      </span>
    </div>
  {/if}

  <!-- Linked Requirement -->
  {#if spec.linkedRequirementCode}
    <span
      class="flex-shrink-0 text-xs text-[var(--vscode-textLink-foreground)]
             bg-[var(--vscode-badge-background)] px-1.5 rounded"
      title="Linked to {spec.linkedRequirementCode}"
    >
      {spec.linkedRequirementCode}
    </span>
  {/if}
</ListItem>
