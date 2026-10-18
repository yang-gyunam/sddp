<script lang="ts">
  import type { AutomaticAnalysisDisplay } from '../../utils/automatic-analysis-display';
  let { analysis }: { analysis: AutomaticAnalysisDisplay } = $props();
  const fields = [
    ['keyProposals', 'Key proposals'],
    ['agreements', 'Agreements'],
    ['disagreements', 'Disagreements'],
    ['decisions', 'Decisions'],
    ['unresolvedIssues', 'Unresolved issues'],
    ['nextSteps', 'Next steps'],
  ] as const;
  const relationships = {
    aligns: 'Aligns',
    contradicts: 'Conflicts',
    extends: 'Extends',
    unrelated: 'Unrelated',
  };
</script>

<section
  class="space-y-3 text-sm text-[var(--color-text-primary)] break-words"
  aria-label={analysis.kind === 'summary' ? 'Summary report' : 'Decision reminder'}
>
  <h3 class="text-xs font-semibold text-[var(--color-text-secondary)]">
    {analysis.kind === 'summary' ? 'Summary report' : 'Decision reminder'}
  </h3>
  {#if analysis.kind === 'summary'}
    {#if analysis.summary.trim()}<p class="whitespace-pre-wrap">{analysis.summary}</p>{/if}
    {#each fields as [field, title] (field)}
      {@const items = analysis[field].filter((item) => item.trim())}
      {#if items.length}
        <div>
          <h4 class="text-xs font-semibold text-[var(--color-text-secondary)]">{title}</h4>
          <ul class="list-disc pl-5 mt-1 space-y-1">
            {#each items as item, index (index)}<li class="whitespace-pre-wrap">{item}</li>{/each}
          </ul>
        </div>
      {/if}
    {/each}
    {#if !analysis.summary.trim() && fields.every(([field]) => !analysis[field].some( (item) => item.trim() ))}
      <p class="text-[var(--color-text-muted)]">No summary content.</p>
    {/if}
  {:else}
    <div>
      <h4 class="text-xs font-semibold text-[var(--color-text-secondary)]">Recommendation</h4>
      <p class="mt-1 whitespace-pre-wrap">
        {analysis.recommendation.trim() ? analysis.recommendation : 'No recommendation.'}
      </p>
    </div>
    <p class="text-xs text-[var(--color-text-secondary)]">
      {analysis.hasConflicts ? 'AI reported a conflict.' : 'AI reported no conflicts.'}
    </p>
    <div>
      <h4 class="text-xs font-semibold text-[var(--color-text-secondary)]">Related decisions</h4>
      {#if analysis.relatedDecisions.length}
        <ul class="mt-1 space-y-2">
          {#each analysis.relatedDecisions as decision, index (index)}
            <li class="border-l-2 border-[var(--color-border-secondary)] pl-3 space-y-1">
              <p class="text-xs font-medium">{relationships[decision.relationship]}</p>
              <p class="whitespace-pre-wrap">
                {decision.summary.trim() ? decision.summary : 'No decision summary.'}
              </p>
              {#if decision.relevance.trim()}<p
                  class="whitespace-pre-wrap text-[var(--color-text-secondary)]"
                >
                  {decision.relevance}
                </p>{/if}
              <p class="text-xs text-[var(--color-text-muted)]">Source preview unavailable.</p>
            </li>
          {/each}
        </ul>
      {:else}
        <p class="mt-1 text-[var(--color-text-muted)]">No related decisions reported.</p>
      {/if}
    </div>
  {/if}
</section>
