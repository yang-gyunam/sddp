export interface SearchSnippetSegment {
  text: string;
  highlighted: boolean;
}

const maxHighlightTerms = 20;
const maxHighlightTermLength = 100;

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * Splits an untrusted search snippet into plain-text segments. Consumers must
 * render `text` through normal Svelte interpolation; this function never emits HTML.
 */
export function createSearchSnippetSegments(
  snippet: string,
  highlightTerms: readonly string[]
): SearchSnippetSegment[] {
  if (!snippet) return [];

  const terms = [
    ...new Set(
      highlightTerms
        .map((term) => term.trim())
        .filter((term) => term.length > 0 && term.length <= maxHighlightTermLength)
    ),
  ]
    .sort((left, right) => right.length - left.length)
    .slice(0, maxHighlightTerms);

  if (terms.length === 0) {
    return [{ text: snippet, highlighted: false }];
  }

  const matcher = new RegExp(terms.map(escapeRegExp).join('|'), 'giu');
  const segments: SearchSnippetSegment[] = [];
  let cursor = 0;

  for (const match of snippet.matchAll(matcher)) {
    const matchIndex = match.index;
    const matchText = match[0];
    if (matchIndex > cursor) {
      segments.push({
        text: snippet.slice(cursor, matchIndex),
        highlighted: false,
      });
    }
    segments.push({ text: matchText, highlighted: true });
    cursor = matchIndex + matchText.length;
  }

  if (cursor < snippet.length) {
    segments.push({ text: snippet.slice(cursor), highlighted: false });
  }

  return segments.length > 0 ? segments : [{ text: snippet, highlighted: false }];
}
