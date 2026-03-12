<!--
  RequirementCard Idiom
  Clickable card for displaying a requirement item (used in Parent/Children lists).
  Pattern: [LevelBadge] Title Code
-->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import type { RequirementLevel } from '../../types';
  import RequirementLevelBadge from './RequirementLevelBadge.svelte';

  interface Props {
    level: RequirementLevel;
    title: string;
    code: string;
    onclick?: () => void;
    class?: string;
  }

  let {
    level,
    title,
    code,
    onclick,
    class: className = '',
  }: Props = $props();
</script>

<Button
  variant="unstyled"
  {onclick}
  class="w-full text-left px-3 py-2 rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] hover:bg-[var(--color-surface-200)] transition-colors {className}"
>
  <div class="flex items-center gap-2">
    <RequirementLevelBadge {level} />
    <span class="text-sm text-[var(--color-text-primary)] truncate">{title}</span>
    <span class="text-xs font-mono text-[var(--color-text-muted)] flex-shrink-0">{code}</span>
  </div>
</Button>
