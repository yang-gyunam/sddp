import { renderMarkdown } from '@sddp/shell';

const block = /^(ARTICLE|BLOCKQUOTE|DD|DIV|DT|H[1-6]|LI|P|SECTION|TD|TH)$/;

function readNode(node: Node): string {
  if (node.nodeType === Node.TEXT_NODE) return node.textContent ?? '';
  if (node instanceof Element) {
    if (node.matches('button, input, script, style, [hidden], [aria-hidden=true]')) return '';
    if (node.matches('pre')) return '\nCode block.\n';
    if (node.matches('br')) return '\n';
  }
  const text = Array.from(node.childNodes, readNode).join('');
  return node instanceof Element && block.test(node.tagName) ? `\n${text}\n` : text;
}

export function readableText(node: Node): string {
  return readNode(node)
    .replace(/https?:\/\/\S{32,}/gi, 'link')
    .replace(/\s+/g, ' ')
    .trim();
}

export function readableHtml(html: string): string {
  const template = document.createElement('template');
  template.innerHTML = html;
  return readableText(template.content);
}

export function readableMarkdown(markdown: string): string {
  return readableHtml(renderMarkdown(markdown));
}
