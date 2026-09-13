/**
 * Effort Store
 * State management for project effort tracking using createStore pattern
 */

import { createStore, type Store } from '@sddp/shell/core';
import { EffortService, getWeekBoundaries, getMonthBoundaries, parseDate } from '../services';
import type {
  EffortState,
  MemberEffortSummary,
  MemberOwnershipPage,
  OwnershipFilter,
  DailyEffort,
  WeekData,
  AllocationConflict,
  WorkingDay,
  EffortAllocationDto,
  WorklogDto,
} from '../types';

export interface MemberOwnershipOptions {
  type?: OwnershipFilter;
  query?: string;
  page?: number;
  pageSize?: number;
}

// ============================================
// Initial State
// ============================================

function createInitialState(): EffortState {
  return {
    projectId: '',
    selectedMemberId: null,
    dateRange: getWeekBoundaries(),
    viewMode: 'week',
    members: [],
    ownership: null,
    ownershipLoading: false,
    ownershipError: null,
    memberDailyEffort: null,
    weekData: null,
    conflicts: [],
    workingDays: [],
    isLoading: false,
    error: null,
  };
}

/** Create an isolated store for a single Effort editor tab. */
export function createEffortStoreInstance(): Store<EffortState> {
  return createStore<EffortState>(createInitialState());
}

// Backward-compatible application store. Editor tabs use createEffortStoreScope().
const effortStore: Store<EffortState> = createEffortStoreInstance();

// ============================================
// Getters (Selectors)
// ============================================

/**
 * Get current state
 */
export function getEffortState(target: Store<EffortState> = effortStore): EffortState {
  return target.get();
}

/**
 * Get selected member
 */
export function getSelectedMember(
  target: Store<EffortState> = effortStore
): MemberEffortSummary | undefined {
  const state = target.get();
  return state.members.find((m) => m.userId === state.selectedMemberId);
}

/**
 * Check if there are conflicts
 */
export function hasConflicts(target: Store<EffortState> = effortStore): boolean {
  return target.get().conflicts.length > 0;
}

/**
 * Get working day info for a specific date
 */
export function getWorkingDayInfo(
  date: string,
  target: Store<EffortState> = effortStore
): WorkingDay | undefined {
  return target.get().workingDays.find((wd) => wd.date === date);
}

/**
 * Check if a date has any conflicts for the current project
 */
export function getDateConflicts(
  date: string,
  target: Store<EffortState> = effortStore
): AllocationConflict[] {
  return target.get().conflicts.filter((c) => c.date === date);
}

// ============================================
// Reactive State Proxy (for Svelte components)
// ============================================

/**
 * Reactive state accessor for Svelte components
 * This creates a proxy that components can use reactively
 */
export const effortState = {
  get projectId() {
    return effortStore.get().projectId;
  },
  get selectedMemberId() {
    return effortStore.get().selectedMemberId;
  },
  get dateRange() {
    return effortStore.get().dateRange;
  },
  get viewMode() {
    return effortStore.get().viewMode;
  },
  get members() {
    return effortStore.get().members;
  },
  get ownership() {
    return effortStore.get().ownership;
  },
  get ownershipLoading() {
    return effortStore.get().ownershipLoading;
  },
  get ownershipError() {
    return effortStore.get().ownershipError;
  },
  get memberDailyEffort() {
    return effortStore.get().memberDailyEffort;
  },
  get weekData() {
    return effortStore.get().weekData;
  },
  get conflicts() {
    return effortStore.get().conflicts;
  },
  get workingDays() {
    return effortStore.get().workingDays;
  },
  get isLoading() {
    return effortStore.get().isLoading;
  },
  get error() {
    return effortStore.get().error;
  },
  get selectedMember(): MemberEffortSummary | undefined {
    return getSelectedMember();
  },
  get hasConflicts(): boolean {
    return hasConflicts();
  },
};

