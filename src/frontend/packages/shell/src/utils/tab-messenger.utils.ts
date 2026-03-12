/**
 * Tab Messenger Utilities - Tab-scoped messaging on top of EventBus
 */

import { EventBus, type UnsubscribeFn } from '../core/services/EventBus';

export interface TabMessage<T = unknown> {
  fromTabId: string;
  toTabId?: string;
  type: string;
  payload: T;
  timestamp: number;
}

export type TabMessageHandler<T = unknown> = (message: TabMessage<T>) => void;

const TAB_EVENT_PREFIX = 'tab:message:';
const TAB_BROADCAST = `${TAB_EVENT_PREFIX}broadcast`;

function tabEvent(tabId: string): string {
  return `${TAB_EVENT_PREFIX}${tabId}`;
}

function createMessage<T>(
  fromTabId: string,
  type: string,
  payload: T,
  toTabId?: string,
): TabMessage<T> {
  return { fromTabId, toTabId, type, payload, timestamp: Date.now() };
}

/** Send a message to a specific tab */
export function sendToTab<T>(
  toTabId: string,
  type: string,
  payload: T,
  fromTabId: string,
): void {
  const bus = EventBus.getInstance();
  const message = createMessage(fromTabId, type, payload, toTabId);
  bus.emit(tabEvent(toTabId), message);
}

/** Broadcast a message to all tabs */
export function broadcastToTabs<T>(
  type: string,
  payload: T,
  fromTabId: string,
  excludeSelf = false,
): void {
  const bus = EventBus.getInstance();
  const message = createMessage(fromTabId, type, payload);
  if (excludeSelf) {
    (message as TabMessage<T> & { _excludeTabId: string })._excludeTabId = fromTabId;
  }
  bus.emit(TAB_BROADCAST, message);
}

/** Listen for messages targeted at a specific tab */
export function onTabMessage<T>(
  tabId: string,
  type: string,
  handler: TabMessageHandler<T>,
): UnsubscribeFn {
  const bus = EventBus.getInstance();
  return bus.on<TabMessage<T>>(tabEvent(tabId), (message) => {
    if (message.type === type) {
      handler(message);
    }
  });
}

/** Listen for broadcast messages */
export function onBroadcast<T>(
  type: string,
  handler: TabMessageHandler<T>,
  listenerId?: string,
): UnsubscribeFn {
  const bus = EventBus.getInstance();
  return bus.on<TabMessage<T> & { _excludeTabId?: string }>(TAB_BROADCAST, (message) => {
    if (message.type !== type) return;
    if (message._excludeTabId && message._excludeTabId === listenerId) return;
    handler(message as TabMessage<T>);
  });
}

export interface TabMessenger {
  send: <T>(toTabId: string, type: string, payload: T) => void;
  broadcast: <T>(type: string, payload: T, excludeSelf?: boolean) => void;
  listen: <T>(type: string, handler: TabMessageHandler<T>) => UnsubscribeFn;
  listenBroadcast: <T>(type: string, handler: TabMessageHandler<T>) => UnsubscribeFn;
  cleanup: () => void;
}

/** Create a scoped messenger for a specific tab */
export function createTabMessenger(tabId: string): TabMessenger {
  const unsubscribes: UnsubscribeFn[] = [];

  return {
    send<T>(toTabId: string, type: string, payload: T) {
      sendToTab(toTabId, type, payload, tabId);
    },
    broadcast<T>(type: string, payload: T, excludeSelf = false) {
      broadcastToTabs(type, payload, tabId, excludeSelf);
    },
    listen<T>(type: string, handler: TabMessageHandler<T>) {
      const unsub = onTabMessage<T>(tabId, type, handler);
      unsubscribes.push(unsub);
      return unsub;
    },
    listenBroadcast<T>(type: string, handler: TabMessageHandler<T>) {
      const unsub = onBroadcast<T>(type, handler, tabId);
      unsubscribes.push(unsub);
      return unsub;
    },
    cleanup() {
      unsubscribes.forEach((fn) => fn());
      unsubscribes.length = 0;
    },
  };
}
