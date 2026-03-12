/**
 * GlossaryLink Mark Extension
 * TipTap Mark for inline glossary term links
 */
import { Mark, mergeAttributes } from '@tiptap/core';

export interface GlossaryLinkAttributes {
  termId: string | null;
  term: string | null;
  category: string | null;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    glossaryLink: {
      setGlossaryLink: (attrs: GlossaryLinkAttributes) => ReturnType;
      unsetGlossaryLink: () => ReturnType;
    };
  }
}

export const GlossaryLink = Mark.create({
  name: 'glossaryLink',
  priority: 1000,
  keepOnSplit: false,
  inclusive: false,

  addAttributes() {
    return {
      termId: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-term-id'),
        renderHTML: (attributes) => ({
          'data-term-id': attributes.termId,
        }),
      },
      term: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-term'),
        renderHTML: (attributes) => ({
          'data-term': attributes.term,
        }),
      },
      category: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-category'),
        renderHTML: (attributes) => ({
          'data-category': attributes.category,
        }),
      },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'span[data-glossary-term]',
        getAttrs: (element) => {
          if (typeof element === 'string') return false;
          const termId = element.getAttribute('data-term-id');
          return termId ? {} : false;
        },
      },
    ];
  },

  renderHTML({ HTMLAttributes }) {
    return [
      'span',
      mergeAttributes(HTMLAttributes, {
        'data-glossary-term': 'true',
        class:
          'glossary-link text-[var(--color-accent-primary)] underline decoration-dotted decoration-1 underline-offset-2 cursor-help hover:decoration-solid',
        title: HTMLAttributes['data-term'] || 'Glossary term',
      }),
      0,
    ];
  },

  addCommands() {
    return {
      setGlossaryLink:
        (attrs: GlossaryLinkAttributes) =>
        ({ commands }) => {
          return commands.setMark(this.name, attrs);
        },
      unsetGlossaryLink:
        () =>
        ({ commands }) => {
          return commands.unsetMark(this.name);
        },
    };
  },
});

export default GlossaryLink;