// ============================================
// Actions
// ============================================

/**
 * Set loading state
 */
function setEffortLoading(loading: boolean, target: Store<EffortState> = effortStore): void {
  target.update((state) => ({
    ...state,
    isLoading: loading,
  }));
}

/**
 * Set error
 */
function setEffortError(error: string | null, target: Store<EffortState> = effortStore): void {
  target.update((state) => ({
    ...state,
    error,
    isLoading: false,
  }));
}

/**
 * Set members
 */
export function setMembers(
  members: MemberEffortSummary[],
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    members,
  }));
}

/**
 * Set ownership data
 */
export function setOwnership(
  ownership: MemberOwnershipPage | null,
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    ownership,
  }));
}

/**
 * Set ownership loading state
 */
function setOwnershipLoading(loading: boolean, target: Store<EffortState> = effortStore): void {
  target.update((state) => ({
    ...state,
    ownershipLoading: loading,
  }));
}

/**
 * Set ownership error
 */
function setOwnershipError(error: string | null, target: Store<EffortState> = effortStore): void {
  target.update((state) => ({
    ...state,
    ownershipError: error,
  }));
}

/**
 * Set member daily effort data
 */
export function setMemberDailyEffort(
  dailyEffort: EffortState['memberDailyEffort'],
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    memberDailyEffort: dailyEffort,
  }));
}

/**
 * Set week data
 */
export function setWeekData(
  weekData: WeekData | null,
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    weekData,
  }));
}

/**
 * Set conflicts
 */
export function setConflicts(
  conflicts: AllocationConflict[],
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    conflicts,
  }));
}

/**
 * Set working days
 */
export function setWorkingDays(
  workingDays: WorkingDay[],
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    workingDays,
  }));
}

/**
 * Set selected member ID
 */
export function setSelectedMemberId(
  userId: string | null,
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    selectedMemberId: userId,
  }));
}

/**
 * Set date range
 */
export function setDateRangeInternal(
  start: string,
  end: string,
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    dateRange: { start, end },
  }));
}

/**
 * Set view mode
 */
export function setViewModeInternal(
  viewMode: EffortState['viewMode'],
  target: Store<EffortState> = effortStore
): void {
  target.update((state) => ({
    ...state,
    viewMode,
  }));
}

/**
 * Set project ID
 */
export function setProjectId(projectId: string, target: Store<EffortState> = effortStore): void {
  target.update((state) => ({
    ...state,
    projectId,
  }));
}

// ============================================
// Async Actions (orchestration)
// ============================================

/**
 * Initialize the effort store for a project
 */
export async function initEffort(
  projectId: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  setProjectId(projectId, target);
  setEffortError(null, target);

  await loadEffortData(target);
}

/**
 * Load all effort data for the current date range
 */
export async function loadEffortData(target: Store<EffortState> = effortStore): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  setEffortLoading(true, target);
  setEffortError(null, target);

  try {
    const { start, end } = state.dateRange;

    // Load data in parallel
    const [membersResult, conflictsResult, workingDaysResult] = await Promise.allSettled([
      EffortService.getMembersSummary(state.projectId, start, end),
      EffortService.getConflicts(state.projectId, start, end),
      EffortService.getWorkingDays(state.projectId, start, end),
    ]);

    if (membersResult.status === 'fulfilled') setMembers(membersResult.value, target);
    if (conflictsResult.status === 'fulfilled') setConflicts(conflictsResult.value, target);
    if (workingDaysResult.status === 'fulfilled') setWorkingDays(workingDaysResult.value, target);

    // If all failed, propagate error
    if (
      membersResult.status === 'rejected' &&
      conflictsResult.status === 'rejected' &&
      workingDaysResult.status === 'rejected'
    ) {
      throw membersResult.reason;
    }

    // Load selected member's daily data if one is selected
    const currentState = target.get();
    if (currentState.selectedMemberId) {
      await loadMemberDailyEffort(currentState.selectedMemberId, target);
    }
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to load effort data', target);
    console.error('Failed to load effort data:', error);
  } finally {
    setEffortLoading(false, target);
  }
}

