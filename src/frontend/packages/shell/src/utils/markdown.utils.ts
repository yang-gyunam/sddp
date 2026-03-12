/**
 * Shared markdown-to-HTML renderer (regex-based, no external deps).
 * Supports GitHub Flavored Markdown subset: headers, bold, italic,
 * strikethrough, inline code, fenced code blocks, lists, blockquotes, links.
 * HTML tags are escaped to prevent XSS.
 */

/** Escape HTML special characters to prevent XSS */
function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

/**
 * Convert a markdown string to sanitised HTML.
 *
 * Strategy:
 * 1. Escape raw HTML first (XSS prevention).
 * 2. Extract fenced code blocks → placeholders (so inner text is not parsed).
 * 3. Apply block-level transforms (headers, blockquotes, lists).
 * 4. Apply inline transforms (bold, italic, strikethrough, code, links).
 * 5. Convert remaining newlines to `<br>`.
 * 6. Restore code block placeholders.
 */
export function renderMarkdown(text: string): string {
  if (!text) return '';

  let html = escapeHtml(text);

  // --- 1. Extract fenced code blocks ----------------------------------------
  const codeBlocks: string[] = [];
  html = html.replace(/```(\w*)\n([\s\S]*?)```/g, (_match, lang: string, code: string) => {
    const idx = codeBlocks.length;
    const langAttr = lang ? ` data-lang="${lang}"` : '';
    codeBlocks.push(
      `<pre class="md-code-block"${langAttr}><code>${code.replace(/\n$/, '')}</code></pre>`,
    );
    return `\x00CB${idx}\x00`;
  });

  // --- 2. Block-level transforms (line-based) --------------------------------
  const lines = html.split('\n');
  const output: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;

    // Headers (## h2, ### h3, etc.)
    const headerMatch = line.match(/^(#{1,6})\s+(.+)$/);
    if (headerMatch) {
      const level = headerMatch[1]!.length;
      output.push(`<h${level} class="md-h${level}">${headerMatch[2]}</h${level}>`);
      i++;
      continue;
    }

    // Blockquote (> ...)
    if (line.startsWith('&gt; ')) {
      const quoteLines: string[] = [];
      while (i < lines.length && lines[i]!.startsWith('&gt; ')) {
        quoteLines.push(lines[i]!.slice(5)); // length of '&gt; '
        i++;
      }
      output.push(`<blockquote class="md-blockquote">${quoteLines.join('<br>')}</blockquote>`);
      continue;
    }

    // Unordered list (- item)
    if (/^[-*]\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^[-*]\s+/.test(lines[i]!)) {
        items.push(`<li>${lines[i]!.replace(/^[-*]\s+/, '')}</li>`);
        i++;
      }
      output.push(`<ul class="md-list">${items.join('')}</ul>`);
      continue;
    }

    // Ordered list (1. item)
    if (/^\d+\.\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^\d+\.\s+/.test(lines[i]!)) {
        items.push(`<li>${lines[i]!.replace(/^\d+\.\s+/, '')}</li>`);
        i++;
      }
      output.push(`<ol class="md-list">${items.join('')}</ol>`);
      continue;
    }

    // Regular line
    output.push(line);
    i++;
  }

  html = output.join('\n');

  // --- 3. Inline transforms ---------------------------------------------------
  // Bold **text**
  html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  // Italic *text* (but not inside a word like a*b*c)
  html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
  // Strikethrough ~~text~~
  html = html.replace(/~~(.+?)~~/g, '<del>$1</del>');
  // Inline code `code`
  html = html.replace(/`(.+?)`/g, '<code class="md-inline-code">$1</code>');
  // Links [text](url)
  html = html.replace(
    /\[([^\]]+)\]\(([^)]+)\)/g,
    '<a href="$2" target="_blank" rel="noopener noreferrer" class="md-link">$1</a>',
  );

  // --- 4. Newlines → <br> (skip after block elements) ------------------------
  html = html.replace(/\n/g, '<br>');
  // Remove <br> right after block closing tags
  html = html.replace(/(<\/(?:h[1-6]|blockquote|ul|ol|pre|li)>)<br>/g, '$1');
  // Remove <br> right before block opening tags
  html = html.replace(/<br>(<(?:h[1-6]|blockquote|ul|ol|pre)[ >])/g, '$1');

  // --- 5. Restore code block placeholders ------------------------------------
  // eslint-disable-next-line no-control-regex -- \x00 used intentionally as code block placeholder
  html = html.replace(/\x00CB(\d+)\x00/g, (_m, idx: string) => codeBlocks[Number(idx)] ?? '');

  return html;
}
