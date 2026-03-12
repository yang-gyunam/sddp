/**
 * GlossaryLinkSuggestion Extension
 * TipTap Suggestion plugin for glossary term autocomplete
 * Triggered by `[[` input
 */
import { Extension } from '@tiptap/core';
import Suggestion, { type SuggestionOptions } from '@tiptap/suggestion';
import type { GlossaryTermSummary } from '../../glossary/types';
import { autocomplete } from '../../glossary/services/GlossaryService';

export interface GlossaryLinkSuggestionOptions {
  tenantId: string;
  projectId: string;
  suggestion?: Partial<SuggestionOptions<GlossaryTermSummary>>;
}

export const GlossaryLinkSuggestion = Extension.create<GlossaryLinkSuggestionOptions>({
  name: 'glossaryLinkSuggestion',

  addOptions() {
    return {
      tenantId: '',
      projectId: '',
      suggestion: {},
    };
  },

  addProseMirrorPlugins() {
    const { tenantId, projectId, suggestion } = this.options;

    return [
      Suggestion<GlossaryTermSummary>({
        editor: this.editor,
        char: '[[',
        allowSpaces: true,
        startOfLine: false,

        items: async ({ query }) => {
          if (!query || query.length < 2) return [];
          if (!tenantId || !projectId) return [];

          try {
            const results = await autocomplete(tenantId, projectId, query, 10);
            // Only return active terms
            return results.filter((r) => r.status === 'Active');
          } catch (error) {
            console.error('Glossary autocomplete error:', error);
            return [];
          }
        },

        command: ({ editor, range, props }) => {
          const term = props;
          editor
            .chain()
            .focus()
            .deleteRange(range)
            .insertContent([
              {
                type: 'text',
                marks: [
                  {
                    type: 'glossaryLink',
                    attrs: {
                      termId: term.id,
                      term: term.term,
                      category: term.category,
                    },
                  },
                ],
                text: term.term,
              },
            ])
            .run();
        },

        // Merge any custom suggestion options
        ...suggestion,
      }),
    ];
  },
});

export default GlossaryLinkSuggestion;
