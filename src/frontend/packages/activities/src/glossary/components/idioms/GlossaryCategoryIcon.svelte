<script lang="ts">
  import { Briefcase, Code, Hash, Layers, Layout, Server, Shield, type IconNode } from 'lucide';
  import type { TermCategory } from '../../types';

  interface Props {
    category: TermCategory;
    size?: number;
    class?: string;
  }

  const icons: Record<TermCategory, IconNode> = {
    Technical: Code,
    Business: Briefcase,
    Abbreviation: Hash,
    Domain: Layers,
    Architecture: Layout,
    Infrastructure: Server,
    Security: Shield,
  };
  const colors: Record<TermCategory, { light: string; dark: string }> = {
    Technical: { light: 'var(--color-info-600)', dark: 'var(--color-info-400)' },
    Business: { light: 'var(--color-success-600)', dark: 'var(--color-success-400)' },
    Abbreviation: { light: 'var(--color-purple-600)', dark: 'var(--color-purple-400)' },
    Domain: { light: 'var(--color-amber-600)', dark: 'var(--color-amber-400)' },
    Architecture: { light: 'var(--color-indigo-600)', dark: 'var(--color-indigo-400)' },
    Infrastructure: { light: 'var(--color-slate-600)', dark: 'var(--color-slate-400)' },
    Security: { light: 'var(--color-error-600)', dark: 'var(--color-error-400)' },
  };

  let { category, size = 20, class: className = '' }: Props = $props();
  const colorStyle = $derived(
    `--category-icon-light: ${colors[category].light}; --category-icon-dark: ${colors[category].dark};`
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
  class="glossary-category-icon {className}"
  style={colorStyle}
  aria-hidden="true"
>
  {#each icons[category] as [tag, attributes], index (index)}
    <svelte:element this={tag} {...attributes} />
  {/each}
</svg>

<style>
  .glossary-category-icon {
    color: var(--category-icon-light);
  }

  :global(.dark) .glossary-category-icon {
    color: var(--category-icon-dark);
  }
</style>
