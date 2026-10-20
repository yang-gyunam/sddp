import { onMount, untrack } from 'svelte';
import { SvelteURLSearchParams } from 'svelte/reactivity';
import { RouterService } from '@sddp/shell';

/** URL selection belongs to the active editor tab, never to all matching pages. */
export function useDetailRoute(options: {
  tabId: () => string;
  path: () => string;
  selection: () => string | null;
  query?: () => Record<string, string | null>;
  ready?: () => boolean;
  restore: (id: string | null, query?: URLSearchParams) => void | boolean | Promise<void | boolean>;
}) {
  let mounted = $state(false);
  let applying = $state(false);
  let initialized = false;
  let last: string | null = null;
  let generation = 0;
  function selectionKey(id: string | null, query = options.query?.()) {
    return JSON.stringify([id, Object.entries(query ?? {}).sort(([a], [b]) => a.localeCompare(b))]);
  }
  async function restore() {
    if (
      !RouterService.isActiveTab(options.tabId()) ||
      window.location.pathname !== options.path() ||
      options.ready?.() === false
    )
      return;
    initialized = true;
    const params = new SvelteURLSearchParams(window.location.search);
    const id = params.get('id');
    const query = Object.fromEntries(
      Object.keys(options.query?.() ?? {}).map((key) => [key, params.get(key)])
    );
    const key = selectionKey(id, query);
    last = key;
    if (key === selectionKey(options.selection())) return;
    const request = ++generation;
    applying = true;
    let accepted: void | boolean = undefined;
    try {
      accepted = await (options.query ? options.restore(id, params) : options.restore(id));
      if (
        accepted === false &&
        request === generation &&
        RouterService.isActiveTab(options.tabId())
      ) {
        const current = options.selection();
        const restored = new SvelteURLSearchParams();
        if (current) restored.set('id', current);
        for (const [name, value] of Object.entries(options.query?.() ?? {})) {
          if (value !== null) restored.set(name, value);
        }
        RouterService.replace(options.path() + (restored.size ? `?${restored}` : ''));
      }
    } finally {
      if (request === generation) {
        last = accepted === false ? selectionKey(options.selection()) : key;
        applying = false;
      }
    }
  }
  onMount(() => {
    mounted = true;
    window.addEventListener('sddp:routechange', restore);
    return () => {
      ++generation;
      window.removeEventListener('sddp:routechange', restore);
    };
  });
  $effect(() => {
    const id = options.selection();
    const query = options.query?.();
    const key = selectionKey(id, query);
    const ready = options.ready?.() !== false;
    if (!mounted || !ready || applying) return;
    if (!initialized) {
      untrack(() => {
        void restore();
      });
      return;
    }
    if (key === last) return;
    last = key;
    untrack(() => {
      if (RouterService.isActiveTab(options.tabId())) {
        if (query) RouterService.selectItem(id, options.tabId(), query);
        else RouterService.selectItem(id, options.tabId());
      }
    });
  });
}
