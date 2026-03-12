<!-- Section: EffortSidebar — Projects > Effort -->
<script lang="ts">
  import { Button, Icon, Input, IconButton, RadialProgress, Spinner } from '@sddp/ui';
  import { Dropdown, getTabState, setTabState } from '@sddp/shell';
  import type { MemberEffortSummary } from '../../types';
  import { formatHours, getUtilizationGrade } from '../../stores';
  import MemberCard from '../idioms/MemberCard.svelte';

  interface Props {
    members: MemberEffortSummary[];
    selectedMemberId: string | null;
    viewMode: 'week' | 'month';
    periodLabel: string;
    tabId?: string;
    onMemberSelect: (userId: string) => void;
    onPrevPeriod: () => void;
    onNextPeriod: () => void;
    onCurrentPeriod: () => void;
    onViewModeChange: (mode: 'week' | 'month') => void;
    onSettings?: () => void;
    isLoading?: boolean;
    class?: string;
  }

  let {
    members,
    selectedMemberId,
    viewMode,
    periodLabel,
    tabId = '',
    onMemberSelect,
    onPrevPeriod,
    onNextPeriod,
    onCurrentPeriod,
    onViewModeChange,
    onSettings,
    isLoading = false,
    class: className = '',
  }: Props = $props();

  let searchQuery = $state('');
  let sortBy = $state<'name' | 'utilization'>('name');
  let filterGrade = $state<'all' | 'excellent' | 'good' | 'average' | 'poor'>('all');

  interface EffortSidebarTabState {
    searchQuery: string;
    sortBy: 'name' | 'utilization';
    filterGrade: 'all' | 'excellent' | 'good' | 'average' | 'poor';
  }

  const tabStateKey = $derived(tabId ? `${tabId}:effort-sidebar` : '');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<EffortSidebarTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      sortBy = saved.sortBy ?? 'name';
      filterGrade = saved.filterGrade ?? 'all';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<EffortSidebarTabState>(tabStateKey, {
      searchQuery,
      sortBy,
      filterGrade,
    });
  });

  // Filter and sort members
  const filteredMembers = $derived.by(() => {
    let result = members;

    // Search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (m) =>
          m.userName.toLowerCase().includes(query) ||
          m.userEmail.toLowerCase().includes(query)
      );
    }

    // Grade filter
    if (filterGrade !== 'all') {
      result = result.filter((m) => {
        const grade = getUtilizationGrade(m.utilizationRate).grade;
        return grade === filterGrade;
      });
    }

    // Sort
    return [...result].sort((a, b) => {
      if (sortBy === 'utilization') {
        return b.utilizationRate - a.utilizationRate;
      }
      return a.userName.localeCompare(b.userName);
    });
  });

  const filteredTotals = $derived.by(() => {
    return filteredMembers.reduce(
      (totals, member) => {
        totals.allocated += member.totalAllocated;
        totals.spent += member.totalSpent;
        return totals;
      },
      { allocated: 0, spent: 0 }
    );
  });

  const filteredRemaining = $derived(filteredTotals.allocated - filteredTotals.spent);
  const filteredUtilization = $derived(
    filteredTotals.allocated > 0
      ? Math.round((filteredTotals.spent / filteredTotals.allocated) * 100)
      : 0
  );

  const gradeOptions = [
    { key: 'all', label: 'All Members' },
    { key: 'excellent', label: 'Excellent (100%+)' },
    { key: 'good', label: 'Good (80%+)' },
    { key: 'average', label: 'Average (50%+)' },
    { key: 'poor', label: 'Poor (<50%)' },
  ] as const;

  const sortOptions = [
    { key: 'name', label: 'Sort by Name' },
    { key: 'utilization', label: 'Sort by Utilization' },
  ] as const;
</script>

