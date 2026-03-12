/**
 * AcceptanceCriteriaBlock Extension
 * TipTap Node for Given/When/Then acceptance criteria checklist
 */
import { Node, mergeAttributes } from '@tiptap/core';

export interface CriteriaItem {
  id: string;
  given: string;
  when: string;
  then: string;
  checked: boolean;
}

export interface AcceptanceCriteriaBlockAttributes {
  title: string;
  criteria: CriteriaItem[];
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    acceptanceCriteriaBlock: {
      insertAcceptanceCriteriaBlock: (attrs?: Partial<AcceptanceCriteriaBlockAttributes>) => ReturnType;
    };
  }
}

function generateId(): string {
  return Math.random().toString(36).substring(2, 9);
}

export const AcceptanceCriteriaBlock = Node.create({
  name: 'acceptanceCriteriaBlock',
  group: 'block',
  atom: true,
  draggable: true,

  addAttributes() {
    return {
      title: {
        default: 'Acceptance Criteria',
        parseHTML: (element) => element.getAttribute('data-title') || 'Acceptance Criteria',
        renderHTML: (attributes) => ({
          'data-title': attributes.title,
        }),
      },
      criteria: {
        default: [],
        parseHTML: (element) => {
          const data = element.getAttribute('data-criteria');
          return data ? JSON.parse(data) : [];
        },
        renderHTML: (attributes) => ({
          'data-criteria': JSON.stringify(attributes.criteria),
        }),
      },
    };
  },

  parseHTML() {
    return [{ tag: 'div[data-type="acceptance-criteria-block"]' }];
  },

  renderHTML({ HTMLAttributes, node }) {
    const attrs = node.attrs as AcceptanceCriteriaBlockAttributes;
    const criteria = attrs.criteria || [];
    const checkedCount = criteria.filter((c) => c.checked).length;
    const totalCount = criteria.length;
    const progressPercent = totalCount > 0 ? Math.round((checkedCount / totalCount) * 100) : 0;

    return [
      'div',
      mergeAttributes(HTMLAttributes, {
        'data-type': 'acceptance-criteria-block',
        class: 'acceptance-criteria-block border-l-4 border-green-500 bg-green-50 dark:bg-green-900/20 p-4 my-2 rounded-r',
      }),
      [
        'div',
        { class: 'space-y-3' },
        // Header with progress
        [
          'div',
          { class: 'flex items-center justify-between' },
          [
            'div',
            { class: 'flex items-center gap-2 text-green-600 dark:text-green-400 font-semibold text-sm' },
            ['span', { class: 'text-lg' }, '✅'],
            ['span', {}, attrs.title],
          ],
          totalCount > 0
            ? [
                'div',
                { class: 'text-xs text-gray-500 dark:text-gray-400' },
                `${checkedCount}/${totalCount} (${progressPercent}%)`,
              ]
            : '',
        ],
        // Progress bar
        totalCount > 0
          ? [
              'div',
              { class: 'h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden' },
              [
                'div',
                {
                  class: 'h-full bg-green-500 transition-all duration-300',
                  style: `width: ${progressPercent}%`,
                },
              ],
            ]
          : '',
        // Criteria list
        criteria.length > 0
          ? [
              'div',
              { class: 'space-y-2' },
              ...criteria.map((item: CriteriaItem) => [
                'div',
                {
                  class: `text-sm p-2 rounded ${
                    item.checked
                      ? 'bg-green-100 dark:bg-green-800/30 line-through opacity-70'
                      : 'bg-white dark:bg-gray-800/50'
                  }`,
                },
                [
                  'div',
                  { class: 'flex items-start gap-2' },
                  [
                    'span',
                    { class: 'text-xs font-mono text-gray-400' },
                    item.checked ? '☑' : '☐',
                  ],
                  [
                    'div',
                    { class: 'flex-1 space-y-1' },
                    item.given
                      ? [
                          'div',
                          {},
                          ['span', { class: 'font-medium text-purple-600 dark:text-purple-400' }, 'Given '],
                          ['span', { class: 'text-gray-700 dark:text-gray-300' }, item.given],
                        ]
                      : '',
                    item.when
                      ? [
                          'div',
                          {},
                          ['span', { class: 'font-medium text-blue-600 dark:text-blue-400' }, 'When '],
                          ['span', { class: 'text-gray-700 dark:text-gray-300' }, item.when],
                        ]
                      : '',
                    item.then
                      ? [
                          'div',
                          {},
                          ['span', { class: 'font-medium text-green-600 dark:text-green-400' }, 'Then '],
                          ['span', { class: 'text-gray-700 dark:text-gray-300' }, item.then],
                        ]
                      : '',
                  ].filter(Boolean),
                ],
              ]),
            ]
          : [
              'div',
              { class: 'text-sm text-gray-500 dark:text-gray-400 italic' },
              'No acceptance criteria defined',
            ],
      ].filter(Boolean),
    ];
  },

  addCommands() {
    return {
      insertAcceptanceCriteriaBlock:
        (attrs?: Partial<AcceptanceCriteriaBlockAttributes>) =>
        ({ commands }) => {
          return commands.insertContent({
            type: this.name,
            attrs: {
              title: attrs?.title || 'Acceptance Criteria',
              criteria: attrs?.criteria || [
                {
                  id: generateId(),
                  given: '',
                  when: '',
                  then: '',
                  checked: false,
                },
              ],
            },
          });
        },
    };
  },
});

export default AcceptanceCriteriaBlock;
