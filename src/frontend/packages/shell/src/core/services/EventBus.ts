/**
 * EventBus - Type-safe Event Emitter
 * Provides publish/subscribe pattern with TypeScript generics
 */

export type EventHandler<T = unknown> = (payload: T) => void;
export type UnsubscribeFn = () => void;

interface EventSubscription<T> {
  handler: EventHandler<T>;
  once: boolean;
}

export class EventBus {
  private events = new Map<string, Set<EventSubscription<unknown>>>();
  private static instance: EventBus | null = null;

  /**
   * Get the singleton instance of the EventBus
   */
  static getInstance(): EventBus {
    if (!EventBus.instance) {
      EventBus.instance = new EventBus();
    }
    return EventBus.instance;
  }

  /**
   * Reset the EventBus (useful for testing)
   */
  static reset(): void {
    EventBus.instance = null;
  }

  /**
   * Subscribe to an event
   * @returns Unsubscribe function
   */
  on<T = unknown>(event: string, handler: EventHandler<T>): UnsubscribeFn {
    return this.subscribe(event, handler, false);
  }

  /**
   * Subscribe to an event (fires only once)
   * @returns Unsubscribe function
   */
  once<T = unknown>(event: string, handler: EventHandler<T>): UnsubscribeFn {
    return this.subscribe(event, handler, true);
  }

  /**
   * Emit an event with optional payload
   */
  emit<T = unknown>(event: string, payload?: T): void {
    const subscribers = this.events.get(event);

    if (!subscribers || subscribers.size === 0) {
      return;
    }

    const toRemove: EventSubscription<unknown>[] = [];

    subscribers.forEach((subscription) => {
      try {
        (subscription.handler as EventHandler<T>)(payload as T);
      } catch (error) {
        console.error(`Error in event handler for '${event}':`, error);
      }

      if (subscription.once) {
        toRemove.push(subscription);
      }
    });

    // Remove one-time subscriptions
    toRemove.forEach((subscription) => subscribers.delete(subscription));
  }

  /**
   * Remove a specific handler from an event
   */
  off<T = unknown>(event: string, handler: EventHandler<T>): void {
    const subscribers = this.events.get(event);

    if (!subscribers) {
      return;
    }

    subscribers.forEach((subscription) => {
      if (subscription.handler === handler) {
        subscribers.delete(subscription);
      }
    });
  }

  /**
   * Remove all handlers for an event
   */
  offAll(event: string): void {
    this.events.delete(event);
  }

  /**
   * Clear all event subscriptions
   */
  clear(): void {
    this.events.clear();
  }

  /**
   * Get the number of subscribers for an event
   */
  listenerCount(event: string): number {
    return this.events.get(event)?.size ?? 0;
  }

  /**
   * Get all registered event names
   */
  eventNames(): string[] {
    return Array.from(this.events.keys());
  }

  private subscribe<T>(event: string, handler: EventHandler<T>, once: boolean): UnsubscribeFn {
    if (!this.events.has(event)) {
      this.events.set(event, new Set());
    }

    const subscription: EventSubscription<T> = { handler, once };
    const subscribers = this.events.get(event)!;
    subscribers.add(subscription as EventSubscription<unknown>);

    return () => {
      subscribers.delete(subscription as EventSubscription<unknown>);
    };
  }
}

// Export a default EventBus instance
export const eventBus = EventBus.getInstance();
