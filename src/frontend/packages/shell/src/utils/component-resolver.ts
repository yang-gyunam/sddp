/**
 * Component Resolver Utility
 * Handles dynamic component resolution for tabs
 */

import type { Tab } from '../types/layout.types';

export interface ComponentResolutionResult {
  component: unknown | null;
  error: Error | null;
}

/**
 * Determines the type of component reference
 */
export function getComponentType(
  component: Tab['component']
): 'none' | 'function' | 'promise' | 'module' | 'unknown' {
  if (!component) {
    return 'none';
  }

  if (typeof component === 'function') {
    return 'function';
  }

  if (component instanceof Promise) {
    return 'promise';
  }

  if (typeof component === 'object' && 'default' in component) {
    return 'module';
  }

  return 'unknown';
}

/**
 * Synchronously resolves a component if possible
 */
export function resolveComponentSync(
  component: Tab['component']
): ComponentResolutionResult {
  if (!component) {
    return { component: null, error: null };
  }

  if (typeof component === 'function') {
    return { component, error: null };
  }

  if (typeof component === 'object' && 'default' in component) {
    return { component: (component as { default: unknown }).default, error: null };
  }

  // Promise or unknown - cannot resolve synchronously
  return { component: null, error: null };
}

/**
 * Asynchronously resolves a component
 */
export async function resolveComponentAsync(
  component: Tab['component']
): Promise<ComponentResolutionResult> {
  try {
    if (!component) {
      return { component: null, error: null };
    }

    if (typeof component === 'function') {
      return { component, error: null };
    }

    if (component instanceof Promise) {
      const module = await component;
      const resolved =
        typeof module === 'object' && module !== null && 'default' in module
          ? (module as { default: unknown }).default
          : module;
      return { component: resolved, error: null };
    }

    if (typeof component === 'object' && 'default' in component) {
      return { component: (component as { default: unknown }).default, error: null };
    }

    // Unknown type
    return { component: null, error: null };
  } catch (error) {
    return { component: null, error: error as Error };
  }
}

/**
 * Checks if a tab needs async loading
 */
export function needsAsyncLoad(component: Tab['component']): boolean {
  return component instanceof Promise;
}
