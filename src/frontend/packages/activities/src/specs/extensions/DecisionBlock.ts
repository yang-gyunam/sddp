/**
 * DecisionBlock Extension
 * TipTap Node for structured decision documentation
 */
import { Node, mergeAttributes } from '@tiptap/core';

export interface DecisionBlockAttributes {
  context: string;
  decision: string;
  rationale: string;
  alternatives: string[];
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    decisionBlock: {
      insertDecisionBlock: (attrs?: Partial<DecisionBlockAttributes>) => ReturnType;
    };
  }
}

export const DecisionBlock = Node.create({
  name: 'decisionBlock',
  group: 'block',
  atom: true,
  draggable: true,

  addAttributes() {
    return {
      context: {
        default: '',
        parseHTML: (element) => element.getAttribute('data-context') || '',
        renderHTML: (attributes) => ({
          'data-context': attributes.context,
        }),
      },
      decision: {
        default: '',
        parseHTML: (element) => element.getAttribute('data-decision') || '',
        renderHTML: (attributes) => ({
          'data-decision': attributes.decision,
        }),
      },
      rationale: {
        default: '',
        parseHTML: (element) => element.getAttribute('data-rationale') || '',
        renderHTML: (attributes) => ({
          'data-rationale': attributes.rationale,
        }),
      },
      alternatives: {
        default: [],
        parseHTML: (element) => {
          const data = element.getAttribute('data-alternatives');
          return data ? JSON.parse(data) : [];
        },
        renderHTML: (attributes) => ({
          'data-alternatives': JSON.stringify(attributes.alternatives),
        }),
      },
    };
  },

  parseHTML() {
    return [{ tag: 'div[data-type="decision-block"]' }];
  },

  renderHTML({ HTMLAttributes, node }) {
    const attrs = node.attrs as DecisionBlockAttributes;

    return [
      'div',
      mergeAttributes(HTMLAttributes, {
        'data-type': 'decision-block',
        class: 'decision-block border-l-4 border-blue-500 bg-blue-50 dark:bg-blue-900/20 p-4 my-2 rounded-r',
      }),
      [
        'div',
        { class: 'space-y-3' },
        // Header
        [
          'div',
          { class: 'flex items-center gap-2 text-blue-600 dark:text-blue-400 font-semibold text-sm' },
          ['span', { class: 'text-lg' }, '🎯'],
          ['span', {}, 'Decision Record'],
        ],
        // Context
        attrs.context
          ? [
              'div',
              { class: 'text-sm' },
              ['span', { class: 'font-medium text-gray-700 dark:text-gray-300' }, 'Context: '],
              ['span', { class: 'text-gray-600 dark:text-gray-400' }, attrs.context],
            ]
          : '',
        // Decision
        [
          'div',
          { class: 'text-sm' },
          ['span', { class: 'font-medium text-gray-700 dark:text-gray-300' }, 'Decision: '],
          ['span', { class: 'text-gray-800 dark:text-gray-200 font-medium' }, attrs.decision || 'No decision specified'],
        ],
        // Rationale
        attrs.rationale
          ? [
              'div',
              { class: 'text-sm' },
              ['span', { class: 'font-medium text-gray-700 dark:text-gray-300' }, 'Rationale: '],
              ['span', { class: 'text-gray-600 dark:text-gray-400' }, attrs.rationale],
            ]
          : '',
        // Alternatives
        attrs.alternatives && attrs.alternatives.length > 0
          ? [
              'div',
              { class: 'text-sm' },
              ['span', { class: 'font-medium text-gray-700 dark:text-gray-300' }, 'Alternatives considered: '],
              [
                'ul',
                { class: 'list-disc list-inside text-gray-600 dark:text-gray-400 ml-2' },
                ...attrs.alternatives.map((alt: string) => ['li', {}, alt]),
              ],
            ]
          : '',
      ].filter(Boolean),
    ];
  },

  addCommands() {
    return {
      insertDecisionBlock:
        (attrs?: Partial<DecisionBlockAttributes>) =>
        ({ commands }) => {
          return commands.insertContent({
            type: this.name,
            attrs: {
              context: attrs?.context || '',
              decision: attrs?.decision || '',
              rationale: attrs?.rationale || '',
              alternatives: attrs?.alternatives || [],
            },
          });
        },
    };
  },
});

export default DecisionBlock;
