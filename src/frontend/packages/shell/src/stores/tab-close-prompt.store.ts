
import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { setTabCloseDecider, type TabCloseOutcome, type TabCloseRequest } from './tab-close';

export interface TabClosePromptState {
  /** The prompt awaiting an answer, or null when nothing is being asked. */
  current: TabCloseRequest | null;
  /** How many further prompts are waiting behind it. */
  queued: number;
}

interface PendingPrompt {
  prompt: TabCloseRequest;
  answer: (outcome: TabCloseOutcome) => void;
}

const pending: PendingPrompt[] = [];
const { subscribe, set } = writable<TabClosePromptState>({ current: null, queued: 0 });

let currentState: TabClosePromptState = { current: null, queued: 0 };

function publish(): void {
  currentState = {
    current: pending[0]?.prompt ?? null,
    queued: Math.max(pending.length - 1, 0),
  };
  set(currentState);
}

export const tabClosePromptStore = {
  subscribe: (run: Subscriber<TabClosePromptState>): Unsubscriber => subscribe(run),
  get: (): TabClosePromptState => currentState,
};

/**
 * Answer the prompt at the head of the queue. Answering something nobody asked is a no-op rather
 * than an error — a stray click on a closing dialog should not throw.
 */
export function answerTabClosePrompt(outcome: TabCloseOutcome): void {
  const head = pending.shift();
  publish();
  head?.answer(outcome);
}

/**
 * Point the close guard at this prompt. Returns the uninstall function, which cancels everything
 * still queued — an unmounted dialog can no longer answer, and cancel is the answer that loses
 * nothing.
 */
export function installTabClosePromptDecider(): () => void {
  setTabCloseDecider(
    (prompt) =>
      new Promise<TabCloseOutcome>((resolve) => {
        pending.push({ prompt, answer: resolve });
        publish();
      })
  );

  return () => {
    setTabCloseDecider(null);
    while (pending.length > 0) {
      pending.shift()!.answer('cancel');
    }
    publish();
  };
}
