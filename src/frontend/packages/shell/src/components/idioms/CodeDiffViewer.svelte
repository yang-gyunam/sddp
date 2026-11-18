<!-- Idiom: read-only text comparison; callers own data, permissions and structural meaning. -->
<script lang="ts">
  import { onMount, untrack } from 'svelte';
  import type { editor as MonacoEditor } from 'monaco-editor/editor/editor.api';

  let {
    original,
    modified,
    language = 'plaintext',
    originalLabel = 'Left revision',
    modifiedLabel = 'Right revision',
  }: {
    original: string;
    modified: string;
    language?: string;
    originalLabel?: string;
    modifiedLabel?: string;
  } = $props();

  let container = $state<HTMLDivElement | null>(null);
  let loadFailed = $state(false);
  let loading = $state(true);
  // Monaco objects must retain their identity outside Svelte's reactive proxies.
  let editor: MonacoEditor.IStandaloneDiffEditor | null = null;
  let leftModel: MonacoEditor.ITextModel | null = null;
  let rightModel: MonacoEditor.ITextModel | null = null;
  let api: typeof import('monaco-editor/editor/editor.api') | null = null;
  let mountToken = 0;

  function teardown() {
    ++mountToken;
    editor?.dispose();
    editor = null;
    leftModel?.dispose();
    rightModel?.dispose();
    leftModel = rightModel = null;
  }

  function prefersDark(): boolean {
    const root = document.documentElement;
    const explicit = root.getAttribute('data-theme');
    if (explicit === 'dark') return true;
    if (explicit === 'light') return false;
    if (root.classList.contains('dark')) return true;
    if (root.classList.contains('light') || root.style.colorScheme === 'light') return false;
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }

  async function mount(
    node: HTMLDivElement,
    left: string,
    right: string,
    lang: string,
    leftLabel: string,
    rightLabel: string
  ) {
    const token = ++mountToken;
    try {
      const [monaco, worker] = await Promise.all([
        import('monaco-editor/editor/editor.api'),
        import('monaco-editor/editor/editor.worker?worker'),
      ]);
      // Monarch highlighting only; no language service workers are needed.
      // @ts-expect-error untyped side-effect module
      await import('monaco-editor/basic-languages/monaco.contribution');
      if (token !== mountToken) return;

      api = monaco;
      if (!globalThis.MonacoEnvironment?.getWorker && !globalThis.MonacoEnvironment?.getWorkerUrl) {
        globalThis.MonacoEnvironment = {
          ...globalThis.MonacoEnvironment,
          getWorker: () => new worker.default(),
        };
      }
      leftModel = monaco.editor.createModel(left, lang);
      rightModel = monaco.editor.createModel(right, lang);
      editor = monaco.editor.createDiffEditor(node, {
        readOnly: true,
        domReadOnly: true,
        originalEditable: false,
        originalAriaLabel: leftLabel,
        modifiedAriaLabel: rightLabel,
        diffAlgorithm: 'advanced',
        ignoreTrimWhitespace: false,
        maxComputationTime: 5000,
        automaticLayout: true,
        renderSideBySide: true,
        useInlineViewWhenSpaceIsLimited: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 13,
        theme: prefersDark() ? 'vs-dark' : 'vs',
      });
      editor.setModel({ original: leftModel, modified: rightModel });
      loading = false;
    } catch {
      if (token !== mountToken) return;
      teardown();
      loading = false;
      loadFailed = true;
    }
  }

  $effect(() => {
    const node = container;
    if (!node) return;
    const left = original;
    const right = modified;
    const lang = language;
    const leftLabel = originalLabel;
    const rightLabel = modifiedLabel;
    untrack(() => {
      teardown();
      loadFailed = false;
      loading = true;
      void mount(node, left, right, lang, leftLabel, rightLabel);
    });
    return teardown;
  });

  onMount(() => {
    const updateTheme = () => api?.editor.setTheme(prefersDark() ? 'vs-dark' : 'vs');
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
</script>

<div aria-label="Text comparison" class="min-w-0 space-y-2">
  <div class="grid grid-cols-2 gap-3 text-xs text-[var(--color-text-secondary)]">
    <p>{originalLabel}</p>
    <p>{modifiedLabel}</p>
  </div>
  {#if loading}<p role="status" class="text-sm">Loading text comparison…</p>{/if}
  {#if loadFailed}
    <p role="status" class="text-sm">Text comparison unavailable. Saved text is shown below.</p>
    {#each [[originalLabel, original], [modifiedLabel, modified]] as [label, text], index (index)}
      <section aria-label={label}>
        <p class="text-xs">{label}</p>
        <pre class="overflow-auto whitespace-pre-wrap break-words p-3 text-xs">{text}</pre>
      </section>
    {/each}
  {/if}
  <div
    bind:this={container}
    hidden={loadFailed}
    class="h-[28rem] min-w-0 overflow-hidden rounded border border-[var(--color-border)]"
    data-testid="code-diff-viewer"
  ></div>
</div>
