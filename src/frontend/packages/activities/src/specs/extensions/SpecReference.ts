/**
 * SpecReference Mark Extension
 * TipTap Mark for inline spec references
 */
import { Mark, mergeAttributes } from '@tiptap/core';

export interface SpecReferenceAttributes {
  specId: string | null;
  specCode: string | null;
  specTitle: string | null;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    specReference: {
      setSpecReference: (attrs: SpecReferenceAttributes) => ReturnType;
      unsetSpecReference: () => ReturnType;
    };
  }
}

export const SpecReference = Mark.create({
  name: 'specReference',
  priority: 1000,
  keepOnSplit: false,
  inclusive: false,

  addAttributes() {
    return {
      specId: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-spec-id'),
        renderHTML: (attributes) => ({
          'data-spec-id': attributes.specId,
        }),
      },
      specCode: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-spec-code'),
        renderHTML: (attributes) => ({
          'data-spec-code': attributes.specCode,
        }),
      },
      specTitle: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-spec-title'),
        renderHTML: (attributes) => ({
          'data-spec-title': attributes.specTitle,
        }),
      },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'span[data-spec-ref]',
        getAttrs: (element) => {
          if (typeof element === 'string') return false;
          const specId = element.getAttribute('data-spec-id');
          return specId ? {} : false;
        },
      },
    ];
  },

  renderHTML({ HTMLAttributes }) {
    return [
      'span',
      mergeAttributes(HTMLAttributes, {
        'data-spec-ref': 'true',
        class:
          'spec-reference text-purple-600 dark:text-purple-400 font-mono text-sm bg-purple-50 dark:bg-purple-900/20 px-1 rounded cursor-pointer hover:underline',
        title: HTMLAttributes['data-spec-title'] || 'Spec Reference',
      }),
      0,
    ];
  },

  addCommands() {
    return {
      setSpecReference:
        (attrs: SpecReferenceAttributes) =>
        ({ commands }) => {
          return commands.setMark(this.name, attrs);
        },
      unsetSpecReference:
        () =>
        ({ commands }) => {
          return commands.unsetMark(this.name);
        },
    };
  },
});

export default SpecReference;
