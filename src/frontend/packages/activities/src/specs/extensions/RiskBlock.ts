/**
 * RiskBlock Extension
 * TipTap Node for risk documentation with impact/probability assessment
 */
import { Node, mergeAttributes } from '@tiptap/core';

export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export interface RiskBlockAttributes {
  description: string;
  impact: RiskLevel;
  probability: RiskLevel;
  mitigation: string;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    riskBlock: {
      insertRiskBlock: (attrs?: Partial<RiskBlockAttributes>) => ReturnType;
    };
  }
}

const RISK_COLORS: Record<RiskLevel, { border: string; bg: string; text: string; badge: string }> = {
  Low: {
    border: 'border-green-500',
    bg: 'bg-green-50 dark:bg-green-900/20',
    text: 'text-green-600 dark:text-green-400',
    badge: 'bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300',
  },
  Medium: {
    border: 'border-yellow-500',
    bg: 'bg-yellow-50 dark:bg-yellow-900/20',
    text: 'text-yellow-600 dark:text-yellow-400',
    badge: 'bg-yellow-100 dark:bg-yellow-800 text-yellow-700 dark:text-yellow-300',
  },
  High: {
    border: 'border-orange-500',
    bg: 'bg-orange-50 dark:bg-orange-900/20',
    text: 'text-orange-600 dark:text-orange-400',
    badge: 'bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300',
  },
  Critical: {
    border: 'border-red-500',
    bg: 'bg-red-50 dark:bg-red-900/20',
    text: 'text-red-600 dark:text-red-400',
    badge: 'bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300',
  },
};

function getRiskScore(level: RiskLevel): number {
  const scores: Record<RiskLevel, number> = { Low: 1, Medium: 2, High: 3, Critical: 4 };
  return scores[level] || 2;
}

function getOverallRisk(impact: RiskLevel, probability: RiskLevel): RiskLevel {
  const score = getRiskScore(impact) * getRiskScore(probability);
  if (score <= 2) return 'Low';
  if (score <= 6) return 'Medium';
  if (score <= 12) return 'High';
  return 'Critical';
}

export const RiskBlock = Node.create({
  name: 'riskBlock',
  group: 'block',
  atom: true,
  draggable: true,

  addAttributes() {
    return {
      description: {
        default: '',
        parseHTML: (element) => element.getAttribute('data-description') || '',
        renderHTML: (attributes) => ({
          'data-description': attributes.description,
        }),
      },
      impact: {
        default: 'Medium' as RiskLevel,
        parseHTML: (element) => (element.getAttribute('data-impact') as RiskLevel) || 'Medium',
        renderHTML: (attributes) => ({
          'data-impact': attributes.impact,
        }),
      },
      probability: {
        default: 'Medium' as RiskLevel,
        parseHTML: (element) => (element.getAttribute('data-probability') as RiskLevel) || 'Medium',
        renderHTML: (attributes) => ({
          'data-probability': attributes.probability,
        }),
      },
      mitigation: {
        default: '',
        parseHTML: (element) => element.getAttribute('data-mitigation') || '',
        renderHTML: (attributes) => ({
          'data-mitigation': attributes.mitigation,
        }),
      },
    };
  },

  parseHTML() {
    return [{ tag: 'div[data-type="risk-block"]' }];
  },

  renderHTML({ HTMLAttributes, node }) {
    const attrs = node.attrs as RiskBlockAttributes;
    const overallRisk = getOverallRisk(attrs.impact, attrs.probability);
    const colors = RISK_COLORS[overallRisk];

    return [
      'div',
      mergeAttributes(HTMLAttributes, {
        'data-type': 'risk-block',
        class: `risk-block border-l-4 ${colors.border} ${colors.bg} p-4 my-2 rounded-r`,
      }),
      [
        'div',
        { class: 'space-y-3' },
        // Header
        [
          'div',
          { class: 'flex items-center justify-between' },
          [
            'div',
            { class: `flex items-center gap-2 ${colors.text} font-semibold text-sm` },
            ['span', { class: 'text-lg' }, '⚠️'],
            ['span', {}, 'Risk'],
          ],
          [
            'span',
            { class: `text-xs px-2 py-0.5 rounded-full font-medium ${colors.badge}` },
            overallRisk,
          ],
        ],
        // Description
        [
          'div',
          { class: 'text-sm text-gray-800 dark:text-gray-200' },
          attrs.description || 'No description provided',
        ],
        // Impact & Probability
        [
          'div',
          { class: 'flex gap-4 text-xs' },
          [
            'div',
            { class: 'flex items-center gap-1' },
            ['span', { class: 'font-medium text-gray-600 dark:text-gray-400' }, 'Impact:'],
            [
              'span',
              { class: `px-1.5 py-0.5 rounded ${RISK_COLORS[attrs.impact].badge}` },
              attrs.impact,
            ],
          ],
          [
            'div',
            { class: 'flex items-center gap-1' },
            ['span', { class: 'font-medium text-gray-600 dark:text-gray-400' }, 'Probability:'],
            [
              'span',
              { class: `px-1.5 py-0.5 rounded ${RISK_COLORS[attrs.probability].badge}` },
              attrs.probability,
            ],
          ],
        ],
        // Mitigation
        attrs.mitigation
          ? [
              'div',
              { class: 'text-sm border-t border-gray-200 dark:border-gray-700 pt-2 mt-2' },
              [
                'span',
                { class: 'font-medium text-gray-700 dark:text-gray-300' },
                'Mitigation: ',
              ],
              ['span', { class: 'text-gray-600 dark:text-gray-400' }, attrs.mitigation],
            ]
          : '',
      ].filter(Boolean),
    ];
  },

  addCommands() {
    return {
      insertRiskBlock:
        (attrs?: Partial<RiskBlockAttributes>) =>
        ({ commands }) => {
          return commands.insertContent({
            type: this.name,
            attrs: {
              description: attrs?.description || '',
              impact: attrs?.impact || 'Medium',
              probability: attrs?.probability || 'Medium',
              mitigation: attrs?.mitigation || '',
            },
          });
        },
    };
  },
});

export default RiskBlock;