<div class="effort-sidebar {className}">
  <!-- Header: Period Navigation + Search + Actions -->
  <div class="effort-sidebar__header">
    <div class="effort-sidebar__header-row">
      <div class="effort-sidebar__period">
        <IconButton
          icon="chevron-left"
          size="xs"
          variant="ghost"
          onclick={onPrevPeriod}
          title={viewMode === 'week' ? 'Previous week' : 'Previous month'}
        />
        <Dropdown position="bottom-left">
          {#snippet trigger()}
            <Button
              variant="unstyled"
              class="effort-sidebar__period-label"
              title="Switch view"
            >
              {periodLabel}
              <Icon name="chevron-down" size="xs" />
            </Button>
          {/snippet}
          <div class="effort-sidebar__menu">
            <div class="effort-sidebar__menu-section">
              <span class="effort-sidebar__menu-label">View</span>
              <Button
                variant="unstyled"
                class="effort-sidebar__menu-item {viewMode === 'week' ? 'effort-sidebar__menu-item--active' : ''}"
                onclick={() => onViewModeChange('week')}
              >
                Week
                {#if viewMode === 'week'}
                  <Icon name="check" size="xs" />
                {/if}
              </Button>
              <Button
                variant="unstyled"
                class="effort-sidebar__menu-item {viewMode === 'month' ? 'effort-sidebar__menu-item--active' : ''}"
                onclick={() => onViewModeChange('month')}
              >
                Month
                {#if viewMode === 'month'}
                  <Icon name="check" size="xs" />
                {/if}
              </Button>
            </div>
            <div class="effort-sidebar__menu-divider"></div>
            <Button variant="unstyled" class="effort-sidebar__menu-item" onclick={onCurrentPeriod}>
              {viewMode === 'week' ? 'Go to current week' : 'Go to current month'}
            </Button>
          </div>
        </Dropdown>
        <IconButton
          icon="chevron-right"
          size="xs"
          variant="ghost"
          onclick={onNextPeriod}
          title={viewMode === 'week' ? 'Next week' : 'Next month'}
        />
      </div>

      <!-- Search (inline) -->
      <div class="effort-sidebar__search">
        <Icon
          name="search"
          size="sm"
          class="effort-sidebar__search-icon"
        />
        <Input
          type="text"
          placeholder="Search..."
          bind:value={searchQuery}
          variant="flat"
          size="sm"
          class="effort-sidebar__search-input"
        />
      </div>

      <div class="effort-sidebar__actions">
        {#if onSettings}
          <IconButton
            icon="settings-gear"
            size="sm"
            variant="ghost"
            onclick={onSettings}
            title="Settings"
          />
        {/if}
        <Dropdown position="bottom-right">
          {#snippet trigger()}
            <IconButton
              icon="more-vertical"
              size="sm"
              variant="ghost"
              title="More options"
            />
          {/snippet}
          <div class="effort-sidebar__menu">
            <div class="effort-sidebar__menu-section">
              <span class="effort-sidebar__menu-label">Filter</span>
              {#each gradeOptions as option (option.key)}
                <Button
                  variant="unstyled"
                  class="effort-sidebar__menu-item {filterGrade === option.key ? 'effort-sidebar__menu-item--active' : ''}"
                  onclick={() => filterGrade = option.key}
                >
                  {option.label}
                  {#if filterGrade === option.key}
                    <Icon name="check" size="sm" />
                  {/if}
                </Button>
              {/each}
            </div>
            <div class="effort-sidebar__menu-divider"></div>
            <div class="effort-sidebar__menu-section">
              <span class="effort-sidebar__menu-label">Sort</span>
              {#each sortOptions as option (option.key)}
                <Button
                  variant="unstyled"
                  class="effort-sidebar__menu-item {sortBy === option.key ? 'effort-sidebar__menu-item--active' : ''}"
                  onclick={() => sortBy = option.key}
                >
                  {option.label}
                  {#if sortBy === option.key}
                    <Icon name="check" size="sm" />
                  {/if}
                </Button>
              {/each}
            </div>
          </div>
        </Dropdown>
      </div>
    </div>
  </div>

  <!-- Table Header -->
  <div class="effort-sidebar__table-header">
    <span class="effort-sidebar__col effort-sidebar__col--member">Member</span>
    <span class="effort-sidebar__col effort-sidebar__col--num">Allocated</span>
    <span class="effort-sidebar__col effort-sidebar__col--num">Spent</span>
    <span class="effort-sidebar__col effort-sidebar__col--num">Remaining</span>
    <span class="effort-sidebar__col effort-sidebar__col--progress"></span>
  </div>

  <!-- Member List -->
  <div class="effort-sidebar__list">
    {#if isLoading}
      <div class="effort-sidebar__loading">
        <Spinner size="md" />
        <span>Loading...</span>
      </div>
    {:else if filteredMembers.length === 0}
      <div class="effort-sidebar__empty">
        <Icon name="user" size="lg" />
        <span>{searchQuery ? 'No matching members' : 'No members'}</span>
      </div>
    {:else}
      {#each filteredMembers as member (member.userId)}
        <MemberCard
          {member}
          isSelected={member.userId === selectedMemberId}
          onclick={() => onMemberSelect(member.userId)}
        />
      {/each}
    {/if}
    {#if !isLoading && members.length > 0}
      <div class="effort-sidebar__total">
        <div class="effort-sidebar__total-row">
          <div class="effort-sidebar__total-label">Total (filtered)</div>
          <span class="effort-sidebar__total-num">{formatHours(filteredTotals.allocated)}</span>
          <span class="effort-sidebar__total-num">{formatHours(filteredTotals.spent)}</span>
          <span
            class="effort-sidebar__total-num"
            class:effort-sidebar__total-num--negative={filteredRemaining < 0}
          >
            {formatHours(filteredRemaining)}
          </span>
          <div class="effort-sidebar__total-progress" title={`${filteredUtilization}%`}>
            <RadialProgress
              value={filteredUtilization}
              size="xs"
              variant={getUtilizationGrade(filteredUtilization).variant}
            />
          </div>
        </div>
      </div>
    {/if}
  </div>
</div>

<style>
  .effort-sidebar {
    display: flex;
    flex-direction: column;
    height: 100%;
    background-color: var(--color-bg-primary);
  }

  /* Header */
  .effort-sidebar__header {
    display: flex;
    flex-direction: column;
    justify-content: center;
    min-height: 3rem;
    padding: 0 1rem;
    border-bottom: 1px solid var(--color-border);
  }

  .effort-sidebar__header-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
  }

  .effort-sidebar__period {
    display: flex;
    align-items: center;
    gap: 0;
    flex-shrink: 0;
  }

  :global(.effort-sidebar__period-label) {
    padding: 0.125rem 0.375rem;
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-primary);
    background: transparent;
    border: none;
    border-radius: var(--radius-sm);
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    cursor: pointer;
    transition: background-color 150ms ease;
  }

  :global(.effort-sidebar__period-label:hover) {
    background-color: var(--color-bg-tertiary);
  }

  /* Search (inline in header row) */
  .effort-sidebar__search {
    position: relative;
    flex: 1;
    min-width: 0;
  }

  :global(.effort-sidebar__search-icon) {
    position: absolute;
    left: 0.5rem;
    top: 50%;
    transform: translateY(-50%);
    color: var(--color-text-tertiary);
    pointer-events: none;
    z-index: 1;
  }

  :global(.effort-sidebar__search-input) {
    padding-left: 1.75rem !important;
    width: 100%;
  }

  .effort-sidebar__actions {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    flex-shrink: 0;
  }

  /* Menu (inside Dropdown portal — needs :global) */
  :global(.effort-sidebar__menu) {
    min-width: 160px;
    padding: 0.25rem 0;
  }

  :global(.effort-sidebar__menu-section) {
    padding: 0.125rem 0;
  }

  :global(.effort-sidebar__menu-label) {
    display: block;
    padding: 0.125rem 0.625rem;
    font-size: var(--text-2xs, 0.65rem);
    font-weight: 600;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  :global(.effort-sidebar__menu-item) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 0.25rem 0.625rem;
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
    background: transparent;
    border: none;
    cursor: pointer;
    transition: background-color 150ms ease;
  }

  :global(.effort-sidebar__menu-item:hover) {
    background-color: var(--color-bg-tertiary);
  }

  :global(.effort-sidebar__menu-item--active) {
    color: var(--color-accent-primary);
  }

  :global(.effort-sidebar__menu-divider) {
    height: 1px;
    margin: 0.25rem 0;
    background-color: var(--color-border);
  }

  /* Table Header */
  .effort-sidebar__table-header {
    display: grid;
    grid-template-columns: 1fr 68px 52px 72px 48px;
    gap: 4px;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid var(--color-border);
    background-color: var(--color-bg-secondary);
  }

  .effort-sidebar__col {
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.025em;
    text-align: center;
  }

  .effort-sidebar__col--member {
    text-align: left;
  }

  /* List */
  .effort-sidebar__list {
    flex: 1;
    overflow-y: auto;
    position: relative;
  }

  .effort-sidebar__total {
    position: sticky;
    bottom: 0;
    z-index: 1;
    border-top: 1px solid var(--color-border);
    background-color: var(--color-bg-secondary);
    box-shadow: 0 -6px 12px rgba(0, 0, 0, 0.04);
  }

  .effort-sidebar__total-row {
    display: grid;
    grid-template-columns: 1fr 68px 52px 72px 48px;
    gap: 4px;
    align-items: center;
    padding: 0.5rem 0.75rem;
  }

  .effort-sidebar__total-label {
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-secondary);
  }

  .effort-sidebar__total-num {
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-secondary);
    text-align: center;
    font-variant-numeric: tabular-nums;
  }

  .effort-sidebar__total-num--negative {
    color: var(--color-danger-500);
  }

  .effort-sidebar__total-progress {
    display: flex;
    justify-content: center;
  }

  .effort-sidebar__loading,
  .effort-sidebar__empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 2rem;
    color: var(--color-text-tertiary);
    font-size: var(--text-sm);
  }

  :global(.effort-sidebar__spinner) {
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }
</style>
