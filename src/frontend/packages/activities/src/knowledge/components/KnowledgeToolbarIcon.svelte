<script lang="ts">
  import {
    AtSign,
    Bold,
    Code,
    Heading1,
    Heading2,
    Italic,
    List,
    ListOrdered,
    Quote,
    Table,
    type IconNode,
  } from 'lucide';

  type KnowledgeToolbarIconName =
    | 'at-sign'
    | 'bold'
    | 'code'
    | 'heading-1'
    | 'heading-2'
    | 'italic'
    | 'list'
    | 'list-ordered'
    | 'quote'
    | 'table';

  interface Props {
    name: KnowledgeToolbarIconName;
    size?: number;
    class?: string;
  }

  const icons: Record<KnowledgeToolbarIconName, IconNode> = {
    'at-sign': AtSign,
    bold: Bold,
    code: Code,
    'heading-1': Heading1,
    'heading-2': Heading2,
    italic: Italic,
    list: List,
    'list-ordered': ListOrdered,
    quote: Quote,
    table: Table,
  };

  let { name, size = 16, class: className = '' }: Props = $props();
</script>

<svg
  xmlns="http://www.w3.org/2000/svg"
  width={size}
  height={size}
  viewBox="0 0 24 24"
  fill="none"
  stroke="currentColor"
  stroke-width="2"
  stroke-linecap="round"
  stroke-linejoin="round"
  class={className}
  aria-hidden="true"
>
  {#each icons[name] as [tag, attributes], index (index)}
    <svelte:element this={tag} {...attributes} />
  {/each}
</svg>
