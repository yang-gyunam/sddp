
import { tabActions, tabsStore } from './tabs.store';

export type TabCloseOutcome = 'save' | 'discard' | 'cancel';

export interface TabCloseRequest {
  tabId: string;
  title: string;
}

export interface TabCloseGuard {
  /** Whether the tab holds unsaved work. Re-evaluated on every close request. */
  isDirty: () => boolean;
  /** Name to show in the prompt. Falls back to the tab title. */
  describe?: () => string;
  /** Attempt to persist. Resolving false (or throwing) leaves the tab open. */
  save: () => Promise<boolean>;
}

export type TabCloseDecider = (prompt: TabCloseRequest) => Promise<TabCloseOutcome>;

const guards = new Map<string, TabCloseGuard>();
const inFlight = new Map<string, Promise<boolean>>();

/**
 * The default decider keeps today's behavior: a confirmed prompt discards, a dismissed one cancels.
 * It never answers `save`. A host that mounts the three-way dialog installs its own decider.
 *
 * Defaulting to a fixed `cancel` would make tabs unclosable wherever the dialog is not mounted, and
 * defaulting to a fixed `discard` would keep the silent data loss this guard exists to stop.
 */
const confirmDecider: TabCloseDecider = async () => {
  const confirm = globalThis.confirm;
  if (typeof confirm !== 'function') return 'cancel';
  return confirm.call(globalThis, 'Discard unsaved changes?') ? 'discard' : 'cancel';
};

let decider: TabCloseDecider = confirmDecider;

/** Pass null to restore the confirm-backed default. */
export function setTabCloseDecider(next: TabCloseDecider | null): void {
  decider = next ?? confirmDecider;
}

/** Returns the unregister function; components pass it straight out of `onMount`. */
export function registerTabCloseGuard(tabId: string, guard: TabCloseGuard): () => void {
  if (!tabId) return () => {};

  guards.set(tabId, guard);
  return () => {
    if (guards.get(tabId) === guard) {
      guards.delete(tabId);
    }
  };
}

export function clearTabCloseGuard(tabId: string): void {
  guards.delete(tabId);
}

export function resetTabCloseGuards(): void {
  guards.clear();
  inFlight.clear();
}

function tabTitle(tabId: string): string {
  for (const group of tabsStore.get().groups) {
    const tab = group.tabs.find((candidate) => candidate.id === tabId);
    if (tab) return tab.title ?? '';
  }
  return '';
}

/**
 * `closeTab` drops the tab state of every tab it removes, and refuses to remove a pinned one.
 *
 * The guard is forgotten only when the tab is actually gone. A pinned tab stays, and its component
 * stays mounted — and since components register on mount, dropping the guard here would leave that
 * tab unguarded for every later attempt.
 */
function close(tabId: string, groupId?: string): boolean {
  tabActions.closeTab(tabId, groupId);

  const closed = !tabsStore
    .get()
    .groups.some((group) => group.tabs.some((tab) => tab.id === tabId));
  if (closed) guards.delete(tabId);
  return closed;
}

async function resolveClose(tabId: string, groupId: string | undefined): Promise<boolean> {
  const guard = guards.get(tabId);
  if (!guard || !guard.isDirty()) return close(tabId, groupId);

  const outcome = await decider({ tabId, title: guard.describe?.() ?? tabTitle(tabId) });
  if (outcome === 'cancel') return false;

  if (outcome === 'save') {
    let saved = false;
    try {
      saved = await guard.save();
    } catch {
      saved = false;
    }
    if (!saved) return false;
  }

  return close(tabId, groupId);
}

/**
 * Ask to close one tab. Resolves to whether the tab is gone.
 *
 * A second request for the same tab joins the first rather than opening a second prompt.
 */
export function requestCloseTab(tabId: string, groupId?: string): Promise<boolean> {
  const pending = inFlight.get(tabId);
  if (pending) return pending;

  const request = resolveClose(tabId, groupId).finally(() => {
    inFlight.delete(tabId);
  });
  inFlight.set(tabId, request);
  return request;
}

/**
 * Ask to close several tabs, one prompt at a time. A cancel stops there and leaves the rest open,
 * so "close all, then cancel" never keeps closing tabs behind the cancelled one.
 */
export async function requestCloseTabs(tabIds: string[], groupId?: string): Promise<boolean> {
  for (const tabId of tabIds) {
    if (!(await requestCloseTab(tabId, groupId))) return false;
  }
  return true;
}
