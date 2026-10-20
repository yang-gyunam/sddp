<!-- Idiom: CodeEditor — read-only source viewer backed by Monaco -->
<script lang="ts">
  import { onDestroy, onMount, untrack } from 'svelte';

  interface Props {
    /** File contents to display. */
    value: string;
    /** Monaco language id, e.g. "csharp". Resolved server-side from the file extension. */
    language?: string;
    /** v1 is review-only; annotation will flip this. */
    readOnly?: boolean;
    /** Height of the editor surface. */
    height?: string;
    /** Fill a parent editor pane without a nested card frame. */
    embedded?: boolean;
    class?: string;
  }

  let {
    value,
    language = 'plaintext',
    readOnly = true,
    height = '100%',
    embedded = false,
    class: className = '',
  }: Props = $props();

  let container = $state<HTMLDivElement | null>(null);
  let loadFailed = $state(false);

  // Monaco is large, so it is imported on demand rather than bundled into the initial
  // chunk. Anything holding editor state lives outside $state — Svelte proxies would
  // interfere with Monaco's own object identity.
  let editor: { dispose(): void; setValue(v: string): void; getValue(): string } | null = null;
  let model: { dispose(): void } | null = null;
  let monacoModule: typeof import('monaco-editor/editor/editor.api') | null = null;

  // Guards a race: if the component unmounts, or props change, while the dynamic import
  // is still in flight, the resolved module must not build an editor for a stale node.
  let mountToken = 0;

  async function mount(node: HTMLDivElement, initialValue: string, lang: string) {
    const token = ++mountToken;

    try {
      // editor.api + basic-languages gives Monarch syntax highlighting on the main
      // thread. The full "monaco-editor" barrel additionally pulls the TypeScript, CSS,
      // HTML and JSON language services, which emit ~8.8MB of workers (ts.worker alone
      // is 6.7MB) to power IntelliSense a read-only viewer never asks for.
      const api = await import('monaco-editor/editor/editor.api');
      // Side-effect import: registers the Monarch tokenizers. It ships no type
      // declarations, and the suppression has to live here rather than in an ambient
      // .d.ts because activities and web type-check this file from outside shell.
      // @ts-expect-error untyped side-effect module
      await import('monaco-editor/basic-languages/monaco.contribution');
      monacoModule ??= api;
    } catch {
      loadFailed = true;
      return;
    }

    if (token !== mountToken) return;

    const monaco = monacoModule;
    model = monaco.editor.createModel(initialValue, lang);
    editor = monaco.editor.create(node, {
      model: model as never,
      readOnly,
      // A viewer, not an IDE: drop the affordances that imply editing.
      automaticLayout: true,
      minimap: { enabled: false },
      overviewRulerLanes: 0,
      hideCursorInOverviewRuler: true,
      overviewRulerBorder: false,
      scrollbar: {
        // Match the app's native gutters with a narrower, rounded thumb on both axes.
        vertical: 'visible',
        horizontal: 'visible',
        verticalScrollbarSize: 14,
        horizontalScrollbarSize: 14,
        verticalSliderSize: 8,
        horizontalSliderSize: 8,
        useShadows: false,
        scrollByPage: true,
      },
      scrollBeyondLastLine: false,
      renderLineHighlight: readOnly ? 'none' : 'line',
      fontSize: 13,
      lineNumbers: 'on',
      theme: prefersDark() ? 'vs-dark' : 'vs',
    });
  }

  function prefersDark(): boolean {
    const root = document.documentElement;
    const explicit = root.getAttribute('data-theme');
    if (explicit === 'dark') return true;
    if (explicit === 'light') return false;
    if (root.classList.contains('dark')) return true;
    if (root.classList.contains('light')) return false;
    if (root.style.colorScheme === 'light') return false;
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }

  onMount(() => {
    const updateTheme = () => monacoModule?.editor.setTheme(prefersDark() ? 'vs-dark' : 'vs');
    const observer = new MutationObserver(updateTheme);
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class', 'data-theme', 'style'],
    });
    const media = window.matchMedia?.('(prefers-color-scheme: dark)');
    media?.addEventListener('change', updateTheme);
    return () => {
      observer.disconnect();
      media?.removeEventListener('change', updateTheme);
    };
  });

  function teardown() {
    mountToken++;
    editor?.dispose();
    editor = null;
    model?.dispose();
    model = null;
  }

  $effect(() => {
    const node = container;
    if (!node) return;

    // Re-read on change, but keep the async work out of the reactive graph.
    const currentValue = value;
    const currentLanguage = language;

    untrack(() => {
      teardown();
      loadFailed = false;
      void mount(node, currentValue, currentLanguage);
    });

    return teardown;
  });

  onDestroy(teardown);
</script>

{#if loadFailed}
  <pre
    class="overflow-auto {embedded
      ? ''
      : 'rounded border border-[var(--color-border)]'} bg-[var(--color-surface-2)] p-3 font-mono text-xs text-[var(--color-text-secondary)] {className}"
    style="height: {height}">{value}</pre>
{:else}
  <div
    bind:this={container}
    class="code-editor overflow-hidden {embedded
      ? ''
      : 'rounded border border-[var(--color-border)]'} {className}"
    style="height: {height}"
    data-testid="code-editor"
  ></div>
{/if}

<style>
  .code-editor :global(.monaco-scrollable-element > .scrollbar) {
    background: var(--color-bg-secondary);
  }
  .code-editor :global(.monaco-scrollable-element > .scrollbar > .slider) {
    border-radius: 4px;
    background: light-dark(var(--color-neutral-400), var(--color-neutral-500));
  }
  .code-editor :global(.monaco-scrollable-element > .scrollbar > .slider:hover),
  .code-editor :global(.monaco-scrollable-element > .scrollbar > .slider.active) {
    background: light-dark(var(--color-neutral-500), var(--color-neutral-400));
  }
</style>
