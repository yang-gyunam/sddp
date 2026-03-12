/**
 * Timeline Store - Timeline Events State Management
 * Manages timeline events, filters, and expanded state
 */

import { createStore, type Store } from '@sddp/shell/core';
import type { TimelineEvent, TimelineState, TimelineFilter, TimelineEventType } from '../types';

// ============================================
// Initial State
// ============================================

const initialState: TimelineState = {
  events: [],
  loading: false,
  error: null,
  filter: { period: 'all' },
  expanded: false,
};

// Create the store
const timelineStore: Store<TimelineState> = createStore<TimelineState>(initialState);

// ============================================
// Events Actions
// ============================================

/**
 * Set timeline events
 */
export function setTimelineEvents(events: TimelineEvent[]): void {
  timelineStore.update((state) => ({
    ...state,
    events,
    loading: false,
    error: null,
  }));
}

/**
 * Set timeline loading state
 */
export function setTimelineLoading(loading: boolean): void {
  timelineStore.update((state) => ({
    ...state,
    loading,
  }));
}

/**
 * Set timeline error
 */
export function setTimelineError(error: string | null): void {
  timelineStore.update((state) => ({
    ...state,
    error,
    loading: false,
  }));
}

/**
 * Add a new event to the timeline (prepend)
 */
export function addTimelineEvent(event: TimelineEvent): void {
  timelineStore.update((state) => {
    // Prevent duplicate events
    if (state.events.some((e) => e.id === event.id)) {
      return state;
    }
    return {
      ...state,
      events: [event, ...state.events],
    };
  });
}

/**
 * Remove an event from the timeline
 */
export function removeTimelineEvent(eventId: string): void {
  timelineStore.update((state) => ({
    ...state,
    events: state.events.filter((e) => e.id !== eventId),
  }));
}

/**
 * Clear all timeline events
 */
export function clearTimelineEvents(): void {
  timelineStore.update((state) => ({
    ...state,
    events: [],
  }));
}

// ============================================
// Filter Actions
// ============================================

/**
 * Set timeline filter
 */
export function setTimelineFilter(filter: TimelineFilter): void {
  timelineStore.update((state) => ({
    ...state,
    filter,
  }));
}

/**
 * Set filter period
 */
export function setFilterPeriod(period: TimelineFilter['period']): void {
  timelineStore.update((state) => ({
    ...state,
    filter: { ...state.filter, period },
  }));
}

/**
 * Set filter event types
 */
export function setFilterEventTypes(types: TimelineEventType[] | undefined): void {
  timelineStore.update((state) => ({
    ...state,
    filter: { ...state.filter, types },
  }));
}

/**
 * Clear timeline filter
 */
export function clearTimelineFilter(): void {
  timelineStore.update((state) => ({
    ...state,
    filter: { period: 'all' },
  }));
}

// ============================================
// Expanded State Actions
// ============================================

/**
 * Set timeline expanded state
 */
export function setTimelineExpanded(expanded: boolean): void {
  timelineStore.update((state) => ({
    ...state,
    expanded,
  }));
}

/**
 * Toggle timeline expanded state
 */
export function toggleTimelineExpanded(): void {
  timelineStore.update((state) => ({
    ...state,
    expanded: !state.expanded,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getTimelineState(): TimelineState {
  return timelineStore.get();
}

/**
 * Get timeline events
 */
export function getTimelineEvents(): TimelineEvent[] {
  return timelineStore.get().events;
}

/**
 * Get filtered events based on current filter
 */
export function getFilteredEvents(): TimelineEvent[] {
  const state = timelineStore.get();
  let events = state.events;

  // Filter by event types
  if (state.filter.types && state.filter.types.length > 0) {
    events = events.filter((e) => state.filter.types!.includes(e.type));
  }

  // Filter by entity types
  if (state.filter.entityTypes && state.filter.entityTypes.length > 0) {
    events = events.filter((e) => state.filter.entityTypes!.includes(e.entityType));
  }

  // Filter by actor IDs
  if (state.filter.actorIds && state.filter.actorIds.length > 0) {
    events = events.filter((e) => state.filter.actorIds!.includes(e.actorId));
  }

  // Filter by period
  const now = new Date();
  let cutoffTime: Date;

  switch (state.filter.period) {
    case 'minutes-10':
      cutoffTime = new Date(now.getTime() - 10 * 60 * 1000);
      break;
    case 'hour-1':
      cutoffTime = new Date(now.getTime() - 60 * 60 * 1000);
      break;
    case 'today':
      cutoffTime = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      break;
    case 'week':
      cutoffTime = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
      break;
    default:
      cutoffTime = new Date(0);
  }

  events = events.filter((e) => new Date(e.timestamp) >= cutoffTime);

  return events;
}

/**
 * Get timeline filter
 */
export function getTimelineFilter(): TimelineFilter {
  return timelineStore.get().filter;
}

/**
 * Get timeline expanded state
 */
export function isTimelineExpanded(): boolean {
  return timelineStore.get().expanded;
}

/**
 * Get events for a specific project
 */
export function getProjectEvents(projectId: string): TimelineEvent[] {
  return timelineStore.get().events.filter((e) => e.projectId === projectId);
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to timeline state changes
 */
export function subscribeTimeline(
  listener: (state: TimelineState, prevState: TimelineState) => void
): () => void {
  return timelineStore.subscribe(listener);
}

/**
 * Reset timeline store
 */
export function resetTimelineStore(): void {
  timelineStore.reset();
}

// Export the store for direct access
export { timelineStore };

// Re-export types for convenience
export type { TimelineState } from '../types';