/**
 * Build week data for a specific date range
 */
function buildWeekData(dailyEffort: DailyEffort[], start: string, end: string): WeekData {
  const days = dailyEffort
    .filter((d) => d.date >= start && d.date <= end)
    .sort((a, b) => a.date.localeCompare(b.date));

  const totalAllocated = days.reduce((sum, d) => sum + d.allocatedHours, 0);
  const totalSpent = days.reduce((sum, d) => sum + d.spentHours, 0);

  return {
    weekStart: start,
    weekEnd: end,
    days,
    totalAllocated,
    totalSpent,
  };
}

/**
 * Load daily effort data for a specific member
 */
export async function loadMemberDailyEffort(
  userId: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  try {
    const { start, end } = state.dateRange;
    const dailyEffort = await EffortService.getMemberDailyEffort(
      state.projectId,
      userId,
      start,
      end
    );

    setMemberDailyEffort(dailyEffort, target);

    if (state.viewMode === 'week') {
      setWeekData(buildWeekData(dailyEffort, start, end), target);
    } else {
      setWeekData(null, target);
    }
  } catch (error) {
    console.error('Failed to load member daily effort:', error);
  }
}

/**
 * Load owned deliverables for a specific member
 */
export async function loadMemberOwnership(
  userId: string,
  options?: MemberOwnershipOptions,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  setOwnershipLoading(true, target);
  setOwnershipError(null, target);

  try {
    const ownership = await EffortService.getMemberOwnership(state.projectId, userId, options);
    setOwnership(ownership, target);
  } catch (error) {
    setOwnership(null, target);
    setOwnershipError(
      error instanceof Error ? error.message : 'Failed to load owned deliverables',
      target
    );
    console.error('Failed to load member ownership:', error);
  } finally {
    setOwnershipLoading(false, target);
  }
}

/**
 * Select a member to view/edit their effort
 */
export async function selectMember(
  userId: string | null,
  target: Store<EffortState> = effortStore
): Promise<void> {
  setSelectedMemberId(userId, target);

  if (userId) {
    await Promise.allSettled([
      loadMemberDailyEffort(userId, target),
      loadMemberOwnership(userId, { type: 'all', query: '', page: 1, pageSize: 8 }, target),
    ]);
  } else {
    setMemberDailyEffort(null, target);
    setWeekData(null, target);
    setOwnership(null, target);
    setOwnershipError(null, target);
  }
}

/**
 * Set view mode and align date range
 */
