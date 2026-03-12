/**
 * GlobalStateManager - Reactive Global State Container
 * Provides a subscriber-based state management pattern for use outside .svelte files
 * (Svelte 5 runes like $state and $derived only work in .svelte files)
 */

export type StateListener<T> = (newValue: T, oldValue: T) => void;
export type Unsubscriber = () => void;

/**
 * Simple reactive state store
 * Use this in TypeScript files for state management
 */
export interface Store<T> {
  /** Get current value */
  get(): T;
  /** Set new value */
  set(value: T): void;
  /** Update value using a function */
  update(fn: (current: T) => T): void;
  /** Subscribe to changes, returns unsubscribe function */
  subscribe(listener: StateListener<T>): Unsubscriber;
  /** Reset to initial value */
  reset(): void;
}

/**
 * Create a reactive state store with optional persistence
 */
export function createStore<T>(
  initialValue: T,
  options?: {
    persist?: boolean;
    key?: string;
  }
): Store<T> {
  let value = initialValue;
  const listeners = new Set<StateListener<T>>();

  // Load from localStorage if persist is enabled
  if (options?.persist && options?.key) {
    const stored = localStorage.getItem(options.key);
    if (stored !== null) {
      try {
        value = JSON.parse(stored) as T;
      } catch {
        // Invalid JSON, use initial value
      }
    }
  }

  function notify(newValue: T, oldValue: T): void {
    listeners.forEach((listener) => {
      try {
        listener(newValue, oldValue);
      } catch (error) {
        console.error('Error in state listener:', error);
      }
    });
  }

  return {
    get() {
      return value;
    },
    set(newValue: T) {
      const oldValue = value;
      if (Object.is(newValue, oldValue)) return;

      value = newValue;

      // Persist to localStorage if enabled
      if (options?.persist && options?.key) {
        localStorage.setItem(options.key, JSON.stringify(newValue));
      }

      notify(newValue, oldValue);
    },
    update(fn: (current: T) => T) {
      this.set(fn(value));
    },
    subscribe(listener: StateListener<T>): Unsubscriber {
      listeners.add(listener);
      // Immediately call with current value
      listener(value, value);
      return () => listeners.delete(listener);
    },
    reset() {
      const oldValue = value;
      value = initialValue;
      if (options?.persist && options?.key) {
        localStorage.removeItem(options.key);
      }
      notify(initialValue, oldValue);
    },
  };
}

/**
 * Global State Manager - Key-value store with subscriber support
 */
export class GlobalStateManager {
  private stores = new Map<string, Store<unknown>>();
  private static instance: GlobalStateManager | null = null;

  static getInstance(): GlobalStateManager {
    if (!GlobalStateManager.instance) {
      GlobalStateManager.instance = new GlobalStateManager();
    }
    return GlobalStateManager.instance;
  }

  static reset(): void {
    GlobalStateManager.instance = null;
  }

  /**
   * Get or create a store for a key
   */
  getStore<T>(key: string, initialValue: T): Store<T> {
    if (!this.stores.has(key)) {
      this.stores.set(key, createStore(initialValue) as Store<unknown>);
    }
    return this.stores.get(key) as Store<T>;
  }

  /**
   * Set a value directly
   */
  set<T>(key: string, value: T): void {
    if (this.stores.has(key)) {
      (this.stores.get(key) as Store<T>).set(value);
    } else {
      this.stores.set(key, createStore(value) as Store<unknown>);
    }
  }

  /**
   * Get a value
   */
  get<T>(key: string): T | undefined {
    const store = this.stores.get(key) as Store<T> | undefined;
    return store?.get();
  }

  /**
   * Check if a key exists
   */
  has(key: string): boolean {
    return this.stores.has(key);
  }

  /**
   * Delete a store
   */
  delete(key: string): boolean {
    return this.stores.delete(key);
  }

  /**
   * Clear all stores
   */
  clear(): void {
    this.stores.clear();
  }

  /**
   * Get all keys
   */
  keys(): IterableIterator<string> {
    return this.stores.keys();
  }

  /**
   * Get the number of stored values
   */
  get size(): number {
    return this.stores.size;
  }
}

export const globalState = GlobalStateManager.getInstance();
