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

// ============================================
// Initial State
// ============================================

const initialState: EffortState = {
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

// Create the store
const effortStore: Store<EffortState> = createStore<EffortState>(initialState);

// ============================================
// Getters (Selectors)
// ============================================

/**
 * Get current state
 */
export function getEffortState(): EffortState {
  return effortStore.get();
}

/**
 * Get selected member
 */
export function getSelectedMember(): MemberEffortSummary | undefined {
  const state = effortStore.get();
  return state.members.find((m) => m.userId === state.selectedMemberId);
}

/**
 * Check if there are conflicts
 */
export function hasConflicts(): boolean {
  return effortStore.get().conflicts.length > 0;
}

/**
 * Get working day info for a specific date
 */
export function getWorkingDayInfo(date: string): WorkingDay | undefined {
  return effortStore.get().workingDays.find((wd) => wd.date === date);
}

/**
 * Check if a date has any conflicts for the current project
 */
export function getDateConflicts(date: string): AllocationConflict[] {
  return effortStore.get().conflicts.filter((c) => c.date === date);
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
function setEffortLoading(loading: boolean): void {
  effortStore.update((state) => ({
    ...state,
    isLoading: loading,
  }));
}

/**
 * Set error
 */
function setEffortError(error: string | null): void {
  effortStore.update((state) => ({
    ...state,
    error,
    isLoading: false,
  }));
}

/**
 * Set members
 */
export function setMembers(members: MemberEffortSummary[]): void {
  effortStore.update((state) => ({
    ...state,
    members,
  }));
}

/**
 * Set ownership data
 */
export function setOwnership(ownership: MemberOwnershipPage | null): void {
  effortStore.update((state) => ({
    ...state,
    ownership,
  }));
}

/**
 * Set ownership loading state
 */
function setOwnershipLoading(loading: boolean): void {
  effortStore.update((state) => ({
    ...state,
    ownershipLoading: loading,
  }));
}

/**
 * Set ownership error
 */
function setOwnershipError(error: string | null): void {
  effortStore.update((state) => ({
    ...state,
    ownershipError: error,
  }));
}

/**
 * Set member daily effort data
 */
export function setMemberDailyEffort(dailyEffort: EffortState['memberDailyEffort']): void {
  effortStore.update((state) => ({
    ...state,
    memberDailyEffort: dailyEffort,
  }));
}

/**
 * Set week data
 */
export function setWeekData(weekData: WeekData | null): void {
  effortStore.update((state) => ({
    ...state,
    weekData,
  }));
}

/**
 * Set conflicts
 */
export function setConflicts(conflicts: AllocationConflict[]): void {
  effortStore.update((state) => ({
    ...state,
    conflicts,
  }));
}

/**
 * Set working days
 */
export function setWorkingDays(workingDays: WorkingDay[]): void {
  effortStore.update((state) => ({
    ...state,
    workingDays,
  }));
}

/**
 * Set selected member ID
 */
export function setSelectedMemberId(userId: string | null): void {
  effortStore.update((state) => ({
    ...state,
    selectedMemberId: userId,
  }));
}

/**
 * Set date range
 */
export function setDateRangeInternal(start: string, end: string): void {
  effortStore.update((state) => ({
    ...state,
    dateRange: { start, end },
  }));
}

/**
 * Set view mode
 */
export function setViewModeInternal(viewMode: EffortState['viewMode']): void {
  effortStore.update((state) => ({
    ...state,
    viewMode,
  }));
}

/**
 * Set project ID
 */
export function setProjectId(projectId: string): void {
  effortStore.update((state) => ({
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
export async function initEffort(projectId: string): Promise<void> {
  setProjectId(projectId);
  setEffortError(null);

  await loadEffortData();
}

/**
 * Load all effort data for the current date range
 */
export async function loadEffortData(): Promise<void> {
  const state = effortStore.get();
  if (!state.projectId) return;

  setEffortLoading(true);
  setEffortError(null);

  try {
    const { start, end } = state.dateRange;

    // Load data in parallel
    const [membersResult, conflictsResult, workingDaysResult] = await Promise.allSettled([
      EffortService.getMembersSummary(state.projectId, start, end),
      EffortService.getConflicts(state.projectId, start, end),
      EffortService.getWorkingDays(state.projectId, start, end),
    ]);

    if (membersResult.status === 'fulfilled') setMembers(membersResult.value);
    if (conflictsResult.status === 'fulfilled') setConflicts(conflictsResult.value);
    if (workingDaysResult.status === 'fulfilled') setWorkingDays(workingDaysResult.value);

    // If all failed, propagate error
    if (membersResult.status === 'rejected' && conflictsResult.status === 'rejected' && workingDaysResult.status === 'rejected') {
      throw membersResult.reason;
    }

    // Load selected member's daily data if one is selected
    const currentState = effortStore.get();
    if (currentState.selectedMemberId) {
      await loadMemberDailyEffort(currentState.selectedMemberId);
    }
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to load effort data');
    console.error('Failed to load effort data:', error);
  } finally {
    setEffortLoading(false);
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
export async function loadMemberDailyEffort(userId: string): Promise<void> {
  const state = effortStore.get();
  if (!state.projectId) return;

  try {
    const { start, end } = state.dateRange;
    const dailyEffort = await EffortService.getMemberDailyEffort(
      state.projectId,
      userId,
      start,
      end
    );

    setMemberDailyEffort(dailyEffort);

    if (state.viewMode === 'week') {
      setWeekData(buildWeekData(dailyEffort, start, end));
    } else {
      setWeekData(null);
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
  options?: {
    type?: OwnershipFilter;
    query?: string;
    page?: number;
    pageSize?: number;
  }
): Promise<void> {
  const state = effortStore.get();
  if (!state.projectId) return;

  setOwnershipLoading(true);
  setOwnershipError(null);

  try {
    const ownership = await EffortService.getMemberOwnership(state.projectId, userId, options);
    setOwnership(ownership);
  } catch (error) {
    setOwnership(null);
    setOwnershipError(
      error instanceof Error ? error.message : 'Failed to load owned deliverables'
    );
    console.error('Failed to load member ownership:', error);
  } finally {
    setOwnershipLoading(false);
  }
}

/**
 * Select a member to view/edit their effort
 */
export async function selectMember(userId: string | null): Promise<void> {
  setSelectedMemberId(userId);

  if (userId) {
    await Promise.allSettled([
      loadMemberDailyEffort(userId),
      loadMemberOwnership(userId, { type: 'all', query: '', page: 1, pageSize: 8 }),
    ]);
  } else {
    setMemberDailyEffort(null);
    setWeekData(null);
    setOwnership(null);
    setOwnershipError(null);
  }
}

/**
 * Set view mode and align date range
 */
export async function setViewMode(
  viewMode: EffortState['viewMode'],
  anchorDate?: string
): Promise<void> {
  const state = effortStore.get();
  if (state.viewMode === viewMode && !anchorDate) return;

  const anchor = anchorDate ? parseDate(anchorDate) : parseDate(state.dateRange.start);
  const range = viewMode === 'week'
    ? getWeekBoundaries(anchor)
    : getMonthBoundaries(anchor);

  setViewModeInternal(viewMode);
  setDateRangeInternal(range.start, range.end);
  await loadEffortData();
}

/**
 * Navigate to previous week
 */
export async function previousWeek(): Promise<void> {
  const state = effortStore.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setDate(currentStart.getDate() - 7);

  const newRange = getWeekBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Navigate to next week
 */
export async function nextWeek(): Promise<void> {
  const state = effortStore.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setDate(currentStart.getDate() + 7);

  const newRange = getWeekBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Navigate to current week
 */
export async function goToToday(): Promise<void> {
  const newRange = getWeekBoundaries();
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Navigate to previous month
 */
export async function previousMonth(): Promise<void> {
  const state = effortStore.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setMonth(currentStart.getMonth() - 1);

  const newRange = getMonthBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Navigate to next month
 */
export async function nextMonth(): Promise<void> {
  const state = effortStore.get();
  const currentStart = parseDate(state.dateRange.start);
  currentStart.setMonth(currentStart.getMonth() + 1);

  const newRange = getMonthBoundaries(currentStart);
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Navigate to current month
 */
export async function goToCurrentMonth(): Promise<void> {
  const newRange = getMonthBoundaries();
  setDateRangeInternal(newRange.start, newRange.end);
  await loadEffortData();
}

/**
 * Set date range manually
 */
export async function setDateRange(start: string, end: string): Promise<void> {
  setDateRangeInternal(start, end);
  await loadEffortData();
}

/**
 * Update allocation for a member on a specific date
 */
export async function updateAllocation(
  userId: string,
  date: string,
  allocatedHours: number
): Promise<void> {
  const state = effortStore.get();
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
    await loadEffortData();
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to update allocation');
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
  note?: string
): Promise<void> {
  const state = effortStore.get();
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
    await loadEffortData();
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to log work');
    throw error;
  }
}

/**
 * Set working day type for a date
 */
export async function setWorkingDayType(
  date: string,
  type: WorkingDay['type'],
  note?: string
): Promise<void> {
  const state = effortStore.get();
  if (!state.projectId) return;

  try {
    await EffortService.setWorkingDay(state.projectId, {
      projectId: state.projectId,
      date,
      type,
      note,
    });

    // Reload data
    await loadEffortData();
  } catch (error) {
    setEffortError(error instanceof Error ? error.message : 'Failed to set working day');
    throw error;
  }
}

/**
 * Clear error
 */
export function clearError(): void {
  setEffortError(null);
}

/**
 * Reset store to initial state
 */
export function resetEffortStore(): void {
  effortStore.reset();
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to effort state changes
 */
export function subscribeEffort(
  listener: (state: EffortState, prevState: EffortState) => void
): () => void {
  return effortStore.subscribe(listener);
}

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
