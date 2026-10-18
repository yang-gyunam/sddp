import { onMount, untrack } from 'svelte';
import { SvelteURLSearchParams } from 'svelte/reactivity';
import { RouterService } from '@sddp/shell';

/** URL selection belongs to the active editor tab, never to all matching pages. */
export function useDetailRoute(options: {
  tabId: () => string;
  path: () => string;
  selection: () => string | null;
  ready?: () => boolean;
  restore: (id: string | null) => void | boolean | Promise<void | boolean>;
}) {
  let mounted = $state(false);
  let applying = $state(false);
  let initialized = false;
  let last: string | null = null;
  let generation = 0;
  async function restore() {
    if (
      !RouterService.isActiveTab(options.tabId()) ||
      window.location.pathname !== options.path() ||
      options.ready?.() === false
    )
      return;
    initialized = true;
    const id = new SvelteURLSearchParams(window.location.search).get('id');
    last = id;
    if (id === options.selection()) return;
    const request = ++generation;
    applying = true;
    let accepted: void | boolean = undefined;
    try {
      accepted = await options.restore(id);
      if (
        accepted === false &&
        request === generation &&
        RouterService.isActiveTab(options.tabId())
      ) {
        const current = options.selection();
        RouterService.replace(
          options.path() + (current ? `?id=${encodeURIComponent(current)}` : '')
        );
      }
    } finally {
      if (request === generation) {
        last = accepted === false ? options.selection() : id;
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
    const ready = options.ready?.() !== false;
    if (!mounted || !ready || applying) return;
    if (!initialized) {
      untrack(() => {
        void restore();
      });
      return;
    }
    if (id === last) return;
    last = id;
    untrack(() => {
      if (RouterService.isActiveTab(options.tabId())) RouterService.selectItem(id, options.tabId());
    });
  });
}