export async function setViewMode(
  viewMode: EffortState['viewMode'],
  anchorDate?: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (state.viewMode === viewMode && !anchorDate) return;

  const anchor = anchorDate ? parseDate(anchorDate) : parseDate(state.dateRange.start);
  const range = viewMode === 'week' ? getWeekBoundaries(anchor) : getMonthBoundaries(anchor);

  setViewModeInternal(viewMode, target);
  setDateRangeInternal(range.start, range.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to previous week
 */
export async function previousWeek(target: Store<EffortState> = effortStore): Promise<void> {
  const state = target.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setDate(currentStart.getDate() - 7);

  const newRange = getWeekBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to next week
 */
export async function nextWeek(target: Store<EffortState> = effortStore): Promise<void> {
  const state = target.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setDate(currentStart.getDate() + 7);

  const newRange = getWeekBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to current week
 */
export async function goToToday(target: Store<EffortState> = effortStore): Promise<void> {
  const newRange = getWeekBoundaries();
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to previous month
 */
export async function previousMonth(target: Store<EffortState> = effortStore): Promise<void> {
  const state = target.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setMonth(currentStart.getMonth() - 1);

  const newRange = getMonthBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to next month
 */
export async function nextMonth(target: Store<EffortState> = effortStore): Promise<void> {
  const state = target.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setMonth(currentStart.getMonth() + 1);

  const newRange = getMonthBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Navigate to current month
 */
export async function goToCurrentMonth(target: Store<EffortState> = effortStore): Promise<void> {
  const newRange = getMonthBoundaries();
  setDateRangeInternal(newRange.start, newRange.end, target);
  await loadEffortData(target);
}

/**
 * Set date range manually
 */
export async function setDateRange(
  start: string,
  end: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  setDateRangeInternal(start, end, target);
  await loadEffortData(target);
}

/**
 * Update allocation for a member on a specific date
 */
export async function updateAllocation(
  userId: string,
  date: string,
  allocatedHours: number,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  try {
    const dto: EffortAllocationDto = {
      userId,
      projectId: state.projectId,
      date,
      allocatedHours,
    };

    await EffortService.upsertAllocation(state.projectId, dto);

    // Reload data to reflect changes
    await loadEffortData(target);
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to update allocation', target);
    throw error;
  }
}

/**
 * Log work hours for a member
 */
export async function logWork(
  userId: string,
  date: string,
  spentHours: number,
  note?: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  try {
    const dto: WorklogDto = {
      userId,
      projectId: state.projectId,
      date,
      spentHours,
      note,
    };

    await EffortService.createWorklog(state.projectId, dto);

    // Reload data to reflect changes
    await loadEffortData(target);
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to log work', target);
    throw error;
  }
}

/**
 * Set working day type for a date
 */
export async function setWorkingDayType(
  date: string,
  type: WorkingDay['type'],
  note?: string,
  target: Store<EffortState> = effortStore
): Promise<void> {
  const state = target.get();
  if (!state.projectId) return;

  try {
    await EffortService.setWorkingDay(state.projectId, {
      projectId: state.projectId,
      date,
      type,
      note,
    });

    // Reload data
    await loadEffortData(target);
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to set working day', target);
    throw error;
  }
}

/**
 * Clear error
 */
export function clearError(target: Store<EffortState> = effortStore): void {
  setEffortError(null, target);
}

/**
 * Reset store to initial state
 */
export function resetEffortStore(target: Store<EffortState> = effortStore): void {
  target.reset();
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to effort state changes
 */
export function subscribeEffort(
  listener: (state: EffortState, prevState: EffortState) => void,
  target: Store<EffortState> = effortStore
): () => void {
  return target.subscribe(listener);
}

/**
 * Actions bound to one store instance. Each Split Editor tab creates its own scope.
 */
export function createEffortStoreScope() {
  const store = createEffortStoreInstance();

  return {
    getState: () => getEffortState(store),
    getSelectedMember: () => getSelectedMember(store),
    hasConflicts: () => hasConflicts(store),
    getWorkingDayInfo: (date: string) => getWorkingDayInfo(date, store),
    getDateConflicts: (date: string) => getDateConflicts(date, store),
    subscribe: (listener: (state: EffortState, prevState: EffortState) => void) =>
      subscribeEffort(listener, store),
    reset: () => resetEffortStore(store),
    setMembers: (members: MemberEffortSummary[]) => setMembers(members, store),
    setOwnership: (ownership: MemberOwnershipPage | null) => setOwnership(ownership, store),
    setMemberDailyEffort: (dailyEffort: EffortState['memberDailyEffort']) =>
      setMemberDailyEffort(dailyEffort, store),
    setWeekData: (weekData: WeekData | null) => setWeekData(weekData, store),
    setConflicts: (conflicts: AllocationConflict[]) => setConflicts(conflicts, store),
    setWorkingDays: (workingDays: WorkingDay[]) => setWorkingDays(workingDays, store),
    setSelectedMemberId: (userId: string | null) => setSelectedMemberId(userId, store),
    setDateRangeInternal: (start: string, end: string) => setDateRangeInternal(start, end, store),
    setViewModeInternal: (viewMode: EffortState['viewMode']) =>
      setViewModeInternal(viewMode, store),
    setProjectId: (projectId: string) => setProjectId(projectId, store),
    initEffort: (projectId: string) => initEffort(projectId, store),
    loadEffortData: () => loadEffortData(store),
    loadMemberDailyEffort: (userId: string) => loadMemberDailyEffort(userId, store),
    loadMemberOwnership: (userId: string, options?: MemberOwnershipOptions) =>
      loadMemberOwnership(userId, options, store),
    selectMember: (userId: string | null) => selectMember(userId, store),
    setViewMode: (viewMode: EffortState['viewMode'], anchorDate?: string) =>
      setViewMode(viewMode, anchorDate, store),
    previousWeek: () => previousWeek(store),
    nextWeek: () => nextWeek(store),
    goToToday: () => goToToday(store),
    previousMonth: () => previousMonth(store),
    nextMonth: () => nextMonth(store),
    goToCurrentMonth: () => goToCurrentMonth(store),
    setDateRange: (start: string, end: string) => setDateRange(start, end, store),
    updateAllocation: (userId: string, date: string, allocatedHours: number) =>
      updateAllocation(userId, date, allocatedHours, store),
    logWork: (userId: string, date: string, spentHours: number, note?: string) =>
      logWork(userId, date, spentHours, note, store),
    setWorkingDayType: (date: string, type: WorkingDay['type'], note?: string) =>
      setWorkingDayType(date, type, note, store),
    clearError: () => clearError(store),
  };
}

export type EffortStoreScope = ReturnType<typeof createEffortStoreScope>;

// ============================================
// Helper Functions
// ============================================

/**
 * Format hours for display (e.g., "8h" or "4.5h")
 */
export function formatHours(hours: number): string {
  if (hours === 0) return '-';
  if (Number.isInteger(hours)) return `${hours}h`;
  return `${hours.toFixed(1)}h`;
}

/**
 * Utilization grade thresholds and colors
 * Used by HoursBar, RadialProgress, and other effort visualizations
 */
export type UtilizationGrade = 'excellent' | 'good' | 'average' | 'poor';

export interface UtilizationResult {
  grade: UtilizationGrade;
  variant: 'primary' | 'success' | 'warning' | 'danger';
  color: string;
}

/**
 * Calculate utilization grade based on percentage (spent/allocated * 100)
 * - ≥100%: excellent (blue/primary)
 * - ≥80%: good (green/success)
 * - ≥50%: average (yellow/warning)
 * - <50%: poor (red/danger)
 */
export function getUtilizationGrade(rate: number): UtilizationResult {
  if (rate >= 100) {
    return { grade: 'excellent', variant: 'primary', color: 'var(--color-accent-primary)' };
  }
  if (rate >= 80) {
    return { grade: 'good', variant: 'success', color: 'var(--color-success-500)' };
  }
  if (rate >= 50) {
    return { grade: 'average', variant: 'warning', color: 'var(--color-warning-500)' };
  }
  return { grade: 'poor', variant: 'danger', color: 'var(--color-danger-500)' };
}

/**
 * Calculate utilization rate from spent and allocated hours
 */
export function calculateUtilization(spent: number, allocated: number): number {
  if (allocated <= 0) return 0;
  return (spent / allocated) * 100;
}

/**
 * Format week label for display (e.g., "1/20 - 1/26")
 */
export function formatWeekLabel(start: string, end: string): string {
  const startDate = parseDate(start);
  const endDate = parseDate(end);

  const startStr = `${startDate.getMonth() + 1}/${startDate.getDate()}`;
  const endStr = `${endDate.getMonth() + 1}/${endDate.getDate()}`;

  return `${startStr} - ${endStr}`;
}

// Export the store for direct access
export { effortStore };

// Re-export types for convenience
export type { EffortState } from '../types';
