<!-- Activity: Projects > Nav: Effort (project-{id}-effort) | Screen ID: PRJ-EFF-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteDate } from 'svelte/reactivity';
  import { IconButton, Button, Icon, CardGrid } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, EmptyState, SidebarDetailLayout, SurfaceCard, getTabState, setTabState, formatNumber, formatPercent, formatMonthShort, toast, Modal } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import type { ProjectDetail } from '../../types';
  import {
    effortStore,
    initEffort,
    selectMember,
    resetEffortStore,
    subscribeEffort,
    previousWeek,
    nextWeek,
    goToToday,
    previousMonth,
    nextMonth,
    goToCurrentMonth,
    setDateRange,
    setViewMode,
    formatWeekLabel,
    type EffortState,
  } from '../../../effort/stores';
  import { isWeekend, getMemberDailyEffort, formatDate } from '../../../effort/services';
  import type { DailyEffort, HeatmapWeek, MemberEffortSummary, WeekData } from '../../../effort/types';
  import { ConflictWarnings, ActivityHeatmap } from '../../../effort/components/idioms';
  import {
    EffortSidebar,
    WeeklyTimesheet,
    MonthlyTimesheet,
    MemberProductionSummary,
    MemberOwnershipPanel,
  } from '../../../effort/components/sections';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Effort';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // Reactive state from store
  let members = $state<MemberEffortSummary[]>(effortStore.get().members);
  let ownership = $state(effortStore.get().ownership);
  let ownershipLoading = $state(effortStore.get().ownershipLoading);
  let ownershipError = $state(effortStore.get().ownershipError);
  let viewMode = $state(effortStore.get().viewMode);
  let memberDailyEffort = $state(effortStore.get().memberDailyEffort);
  let selectedMemberId = $state(effortStore.get().selectedMemberId);
  let weekData = $state(effortStore.get().weekData);
  let conflicts = $state(effortStore.get().conflicts);
  let isLoading = $state(effortStore.get().isLoading);
  let error = $state(effortStore.get().error);
  let storeProjectId = $state(effortStore.get().projectId);
  let dateRange = $state(effortStore.get().dateRange);

  interface ProjectEffortTabState {
    selectedMemberId: string | null;
    dateRange: { start: string; end: string };
    viewMode?: 'week' | 'month';
    selectedWeekStart?: string | null;
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-effort`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);
  let pendingRestore = $state<ProjectEffortTabState | null>(null);
  let selectedWeekRange = $state<{ start: string; end: string } | null>(null);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    pendingRestore = getTabState<ProjectEffortTabState>(tabStateKey) ?? null;
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectEffortTabState = {
      selectedMemberId,
      dateRange: { ...dateRange },
      viewMode,
      selectedWeekStart: selectedWeekRange?.start ?? null,
    };
    setTabState<ProjectEffortTabState>(tabStateKey, state);
  });

  // Derived values
  const selectedMember = $derived(members.find((m) => m.userId === selectedMemberId));
  const hasConflicts = $derived(conflicts.length > 0);
  const weekLabel = $derived(formatWeekLabel(dateRange.start, dateRange.end));
  const monthLabel = $derived.by(() => {
    const d = new SvelteDate(dateRange.start);
    return `${formatMonthShort(d)} ${d.getFullYear()}`;
  });
  const periodLabel = $derived(viewMode === 'week' ? weekLabel : monthLabel);
  const selectedWeekLabel = $derived(
    selectedWeekRange ? formatWeekLabel(selectedWeekRange.start, selectedWeekRange.end) : ''
  );
  const totalAllocated = $derived(members.reduce((sum, member) => sum + member.totalAllocated, 0));
  const totalSpent = $derived(members.reduce((sum, member) => sum + member.totalSpent, 0));
  const utilizationRate = $derived(totalAllocated > 0 ? totalSpent / totalAllocated : 0);

  const activeWeekData = $derived.by(() => {
    if (viewMode === 'week') return weekData;
    if (!memberDailyEffort || !selectedWeekRange) return null;
    return buildWeekDataFromDaily(memberDailyEffort, selectedWeekRange.start, selectedWeekRange.end);
  });

  $effect(() => {
    if (viewMode !== 'month') return;
    const start = dateRange?.start;
    const end = dateRange?.end;
    if (!start) return;
    untrack(() => {
      if (!selectedWeekRange || selectedWeekRange.start < start || selectedWeekRange.start > end) {
        selectedWeekRange = getWeekRangeFromDate(start);
      }
    });
  });

  // Heatmap data from real API
  let heatmapData = $state<HeatmapWeek[]>([]);
  let prevHeatmapMemberId = $state<string | null>(null);

  $effect(() => {
    if (!selectedMember) {
      heatmapData = [];
      prevHeatmapMemberId = null;
      return;
    }
    if (selectedMember.userId === prevHeatmapMemberId) return;
    prevHeatmapMemberId = selectedMember.userId;
    const userId = selectedMember.userId;
    untrack(() => loadHeatmapData(userId));
  });

  async function loadHeatmapData(userId: string) {
    try {
      const weeks = 12;
      const today = new SvelteDate();
      const startDate = new SvelteDate(today);
      startDate.setDate(startDate.getDate() - (weeks * 7));
      const endDate = today;

      const dailyData = await getMemberDailyEffort(
        projectId,
        userId,
        formatDate(startDate),
        formatDate(endDate),
      );

      heatmapData = aggregateDailyToHeatmap(dailyData, weeks);
    } catch (err) {
      console.error('Failed to load heatmap data:', err);
      heatmapData = [];
    }
  }

  function aggregateDailyToHeatmap(dailyData: DailyEffort[], weeks: number): HeatmapWeek[] {
    const effortMap = new Map(dailyData.map((d) => [d.date, d]));
    const result: HeatmapWeek[] = [];
    const today = new SvelteDate();

    const startDate = new SvelteDate(today);
    startDate.setDate(startDate.getDate() - (weeks * 7));
    const dayOfWeek = startDate.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    startDate.setDate(startDate.getDate() + diff);

    for (let w = 0; w < weeks; w++) {
      const weekStart = new SvelteDate(startDate);
      weekStart.setDate(weekStart.getDate() + (w * 7));

      const days: (HeatmapWeek['days'][number])[] = [];

      for (let d = 0; d < 5; d++) {
        const date = new SvelteDate(weekStart);
        date.setDate(date.getDate() + d);

        if (date > today) {
          days.push(null);
          continue;
        }

        const dateStr = formatDate(date);
        const entry = effortMap.get(dateStr);
        const allocated = entry?.allocatedHours ?? 0;
        const spent = entry?.spentHours ?? 0;
        const utilization = allocated > 0 ? (spent / allocated) * 100 : 0;

        days.push({ date: dateStr, allocated, spent, utilization });
      }

      result.push({
        weekStart: formatDate(weekStart),
        days,
      });
    }

    return result;
  }

  // Initialize
  $effect(() => {
    pendingRestore = getTabState<ProjectEffortTabState>(tabStateKey) ?? null;
    const unsubscribe = subscribeEffort((state: EffortState) => {
      members = state.members;
      ownership = state.ownership;
      ownershipLoading = state.ownershipLoading;
      ownershipError = state.ownershipError;
      viewMode = state.viewMode;
      memberDailyEffort = state.memberDailyEffort;
      selectedMemberId = state.selectedMemberId;
      weekData = state.weekData;
      conflicts = state.conflicts;
      isLoading = state.isLoading;
      error = state.error;
      storeProjectId = state.projectId;
      dateRange = state.dateRange;
    });

    untrack(() => {
      initEffort(projectId).then(async () => {
        await restoreViewState();
      });
    });

    return () => {
      if (tabStateKey) {
        setTabState<ProjectEffortTabState>(tabStateKey, {
          selectedMemberId,
          dateRange: { ...dateRange },
          viewMode,
          selectedWeekStart: selectedWeekRange?.start ?? null,
        });
      }
      unsubscribe();
      resetEffortStore();
    };
  });

  // Reinitialize when projectId changes
  $effect(() => {
    if (projectId && projectId !== storeProjectId) {
      untrack(() => {
        initEffort(projectId).then(async () => {
          await restoreViewState();
        });
      });
    }
  });

  async function restoreViewState(): Promise<void> {
    if (!pendingRestore) return;
    const savedViewMode = pendingRestore.viewMode;
    const savedRange = pendingRestore.dateRange;

    if (savedViewMode) {
      const anchor = pendingRestore.selectedWeekStart ?? savedRange?.start;
      await setViewMode(savedViewMode, anchor);
    }

    if (savedRange && !savedViewMode) {
      const { start, end } = savedRange;
      if (start !== dateRange.start || end !== dateRange.end) {
        await setDateRange(start, end);
      }
    }

    if (savedViewMode === 'month' && pendingRestore.selectedWeekStart) {
      selectedWeekRange = getWeekRangeFromDate(pendingRestore.selectedWeekStart);
    }

    if (pendingRestore.selectedMemberId) {
      await selectMember(pendingRestore.selectedMemberId);
    }
  }

  function handleMemberSelect(userId: string): void {
    selectMember(userId === selectedMemberId ? null : userId);
  }

  async function handlePrevPeriod(): Promise<void> {
    if (viewMode === 'week') {
      await previousWeek();
    } else {
      await previousMonth();
    }
  }

  async function handleNextPeriod(): Promise<void> {
    if (viewMode === 'week') {
      await nextWeek();
    } else {
      await nextMonth();
    }
  }

  async function handleCurrentPeriod(): Promise<void> {
    if (viewMode === 'week') {
      await goToToday();
    } else {
      await goToCurrentMonth();
    }
  }

  async function handleViewModeChange(mode: 'week' | 'month'): Promise<void> {
    if (mode === viewMode) return;
    if (mode === 'month') {
      selectedWeekRange = { start: dateRange.start, end: dateRange.end };
      await setViewMode('month', dateRange.start);
    } else {
      const anchor = selectedWeekRange?.start ?? dateRange.start;
      await setViewMode('week', anchor);
    }
  }

  function handleWeekSelect(range: { start: string; end: string }): void {
    selectedWeekRange = range;
  }

  // Working day settings modal
  let showSettingsModal = $state(false);
  let workingDays = $state({
    monday: true,
    tuesday: true,
    wednesday: true,
    thursday: true,
    friday: true,
    saturday: false,
    sunday: false,
  });
  let hoursPerDay = $state(8);

  function handleSettingsClick(): void {
    showSettingsModal = true;
  }

  function handleSaveSettings(): void {
    const activeDays = Object.entries(workingDays)
      .filter(([_, active]) => active)
      .map(([day]) => day);
    toast.success(`Settings saved: ${activeDays.length} working days, ${hoursPerDay}h/day`);
    showSettingsModal = false;
  }

  function handleCancelSettings(): void {
    showSettingsModal = false;
  }

  function handleRefresh(): void {
    initEffort(projectId);
  }

  function getWeekRangeFromDate(dateStr: string): { start: string; end: string } {
    const clickedDate = new SvelteDate(dateStr);
    const dayOfWeek = clickedDate.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    const weekStart = new SvelteDate(clickedDate);
    weekStart.setDate(clickedDate.getDate() + diff);

    const weekEnd = new SvelteDate(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);

    const formatDateStr = (d: SvelteDate) => formatDate(d);
    return { start: formatDateStr(weekStart), end: formatDateStr(weekEnd) };
  }

  function buildWeekDataFromDaily(
    dailyEffort: DailyEffort[],
    start: string,
    end: string
  ): WeekData {
    const effortMap = new Map(dailyEffort.map((d) => [d.date, d]));
    const dates: string[] = [];
    const current = new SvelteDate(`${start}T00:00:00`);
    const endDate = new SvelteDate(`${end}T00:00:00`);

    while (current <= endDate) {
      dates.push(formatDate(current));
      current.setDate(current.getDate() + 1);
    }

    const days = dates.map((date) => {
      const existing = effortMap.get(date);
      if (existing) return existing;
      const weekend = isWeekend(date);
      return {
        date,
        allocatedHours: 0,
        spentHours: 0,
        isWorkingDay: !weekend,
        workingDayType: weekend ? 'offday' : 'workday',
        worklogs: [],
        hasConflict: false,
      } satisfies DailyEffort;
    });

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

  function handleHeatmapCellClick(date: string): void {
    const range = getWeekRangeFromDate(date);
    if (viewMode === 'month') {
      setViewMode('week', range.start);
    } else {
      setDateRange(range.start, range.end);
    }
  }

</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} loading={isLoading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh effort data"
        onclick={handleRefresh}
      />
    {/snippet}
  </PageHeader>

  <PageBody noPadding>
    <div class="effort-page">
      {#if error}
        <div class="effort-page__error">
          <EmptyState
            icon="alert-circle"
            heading="Error loading effort data"
            subtext={error}
          />
        </div>
      {:else}
        <SidebarDetailLayout
          sidebarWidth={420}
          minSidebarWidth={380}
          maxSidebarWidth={500}
        >
          {#snippet sidebar()}
            <EffortSidebar
              {members}
              {selectedMemberId}
              {viewMode}
              periodLabel={periodLabel}
              tabId={tabStateKey}
              onMemberSelect={handleMemberSelect}
              onPrevPeriod={handlePrevPeriod}
              onNextPeriod={handleNextPeriod}
              onCurrentPeriod={handleCurrentPeriod}
              onViewModeChange={handleViewModeChange}
              onSettings={handleSettingsClick}
              {isLoading}
            />
          {/snippet}

          <!-- Main Content: Timesheet & Details -->
          <div class="flex flex-col h-full">
            {#if selectedMember}
              <!-- Header -->
              <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
                {#snippet leading()}
                  <div class="flex items-center justify-center w-8 h-8 rounded-full bg-[var(--color-surface-200)] text-[var(--color-text-secondary)] text-xs font-semibold">
                    {selectedMember.userName.slice(0, 1)}
                  </div>
                {/snippet}
                <DetailTitle title={selectedMember.userName}>
                  <span class="text-xs text-[var(--color-text-tertiary)] truncate min-w-0">
                    {selectedMember.userEmail}
                  </span>
                  <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
                    • {selectedMember.role}
                  </span>
                </DetailTitle>
                {#snippet actions()}
                  <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={() => handleMemberSelect(selectedMember.userId)} />
                {/snippet}
              </DetailHeader>

              <!-- Content -->
              <div class="flex-1 overflow-y-auto p-3 space-y-3">
                <!-- Conflict Warnings -->
                {#if hasConflicts}
                  <ConflictWarnings
                    {conflicts}
                    class="flex-shrink-0"
                  />
                {/if}

                {#if viewMode === 'month'}
                  <!-- Monthly Overview -->
                  <div>
                    <div class="flex items-center justify-between mb-2">
                      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Monthly Overview</h3>
                      <span class="text-xs text-[var(--color-text-tertiary)]">{monthLabel}</span>
                    </div>
                    <SurfaceCard class="overflow-hidden" padding="sm">
                      <MonthlyTimesheet
                        dailyEffort={memberDailyEffort ?? []}
                        monthStart={dateRange.start}
                        monthEnd={dateRange.end}
                        selectedWeekStart={selectedWeekRange?.start ?? null}
                        onWeekSelect={handleWeekSelect}
                      />
                    </SurfaceCard>
                  </div>

                  <!-- Weekly Drilldown -->
                  {#if activeWeekData}
                    <div>
                      <div class="flex items-center justify-between mb-2">
                        <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Weekly Timesheet</h3>
                        <span class="text-xs text-[var(--color-text-tertiary)]">{selectedWeekLabel}</span>
                      </div>
                      <SurfaceCard class="overflow-hidden" padding="none">
                        <WeeklyTimesheet
                          weekData={activeWeekData}
                          userId={selectedMember.userId}
                          userName={selectedMember.userName}
                          editable={true}
                          showHeader={false}
                          variant="embedded"
                        />
                      </SurfaceCard>
                    </div>
                  {/if}
                {:else}
                  <!-- Weekly Timesheet -->
                  <div>
                    <div class="flex items-center justify-between mb-2">
                      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Weekly Timesheet</h3>
                      <span class="text-xs text-[var(--color-text-tertiary)]">{weekLabel}</span>
                    </div>
                    {#if activeWeekData}
                      <SurfaceCard class="overflow-hidden" padding="none">
                        <WeeklyTimesheet
                          weekData={activeWeekData}
                          userId={selectedMember.userId}
                          userName={selectedMember.userName}
                          editable={true}
                          showHeader={false}
                          variant="embedded"
                        />
                      </SurfaceCard>
                    {:else}
                      <SurfaceCard class="effort-page__ownership-loading">
                        <span>Loading weekly data...</span>
                      </SurfaceCard>
                    {/if}
                  </div>
                {/if}

                <!-- Production Summary -->
                <div>
                  <div class="flex items-center justify-between mb-2">
                    <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Production</h3>
                    <span class="text-xs text-[var(--color-text-tertiary)]">
                      {viewMode === 'week' ? weekLabel : monthLabel}
                    </span>
                  </div>
                  <MemberProductionSummary
                    member={selectedMember}
                    rangeLabel={viewMode === 'week' ? weekLabel : monthLabel}
                    showHeader={false}
                  />
                </div>

                <!-- Ownership -->
                <div>
                  <h3 class="text-sm font-medium text-[var(--color-text-secondary)] mb-2">Ownership</h3>
                  <div class="effort-page__ownership">
                    {#if ownershipError}
                      <EmptyState
                        icon="alert-circle"
                        heading="Failed to load ownership"
                        subtext={ownershipError}
                      />
                    {:else if ownership}
                      <MemberOwnershipPanel
                        {ownership}
                        userId={selectedMember.userId}
                        isLoading={ownershipLoading}
                        showHeader={false}
                      />
                    {:else if ownershipLoading}
                      <SurfaceCard class="effort-page__ownership-loading">
                        <span>Loading owned deliverables...</span>
                      </SurfaceCard>
                    {:else}
                      <EmptyState
                        icon="layers"
                        heading="No ownership data"
                        subtext="This member has no owned deliverables in this project."
                      />
                    {/if}
                  </div>
                </div>

                <!-- Activity Heatmap -->
                <div>
                  <h3 class="text-sm font-medium text-[var(--color-text-secondary)] mb-2">Activity (Last 12 weeks)</h3>
                  <SurfaceCard class="effort-page__heatmap" padding="sm">
                    <ActivityHeatmap
                      data={heatmapData}
                      weeks={12}
                      onCellClick={handleHeatmapCellClick}
                    />
                  </SurfaceCard>
                </div>
              </div>
            {:else if !isLoading}
              <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
                <DetailTitle title="Overview" />
              </DetailHeader>
              <div class="flex-1 overflow-y-auto p-3 space-y-3">
                <CardGrid cols={4} gap="md">
                  <StatCard title="Team Members" value={members.length} icon="user" iconColor="blue-500" />
                  <StatCard title="Allocated" value="{formatNumber(totalAllocated)}h" icon="clock" iconColor="emerald-500" />
                  <StatCard title="Spent" value="{formatNumber(totalSpent)}h" icon="check-circle" iconColor="amber-500" />
                  <StatCard title="Utilization" value={formatPercent(utilizationRate, { maximumFractionDigits: 0 })} icon="activity" iconColor="purple-500" />
                </CardGrid>

                <div class="effort-page__placeholder">
                  <EmptyState
                    icon="mouse-pointer"
                    heading="Select a team member"
                    subtext="Choose a team member from the sidebar to view details."
                  />
                </div>
              </div>
            {/if}
          </div>
        </SidebarDetailLayout>
      {/if}
    </div>
  </PageBody>
</PageShell>

<!-- Working Day Settings Modal -->
<Modal
  open={showSettingsModal}
  title="Working Day Settings"
  size="sm"
  onClose={handleCancelSettings}
>
  <div class="space-y-6">
    <!-- Working Days -->
    <div>
      <h4 class="text-sm font-medium text-[var(--color-text-primary)] mb-3">Working Days</h4>
      <div class="grid grid-cols-7 gap-2">
        {#each [
          { key: 'monday', label: 'Mon' },
          { key: 'tuesday', label: 'Tue' },
          { key: 'wednesday', label: 'Wed' },
          { key: 'thursday', label: 'Thu' },
          { key: 'friday', label: 'Fri' },
          { key: 'saturday', label: 'Sat' },
          { key: 'sunday', label: 'Sun' },
        ] as day (day.key)}
          <Button
            variant="unstyled"
            class="flex flex-col items-center gap-1 px-2 py-3 rounded-lg border transition-colors
              {workingDays[day.key as keyof typeof workingDays]
                ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                : 'border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => workingDays[day.key as keyof typeof workingDays] = !workingDays[day.key as keyof typeof workingDays]}
          >
            <span class="text-xs font-medium">{day.label}</span>
            <Icon
              name={workingDays[day.key as keyof typeof workingDays] ? 'check-circle' : 'circle'}
              size="sm"
            />
          </Button>
        {/each}
      </div>
    </div>

    <!-- Hours per Day -->
    <div>
      <h4 class="text-sm font-medium text-[var(--color-text-primary)] mb-3">Hours per Day</h4>
      <div class="flex items-center gap-3">
        <!-- eslint-disable-next-line svelte/no-restricted-html-elements -->
        <input
          type="range"
          min="1"
          max="12"
          bind:value={hoursPerDay}
          class="flex-1 h-2 bg-[var(--color-bg-tertiary)] rounded-lg appearance-none cursor-pointer accent-[var(--color-accent-primary)]"
        />
        <span class="w-12 text-center text-sm font-medium text-[var(--color-text-primary)]">
          {hoursPerDay}h
        </span>
      </div>
    </div>
  </div>

  {#snippet footer()}
    <div class="flex justify-end gap-2">
      <Button variant="ghost" onclick={handleCancelSettings}>Cancel</Button>
      <Button variant="primary" onclick={handleSaveSettings}>Save Settings</Button>
    </div>
  {/snippet}
</Modal>

<style>
  .effort-page {
    display: flex;
    flex-direction: column;
    height: 100%;
  }

  .effort-page__error {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2rem;
  }

  :global(.effort-page__heatmap) {
    padding: 0.5rem;
  }

  .effort-page__ownership {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  :global(.effort-page__ownership-loading) {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 120px;
    color: var(--color-text-secondary);
    font-size: var(--text-sm);
  }

  .effort-page__placeholder {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg, 8px);
  }
</style>
