<!-- Section: MemberProductionSummary — Projects > Effort -->
<script lang="ts">
  import { CardGrid } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import { StatCard } from '../../../shared/components/idioms';
  import type { MemberEffortSummary } from '../../types';

  interface Props {
    member: MemberEffortSummary;
    rangeLabel?: string;
    showHeader?: boolean;
    class?: string;
  }

  let { member, rangeLabel = '', showHeader = true, class: className = '' }: Props = $props();
</script>

<SurfaceCard class="flex flex-col gap-3 {className}">
  {#if showHeader}
    <div class="flex items-center justify-between">
      <h4 class="surface-card__title m-0 text-sm">Production</h4>
      {#if rangeLabel}
        <span class="surface-card__meta text-xs">{rangeLabel}</span>
      {/if}
    </div>
  {/if}

  <CardGrid cols={2} gap="sm">
    <StatCard size="sm" title="Requirements" value={member.requirementsCreated} />
    <StatCard size="sm" title="Specs" value={member.specsCreated} />
    <StatCard size="sm" title="Glossary" value={member.glossaryTermsCreated} />
    <StatCard size="sm" title="Artifacts" value={member.artifactsCreated} />
  </CardGrid>
</SurfaceCard>
