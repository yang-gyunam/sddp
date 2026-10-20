import { writable } from 'svelte/store';

export interface ConfirmationChoice<T extends string = string> {
  value: T;
  label: string;
  variant?: 'primary' | 'secondary' | 'danger';
}

export interface ChoiceRequest<T extends string = string> {
  title: string;
  message: string;
  choices: readonly ConfirmationChoice<T>[];
  signal?: AbortSignal;
}

export interface ConfirmationOptions {
  title?: string;
  confirmLabel?: string;
  variant?: 'primary' | 'danger';
  signal?: AbortSignal;
}

export interface ConfirmationPromptState {
  current: (Omit<ChoiceRequest, 'signal'> & { id: number }) | null;
  queued: number;
}

interface PendingRequest {
  prompt: NonNullable<ConfirmationPromptState['current']>;
  resolve: (value: string | null) => void;
  cleanup: () => void;
}

const pending: PendingRequest[] = [];
let nextId = 0;
let hosts = 0;
let state: ConfirmationPromptState = { current: null, queued: 0 };
const { subscribe, set } = writable(state);

function publish(): void {
  state = { current: pending[0]?.prompt ?? null, queued: Math.max(0, pending.length - 1) };
  set(state);
}

function settle(id: number, value: string | null): void {
  const index = pending.findIndex((request) => request.prompt.id === id);
  if (index < 0) return;
  const [request] = pending.splice(index, 1);
  if (!request) return;
  request.cleanup();
  publish();
  request.resolve(value);
}

export const confirmationStore = { subscribe, get: () => state };

/** Only the visible request may be answered; stale clicks cannot accept the next operation. */
export function answerConfirmation(id: number, value: string | null): void {
  const prompt = pending[0]?.prompt;
  if (!prompt || prompt.id !== id) return;
  if (value !== null && !prompt.choices.some((choice) => choice.value === value)) return;
  settle(id, value);
}

/** An absent UI must never silently approve or leave an operation waiting forever. */
export function installConfirmationHost(): () => void {
  hosts++;
  let installed = true;
  return () => {
    if (!installed) return;
    installed = false;
    hosts--;
    if (hosts === 0) {
      for (const request of [...pending]) settle(request.prompt.id, null);
    }
  };
}

export function requestChoice<T extends string>(request: ChoiceRequest<T>): Promise<T | null> {
  if (!hosts || request.signal?.aborted) return Promise.resolve(null);
  const id = ++nextId;
  return new Promise<T | null>((resolve) => {
    const abort = () => settle(id, null);
    request.signal?.addEventListener('abort', abort, { once: true });
    pending.push({
      prompt: {
        id,
        title: request.title,
        message: request.message,
        choices: request.choices.map((choice) => ({ ...choice })),
      },
      resolve: (value) => resolve(value as T | null),
      cleanup: () => request.signal?.removeEventListener('abort', abort),
    });
    publish();
  });
}

export async function requestConfirmation(
  message: string,
  options: ConfirmationOptions = {}
): Promise<boolean> {
  return (
    (await requestChoice({
      title: options.title ?? 'Confirm action',
      message,
      choices: [
        {
          value: 'confirm',
          label: options.confirmLabel ?? 'Confirm',
          variant: options.variant ?? 'primary',
        },
      ],
      signal: options.signal,
    })) === 'confirm'
  );
}
