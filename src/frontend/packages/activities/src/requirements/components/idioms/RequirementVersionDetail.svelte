<!--
  RequirementVersionDetail Component
  Read-only display of a past requirement version (used in version history tabs).
  Uses RequirementContentBody for shared People + Metadata rendering.
-->
<script lang="ts">
  import RequirementLevelBadge from './RequirementLevelBadge.svelte';
  import RequirementContentBody from './RequirementContentBody.svelte';
  import type { RequirementVersion } from '../../types';

  interface Props {
    version: RequirementVersion;
    class?: string;
  }

  let { version, class: className = '' }: Props = $props();
</script>

<div class="space-y-4 {className}">
  <!-- Version header -->
  <div class="flex items-center gap-3">
    <RequirementLevelBadge level={version.level} size="md" />
    <div class="min-w-0">
      <h3 class="text-sm font-semibold text-[var(--color-text-primary)] truncate">
        {version.title}
      </h3>
      <div class="flex items-center gap-2 mt-0.5">
        <span class="text-xs text-[var(--color-text-tertiary)] font-mono">{version.code}</span>
        <span class="text-xs text-[var(--color-text-muted)]">·</span>
        <span class="text-xs font-mono text-[var(--color-accent-primary)]">v{version.version}</span>
      </div>
    </div>
  </div>

  <!-- Shared content: Description + People + Metadata -->
  <RequirementContentBody
    description={version.description}
    status={version.status}
    priority={version.priority}
    version={version.version}
    ownerName={version.owner?.name}
    createdByName={version.createdBy?.name}
    updatedByName={version.updatedBy?.name}
    createdAt={version.createdAt}
    updatedAt={version.updatedAt}
    validFrom={version.validFrom}
    validTo={version.validTo}
  />
</div>
