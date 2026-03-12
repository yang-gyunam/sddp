/**
 * ChangeHighlight Mark Extension
 * TipTap Mark for highlighting changes (added, modified, removed)
 */
import { Mark, mergeAttributes } from '@tiptap/core';
import { formatDate } from '@sddp/shell/utils';

export type ChangeType = 'added' | 'modified' | 'removed';

export interface ChangeHighlightAttributes {
  type: ChangeType;
  authorId?: string | null;
  authorName?: string | null;
  timestamp?: string | null;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    changeHighlight: {
      setChangeHighlight: (attrs: ChangeHighlightAttributes) => ReturnType;
      unsetChangeHighlight: () => ReturnType;
      toggleChangeHighlight: (attrs: ChangeHighlightAttributes) => ReturnType;
    };
  }
}

const CHANGE_TYPE_STYLES: Record<ChangeType, string> = {
  added:
    'bg-[var(--color-success-500)]/15 text-[var(--color-success-700)] dark:text-[var(--color-success-300)] border-b border-[var(--color-success-500)]/30',
  modified:
    'bg-[var(--color-warning-500)]/15 text-[var(--color-warning-700)] dark:text-[var(--color-warning-300)] border-b border-[var(--color-warning-500)]/30',
  removed:
    'bg-[var(--color-error-500)]/15 text-[var(--color-error-700)] dark:text-[var(--color-error-300)] line-through border-b border-[var(--color-error-500)]/30',
};

export const ChangeHighlight = Mark.create({
  name: 'changeHighlight',
  priority: 900,
  keepOnSplit: false,
  inclusive: false,

  addAttributes() {
    return {
      type: {
        default: 'modified',
        parseHTML: (element) => element.getAttribute('data-change-type') || 'modified',
        renderHTML: (attributes) => ({
          'data-change-type': attributes.type,
        }),
      },
      authorId: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-author-id'),
        renderHTML: (attributes) =>
          attributes.authorId
            ? { 'data-author-id': attributes.authorId }
            : {},
      },
      authorName: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-author-name'),
        renderHTML: (attributes) =>
          attributes.authorName
            ? { 'data-author-name': attributes.authorName }
            : {},
      },
      timestamp: {
        default: null,
        parseHTML: (element) => element.getAttribute('data-timestamp'),
        renderHTML: (attributes) =>
          attributes.timestamp
            ? { 'data-timestamp': attributes.timestamp }
            : {},
      },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'mark[data-change-highlight]',
        getAttrs: (element) => {
          if (typeof element === 'string') return false;
          return {};
        },
      },
    ];
  },

  renderHTML({ HTMLAttributes }) {
    const changeType = (HTMLAttributes['data-change-type'] || 'modified') as ChangeType;
    const styleClass = CHANGE_TYPE_STYLES[changeType] || CHANGE_TYPE_STYLES.modified;

    // Build tooltip text
    let tooltip = `${changeType.charAt(0).toUpperCase() + changeType.slice(1)}`;
    if (HTMLAttributes['data-author-name']) {
      tooltip += ` by ${HTMLAttributes['data-author-name']}`;
    }
    if (HTMLAttributes['data-timestamp']) {
      try {
        const date = new Date(HTMLAttributes['data-timestamp']);
        tooltip += ` at ${formatDate(date)}`;
      } catch {
        // Ignore date parsing errors
      }
    }

    return [
      'mark',
      mergeAttributes(HTMLAttributes, {
        'data-change-highlight': 'true',
        class: `change-highlight ${styleClass} px-0.5 rounded-sm cursor-help`,
        title: tooltip,
      }),
      0,
    ];
  },

  addCommands() {
    return {
      setChangeHighlight:
        (attrs: ChangeHighlightAttributes) =>
        ({ commands }) => {
          return commands.setMark(this.name, attrs);
        },
      unsetChangeHighlight:
        () =>
        ({ commands }) => {
          return commands.unsetMark(this.name);
        },
      toggleChangeHighlight:
        (attrs: ChangeHighlightAttributes) =>
        ({ commands }) => {
          return commands.toggleMark(this.name, attrs);
        },
    };
  },
});

export default ChangeHighlight;
