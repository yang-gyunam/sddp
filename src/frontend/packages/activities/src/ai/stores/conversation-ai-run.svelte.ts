import { untrack } from 'svelte';
import { observeConversationCompletions } from '../services/observeConversationCompletions';
import {
  ConversationRunTracker,
  type RunScope,
  type RunTrackerState,
} from '../services/ConversationRunTracker';
import { createResourceAiCapability } from './resource-ai-capability.svelte';
import { getConversationHubService } from '../../conversations/services/ConversationHubService';
import { AI_MESSAGE_TYPES, type Message } from '../../conversations/types';

type ViewScope = Omit<RunScope, 'userId'> & { readonly?: boolean };
const empty = (): RunTrackerState => ({ run: null, unavailable: false });
const terminal = new Set(['completed', 'cancelled', 'failed', 'interrupted']);

/** Owned by one conversation view; never persists prompts or answers in browser storage. */
export function createConversationAiRun(
  scope: () => ViewScope,
  reloadMessages: (
    isCurrent: () => boolean,
    passiveOutputId?: string
  ) => Promise<readonly { id: string }[]>
) {
  const capability = createResourceAiCapability(scope);
  const key = $derived.by(() => {
    const value = scope();
    const actor = capability.state.context;
    return actor && value.projectId && value.containerId
      ? [
          actor.userId,
          value.tenantId,
          value.projectId,
          value.containerType,
          value.containerId,
        ].join(':')
      : '';
  });
  let owner = $state('');
  let state = $state<RunTrackerState>(empty());
  let submitting = $state(false);
  let restoring = $state(false);
  let discoveryOwner = $state('');
  let deliveryError = $state(false);
  let delivering = $state(false);
  let tracker: ConversationRunTracker | null = null;
  let request: { requestId: string; inputMessageId: string } | null = null;
  let knownRunId: string | null = null;
  let deliveredRun: string | null = null;
  let generation = 0;
  const visible = $derived(owner === key && !!key);
  const active = $derived(!!state.run && !terminal.has(state.run.status));
  const allowed = $derived(
    visible && !scope().readonly && capability.view('ai.conversation').canExecute
  );

  function clear() {
    generation++;
    tracker?.dispose();
    tracker = null;
    request = null;
    knownRunId = null;
    deliveredRun = null;
    state = empty();
    submitting = false;
    restoring = false;
    deliveryError = false;
    delivering = false;
  }
  $effect(() => {
    const next = key;
    untrack(() => {
      clear();
      owner = next;
      discoveryOwner = '';
    });
    const unsubscribe = next
      ? observeConversationCompletions(
          getConversationHubService(),
          () => knownRunId,
          (current, outputId) => reloadMessages(() => current() && owner === key && !!key, outputId)
        )
      : () => {};
    return () => {
      unsubscribe();
      clear();
      owner = '';
    };
  });

  $effect(() => {
    const current = key;
    const executable = capability.view('ai.conversation').canExecute;
    if (current && executable)
      untrack(() => {
        if (owner === current && discoveryOwner !== current) {
          discoveryOwner = current;
          void discover();
        }
      });
  });

  async function discover() {
    const epoch = generation;
    tracker?.dispose();
    attachTracker(epoch);
    restoring = true;
    try {
      await tracker!.discover();
    } finally {
      if (epoch === generation) restoring = false;
    }
  }

  async function deliver(runId: string, outputMessageId: string, epoch: number) {
    if (delivering || deliveredRun === runId) return;
    delivering = true;
    deliveryError = false;
    const isCurrent = () => epoch === generation && owner === key && !!key;
    try {
      const messages = await reloadMessages(isCurrent);
      if (!isCurrent()) return;
      if (!messages.some((message) => message.id === outputMessageId)) {
        deliveryError = true;
        return;
      }
      deliveredRun = runId;
    } catch {
      if (isCurrent()) deliveryError = true;
    } finally {
      if (isCurrent()) delivering = false;
    }
  }

  function attachTracker(epoch: number) {
    const value = scope();
    tracker = new ConversationRunTracker(
      { ...value, userId: capability.state.context!.userId },
      getConversationHubService(),
      (next) => {
        if (epoch !== generation || owner !== key || !key) return;
        state = next;
        if (next.run) {
          knownRunId = next.run.id;
          request ??= { requestId: crypto.randomUUID(), inputMessageId: next.run.inputMessageId };
        }
        if (next.run?.status === 'completed' && next.run.outputMessageId)
          void deliver(next.run.id, next.run.outputMessageId, epoch);
      }
    );
  }

  async function start(inputMessageId: string) {
    clear();
    const epoch = generation;
    request = { requestId: crypto.randomUUID(), inputMessageId };
    attachTracker(epoch);
    submitting = true;
    try {
      await tracker!.create(request.requestId, inputMessageId);
    } finally {
      if (epoch === generation) submitting = false;
    }
  }

  return {
    get view() {
      return capability.view('ai.conversation');
    },
    get state() {
      return visible ? state : empty();
    },
    get restoring() {
      return visible && restoring;
    },
    get submitting() {
      return visible && submitting;
    },
    get delivering() {
      return visible && delivering;
    },
    get deliveryError() {
      return visible && deliveryError;
    },
    get canAsk() {
      return (
        allowed &&
        discoveryOwner === key &&
        !restoring &&
        !submitting &&
        !active &&
        !state.unavailable &&
        !delivering &&
        !deliveryError
      );
    },
    get canRetry() {
      return (
        allowed && !!state.run && ['failed', 'cancelled', 'interrupted'].includes(state.run.status)
      );
    },
    get canCancel() {
      return (
        visible && !submitting && !!state.run && ['queued', 'running'].includes(state.run.status)
      );
    },
    refreshAvailability: capability.refresh,
    async ask(message: Message) {
      if (
        !allowed ||
        restoring ||
        discoveryOwner !== key ||
        submitting ||
        active ||
        delivering ||
        deliveryError ||
        state.unavailable ||
        AI_MESSAGE_TYPES.includes(message.type)
      )
        return;
      await start(message.id);
    },
    async retry() {
      if (
        allowed &&
        request &&
        state.run &&
        ['failed', 'cancelled', 'interrupted'].includes(state.run.status)
      )
        await start(request.inputMessageId);
    },
    async refresh() {
      if (!visible || !tracker || submitting) return;
      if (deliveryError && state.run?.status === 'completed' && state.run.outputMessageId) {
        await deliver(state.run.id, state.run.outputMessageId, generation);
        return;
      }
      if (!knownRunId && !request) {
        await discover();
        return;
      }
      if (knownRunId) await tracker.refresh();
      else if (allowed && request) {
        const epoch = generation;
        submitting = true;
        try {
          await tracker.create(request.requestId, request.inputMessageId);
        } finally {
          if (epoch === generation) submitting = false;
        }
      }
    },
    dismissDeliveryError() {
      if (visible && deliveryError && !delivering && state.run?.status === 'completed') clear();
    },
    async cancel() {
      if (visible && tracker) await tracker.cancel();
    },
  };
}

export type ConversationAiRun = ReturnType<typeof createConversationAiRun>;
