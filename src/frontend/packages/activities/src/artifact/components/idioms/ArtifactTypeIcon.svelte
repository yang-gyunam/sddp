<script lang="ts">
  import {
    ArrowRight,
    Box,
    CheckSquare,
    Database,
    Edit,
    FileJson,
    FileText,
    GitBranch,
    Globe,
    Layout,
    PlusCircle,
    Search,
    Settings,
    Shield,
    Sliders,
    Terminal,
    Zap,
    type IconNode,
  } from 'lucide';
  import type { ArtifactType } from '../../types';

  interface Props {
    type: ArtifactType;
    size?: number;
    class?: string;
  }

  const icons: Record<ArtifactType, IconNode> = {
    Entity: Box,
    CreateDto: PlusCircle,
    UpdateDto: Edit,
    ResponseDto: ArrowRight,
    Validator: Shield,
    Repository: Database,
    DapperReadRepository: Zap,
    Command: Terminal,
    Query: Search,
    UnitTest: CheckSquare,
    Endpoints: Globe,
    Service: Settings,
    Migration: GitBranch,
    EntityJson: FileJson,
    UiComponent: Layout,
    Documentation: FileText,
    Configuration: Sliders,
  };
  const colors: Record<ArtifactType, { light: string; dark: string }> = {
    Entity: { light: 'var(--color-info-600)', dark: 'var(--color-info-400)' },
    CreateDto: { light: 'var(--color-success-600)', dark: 'var(--color-success-400)' },
    UpdateDto: { light: 'var(--color-amber-600)', dark: 'var(--color-amber-400)' },
    ResponseDto: { light: 'var(--color-purple-600)', dark: 'var(--color-purple-400)' },
    Validator: { light: 'var(--color-error-600)', dark: 'var(--color-error-400)' },
    Repository: { light: 'var(--color-indigo-600)', dark: 'var(--color-indigo-400)' },
    DapperReadRepository: { light: 'var(--color-cyan-600)', dark: 'var(--color-cyan-400)' },
    Command: { light: 'var(--color-orange-600)', dark: 'var(--color-orange-400)' },
    Query: { light: 'var(--color-teal-600)', dark: 'var(--color-teal-400)' },
    UnitTest: { light: 'var(--color-lime-600)', dark: 'var(--color-lime-400)' },
    Endpoints: { light: 'var(--color-pink-600)', dark: 'var(--color-pink-400)' },
    Service: { light: 'var(--color-violet-600)', dark: 'var(--color-violet-400)' },
    Migration: { light: 'var(--color-neutral-600)', dark: 'var(--color-neutral-400)' },
    EntityJson: { light: 'var(--color-yellow-600)', dark: 'var(--color-yellow-400)' },
    UiComponent: { light: 'var(--color-rose-600)', dark: 'var(--color-rose-400)' },
    Documentation: { light: 'var(--color-sky-600)', dark: 'var(--color-sky-400)' },
    Configuration: { light: 'var(--color-slate-600)', dark: 'var(--color-slate-400)' },
  };

  let { type, size = 20, class: className = '' }: Props = $props();
  const colorStyle = $derived(
    `--artifact-icon-light: ${colors[type].light}; --artifact-icon-dark: ${colors[type].dark};`
  );
</script>

<svg
  xmlns="http://www.w3.org/2000/svg"
  width={size}
  height={size}
  viewBox="0 0 24 24"
  fill="none"
  stroke="currentColor"
  stroke-width="1.5"
  stroke-linecap="round"
  stroke-linejoin="round"
  class="artifact-type-icon {className}"
  style={colorStyle}
  aria-hidden="true"
>
  {#each icons[type] as [tag, attributes], index (index)}
    <svelte:element this={tag} {...attributes} />
  {/each}
</svg>

<style>
  .artifact-type-icon {
    color: var(--artifact-icon-light);
  }

  :global(.dark) .artifact-type-icon {
    color: var(--artifact-icon-dark);
  }
</style>
