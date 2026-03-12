<!--
  SplitEditorGroup Section Component
  Manages multiple editor groups with split functionality and resizing
-->
<script lang="ts">
  import { SvelteSet, SvelteMap } from 'svelte/reactivity';
  import type { EditorGroupData } from '../../types/layout.types';
  import EditorGroup from './EditorGroup.svelte';
  import { tabActions } from '../../stores/tabs.store';
  import { computePositionsFromSplitters, computePositionsFromMixedLines } from '../../utils/split-editor.utils';

  interface SplitEditorGroupProps {
    groups: EditorGroupData[];
    activeGroupId: string;
    onTabChange?: (tabId: string, groupId: string) => void;
    onTabClose?: (tabId: string, groupId: string) => void;
    class?: string;
  }

  let {
    groups,
    activeGroupId,
    onTabChange,
    onTabClose,
    class: className = '',
  }: SplitEditorGroupProps = $props();

  // Splitter drag state
  let isDragging = $state(false);
  let currentSplitter = $state<number | null>(null);
  let splitterPositions = $state<number[]>([]);
  let mixedDragging = $state<{ orientation: 'horizontal' | 'vertical'; lineIndex: number } | null>(null);

  // Container reference for position calculations
  let containerElement = $state<HTMLDivElement | undefined>();

  // Determine layout direction based on group positions
  function getLayoutDirection(): 'horizontal' | 'vertical' {
    if (groups.length <= 1) return 'horizontal';

    const uniqueY = new Set(groups.map((g) => g.position.y));
    return uniqueY.size === 1 ? 'horizontal' : 'vertical';
  }

  type LayoutMode = 'horizontal' | 'vertical' | 'mixed';
  type GroupCell = { colStart: number; colEnd: number; rowStart: number; rowEnd: number };
  type SplitterSegment = {
    key: string;
    orientation: 'horizontal' | 'vertical';
    lineIndex: number;
    startIndex: number;
    endIndex: number;
  };

  function getLayoutMode(): LayoutMode {
    if (groups.length <= 1) return 'horizontal';
    const uniqueX = new Set(groups.map((g) => g.position.x));
    const uniqueY = new Set(groups.map((g) => g.position.y));
    if (uniqueY.size === 1) return 'horizontal';
    if (uniqueX.size === 1) return 'vertical';
    return 'mixed';
  }

  let layoutDirection = $derived(getLayoutDirection());
  let layoutMode = $derived(getLayoutMode());
  let mixedXLines = $state<number[]>([]);
  let mixedYLines = $state<number[]>([]);
  let mixedCells = $state<Record<string, GroupCell>>({});

  // Key to detect structural changes in group positions (prevents resetting user drag adjustments)
  let prevMixedLayoutKey = $state('');

  // Initialize splitter positions when groups change.
  // Derives positions from store group.position instead of equal distribution,
  // so that drag-adjusted positions are preserved across layout transitions.
  $effect(() => {
    if (groups.length > 1 && layoutMode !== 'mixed') {
      const neededSplitters = groups.length - 1;
      if (splitterPositions.length !== neededSplitters) {
        const newPositions: number[] = [];
        if (layoutDirection === 'horizontal') {
          const sorted = [...groups].sort((a, b) => a.position.x - b.position.x);
          let cumulative = 0;
          for (let i = 0; i < sorted.length - 1; i++) {
            cumulative += sorted[i]!.position.width;
            newPositions.push(cumulative);
          }
        } else {
          const sorted = [...groups].sort((a, b) => a.position.y - b.position.y);
          let cumulative = 0;
          for (let i = 0; i < sorted.length - 1; i++) {
            cumulative += sorted[i]!.position.height;
            newPositions.push(cumulative);
          }
        }
        splitterPositions = newPositions;
      }
    } else if (splitterPositions.length > 0) {
      splitterPositions = [];
    }
  });

  // Handle tab change
  function handleTabChange(tabId: string, groupId: string) {
    tabActions.switchToTab(tabId, groupId);
    tabActions.setActiveGroup(groupId);
    onTabChange?.(tabId, groupId);
  }

  // Handle tab close
  function handleTabClose(tabId: string, groupId: string) {
    tabActions.closeTab(tabId, groupId);
    onTabClose?.(tabId, groupId);
  }

  // Handle group split
  function handleGroupSplit(direction: 'horizontal' | 'vertical', groupId: string) {
    const targetGroup = groups.find((g) => g.id === groupId);
    if (targetGroup?.activeTab) {
      if (direction === 'horizontal') {
        tabActions.splitRight(targetGroup.activeTab, groupId);
      } else {
        tabActions.splitGroup(direction, groupId);
      }
    }
  }

  // Handle group focus
  function handleGroupFocus(groupId: string) {
    tabActions.setActiveGroup(groupId);
  }

  // Splitter drag handlers
  function handleSplitterPointerDown(e: PointerEvent, index: number) {
    e.preventDefault();
    isDragging = true;
    currentSplitter = index;

    const target = e.target as HTMLElement;
    target.setPointerCapture(e.pointerId);

    document.addEventListener('pointermove', handlePointerMove);
    document.addEventListener('pointerup', handlePointerUp);
  }

  function handlePointerMove(e: PointerEvent) {
    if (!isDragging || currentSplitter === null || !containerElement) return;

    const rect = containerElement.getBoundingClientRect();
    const direction = layoutDirection;
    let newPosition: number;

    if (direction === 'horizontal') {
      newPosition = (e.clientX - rect.left) / rect.width;
    } else {
      newPosition = (e.clientY - rect.top) / rect.height;
    }

    // Constrain position
    const prevPos = currentSplitter > 0 ? splitterPositions[currentSplitter - 1] : undefined;
    const nextPos = currentSplitter < splitterPositions.length - 1 ? splitterPositions[currentSplitter + 1] : undefined;
    const minPos = currentSplitter === 0 ? 0.1 : (prevPos ?? 0) + 0.1;
    const maxPos = currentSplitter === splitterPositions.length - 1 ? 0.9 : (nextPos ?? 1) - 0.1;

    newPosition = Math.max(minPos, Math.min(maxPos, newPosition));

    splitterPositions[currentSplitter] = newPosition;
    splitterPositions = [...splitterPositions];
  }

  function handlePointerUp(e: PointerEvent) {
    isDragging = false;
    currentSplitter = null;

    const target = e.target as HTMLElement;
    if (target.hasPointerCapture?.(e.pointerId)) {
      target.releasePointerCapture(e.pointerId);
    }

    document.removeEventListener('pointermove', handlePointerMove);
    document.removeEventListener('pointerup', handlePointerUp);

    // Sync visual splitter positions back to store
    const updates = computePositionsFromSplitters(groups, splitterPositions, layoutDirection);
    tabActions.updateGroupPositions(updates);
  }

  // Generate grid template based on splitter positions
  function getGridTemplate(): string {
    if (groups.length <= 1) return '1fr';

    const parts: string[] = [];
    let lastPos = 0;

    for (let i = 0; i < groups.length; i++) {
      const currentPos = i === groups.length - 1 ? 1 : (splitterPositions[i] ?? (i + 1) / groups.length);
      const size = currentPos - lastPos;
      parts.push(`${(size * 100).toFixed(2)}fr`);
      lastPos = currentPos;

      if (i < groups.length - 1) {
        parts.push('4px'); // Splitter width
      }
    }

    return parts.join(' ');
  }

  function getMixedGridLines() {
    const xLines = new SvelteSet<number>([0, 1]);
    const yLines = new SvelteSet<number>([0, 1]);

    for (const group of groups) {
      xLines.add(group.position.x);
      xLines.add(group.position.x + group.position.width);
      yLines.add(group.position.y);
      yLines.add(group.position.y + group.position.height);
    }

    const xs = Array.from(xLines).sort((a, b) => a - b);
    const ys = Array.from(yLines).sort((a, b) => a - b);

    return { xs, ys };
  }

  function buildTemplateFromLines(lines: number[]): string {
    const parts: string[] = [];
    for (let i = 0; i < lines.length - 1; i++) {
      const size = Math.max(0, lines[i + 1]! - lines[i]!);
      parts.push(`${(size * 100).toFixed(2)}%`);
    }
    return parts.join(' ');
  }

  function findLineIndex(lines: number[], value: number): number {
    const epsilon = 0.0001;
    for (let i = 0; i < lines.length; i++) {
      if (Math.abs(lines[i]! - value) <= epsilon) {
        return i + 1;
      }
    }
    return -1;
  }

  function getMixedGridAreaById(groupId: string): string {
    const cell = mixedCells[groupId];
    if (!cell) return '1 / 1 / 2 / 2';
    return `${cell.rowStart} / ${cell.colStart} / ${cell.rowEnd} / ${cell.colEnd}`;
  }

  let mixedColumns = $derived(buildTemplateFromLines(mixedXLines));
  let mixedRows = $derived(buildTemplateFromLines(mixedYLines));

  $effect(() => {
    if (layoutMode !== 'mixed') {
      if (mixedXLines.length > 0) mixedXLines = [];
      if (mixedYLines.length > 0) mixedYLines = [];
      if (Object.keys(mixedCells).length > 0) mixedCells = {};
      prevMixedLayoutKey = '';
      return;
    }

    // Build a structural key from group positions only.
    // This prevents re-initializing grid lines when non-structural props
    // (like activeTab) change, which would overwrite user drag adjustments.
    const layoutKey = groups
      .map((g) => `${g.id}:${g.position.x},${g.position.y},${g.position.width},${g.position.height}`)
      .join('|');

    if (layoutKey === prevMixedLayoutKey) return;
    prevMixedLayoutKey = layoutKey;

    const baseLines = getMixedGridLines();
    mixedXLines = baseLines.xs;
    mixedYLines = baseLines.ys;

    const nextCells: Record<string, GroupCell> = {};
    for (const group of groups) {
      const colStart = findLineIndex(baseLines.xs, group.position.x);
      const colEnd = findLineIndex(baseLines.xs, group.position.x + group.position.width);
      const rowStart = findLineIndex(baseLines.ys, group.position.y);
      const rowEnd = findLineIndex(baseLines.ys, group.position.y + group.position.height);

      if (colStart === -1 || colEnd === -1 || rowStart === -1 || rowEnd === -1) continue;

      nextCells[group.id] = { colStart, colEnd, rowStart, rowEnd };
    }

    mixedCells = nextCells;
  });

  function getMixedSplitters(): SplitterSegment[] {
    if (layoutMode !== 'mixed') return [];
    if (mixedXLines.length < 3 && mixedYLines.length < 3) return [];

    const segments = new SvelteMap<string, SplitterSegment>();
    const groupEntries = groups
      .map((g) => ({ id: g.id, cell: mixedCells[g.id] }))
      .filter((entry) => entry.cell);

    for (let i = 0; i < groupEntries.length; i++) {
      const a = groupEntries[i];
      if (!a?.cell) continue;
      for (let j = 0; j < groupEntries.length; j++) {
        const b = groupEntries[j];
        if (!b?.cell || a.id === b.id) continue;

        if (a.cell.colEnd === b.cell.colStart) {
          const start = Math.max(a.cell.rowStart, b.cell.rowStart);
          const end = Math.min(a.cell.rowEnd, b.cell.rowEnd);
          if (start < end && a.cell.colEnd > 1 && a.cell.colEnd < mixedXLines.length) {
            const key = `v-${a.cell.colEnd}-${start}-${end}`;
            if (!segments.has(key)) {
              segments.set(key, {
                key,
                orientation: 'vertical',
                lineIndex: a.cell.colEnd,
                startIndex: start,
                endIndex: end,
              });
            }
          }
        }

        if (a.cell.rowEnd === b.cell.rowStart) {
          const start = Math.max(a.cell.colStart, b.cell.colStart);
          const end = Math.min(a.cell.colEnd, b.cell.colEnd);
          if (start < end && a.cell.rowEnd > 1 && a.cell.rowEnd < mixedYLines.length) {
            const key = `h-${a.cell.rowEnd}-${start}-${end}`;
            if (!segments.has(key)) {
              segments.set(key, {
                key,
                orientation: 'horizontal',
                lineIndex: a.cell.rowEnd,
                startIndex: start,
                endIndex: end,
              });
            }
          }
        }
      }
    }

    return Array.from(segments.values());
  }

  let mixedSplitters = $derived(getMixedSplitters());

  function handleMixedSplitterPointerDown(e: PointerEvent, segment: SplitterSegment) {
    e.preventDefault();
    if (!containerElement) return;

    mixedDragging = { orientation: segment.orientation, lineIndex: segment.lineIndex };
    const target = e.currentTarget as HTMLElement;
    target.setPointerCapture(e.pointerId);

    document.addEventListener('pointermove', handleMixedPointerMove);
    document.addEventListener('pointerup', handleMixedPointerUp);
    document.body.style.cursor = segment.orientation === 'vertical' ? 'col-resize' : 'row-resize';
    document.body.style.userSelect = 'none';
  }

  function handleMixedPointerMove(e: PointerEvent) {
    if (!mixedDragging || !containerElement) return;

    const rect = containerElement.getBoundingClientRect();
    const { orientation, lineIndex } = mixedDragging;
    const minGap = 0.1;

    if (orientation === 'vertical') {
      if (mixedXLines.length < 3) return;
      const prev = mixedXLines[lineIndex - 2];
      const next = mixedXLines[lineIndex];
      if (prev === undefined || next === undefined) return;

      let newPos = (e.clientX - rect.left) / rect.width;
      const minPos = prev + minGap;
      const maxPos = next - minGap;
      newPos = Math.max(minPos, Math.min(maxPos, newPos));

      mixedXLines[lineIndex - 1] = newPos;
      mixedXLines = [...mixedXLines];
      return;
    }

    if (mixedYLines.length < 3) return;
    const prev = mixedYLines[lineIndex - 2];
    const next = mixedYLines[lineIndex];
    if (prev === undefined || next === undefined) return;

    let newPos = (e.clientY - rect.top) / rect.height;
    const minPos = prev + minGap;
    const maxPos = next - minGap;
    newPos = Math.max(minPos, Math.min(maxPos, newPos));

    mixedYLines[lineIndex - 1] = newPos;
    mixedYLines = [...mixedYLines];
  }

  function handleMixedPointerUp(e: PointerEvent) {
    const target = e.target as HTMLElement;
    mixedDragging = null;

    if (target?.hasPointerCapture?.(e.pointerId)) {
      target.releasePointerCapture(e.pointerId);
    }

    document.removeEventListener('pointermove', handleMixedPointerMove);
    document.removeEventListener('pointerup', handleMixedPointerUp);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';

    // Sync visual mixed grid positions back to store
    const updates = computePositionsFromMixedLines(groups, mixedXLines, mixedYLines, mixedCells);
    tabActions.updateGroupPositions(updates);
  }

  // Get grid area for a group
  function getGridArea(index: number): string {
    // Each group occupies column (index * 2 + 1) to account for splitters
    const col = index * 2 + 1;
    return `1 / ${col} / 2 / ${col + 1}`;
  }

  // Get splitter grid area
  function getSplitterGridArea(index: number): string {
    const col = index * 2 + 2; // Splitters are at even positions
    return `1 / ${col} / 2 / ${col + 1}`;
  }
</script>

<div
  bind:this={containerElement}
  class="flex-1 relative {className}"
  style="display: grid; {layoutMode === 'mixed'
    ? `grid-template-columns: ${mixedColumns}; grid-template-rows: ${mixedRows};`
    : layoutDirection === 'horizontal'
      ? `grid-template-columns: ${getGridTemplate()}; grid-template-rows: 1fr;`
      : `grid-template-rows: ${getGridTemplate()}; grid-template-columns: 1fr;`}"
>
  {#each groups as group, index (group.id)}
    <!-- Editor Group - focusable container for keyboard navigation -->
    <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
    <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
    <div
      class="relative min-w-0 min-h-0 {activeGroupId === group.id
        ? 'ring-1 ring-inset ring-[var(--color-accent-primary)]/20'
        : ''}"
      style="grid-area: {layoutMode === 'mixed'
        ? getMixedGridAreaById(group.id)
        : layoutDirection === 'horizontal'
          ? getGridArea(index)
          : `${index * 2 + 1} / 1 / ${index * 2 + 2} / 2`};"
      onclick={() => handleGroupFocus(group.id)}
      onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') handleGroupFocus(group.id); }}
      role="group"
      tabindex="0"
      aria-label="Editor group {index + 1}"
    >
      <EditorGroup
        {group}
        onTabChange={(tabId) => handleTabChange(tabId, group.id)}
        onTabClose={(tabId) => handleTabClose(tabId, group.id)}
        onSplit={(direction) => handleGroupSplit(direction, group.id)}
        class="h-full"
      />
    </div>

    <!-- Splitter between groups -->
    {#if layoutMode !== 'mixed' && index < groups.length - 1}
      <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
      <div
        class="relative z-20 {layoutDirection === 'horizontal'
          ? 'cursor-col-resize hover:bg-[var(--color-accent-primary)]/30'
          : 'cursor-row-resize hover:bg-[var(--color-accent-primary)]/30'}
          {isDragging && currentSplitter === index ? 'bg-[var(--color-accent-primary)]/50' : 'bg-[var(--color-border)]'}
          transition-colors"
        style="grid-area: {layoutDirection === 'horizontal'
          ? getSplitterGridArea(index)
          : `${index * 2 + 2} / 1 / ${index * 2 + 3} / 2`};"
        role="separator"
        aria-orientation={layoutDirection}
        aria-valuenow={Math.round((splitterPositions[index] ?? 0.5) * 100)}
        aria-valuemin={0}
        aria-valuemax={100}
        tabindex="0"
        onpointerdown={(e) => handleSplitterPointerDown(e, index)}
      >
        <!-- Splitter handle visual indicator -->
        <div
          class="absolute inset-0 flex items-center justify-center pointer-events-none"
        >
          <div
            class="{layoutDirection === 'horizontal' ? 'h-8 w-1' : 'w-8 h-1'} rounded bg-[var(--color-text-tertiary)]/30"
          ></div>
        </div>
      </div>
    {/if}
  {/each}

  {#if layoutMode === 'mixed'}
    {#each mixedSplitters as segment (segment.key)}
      <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
      <div
        class="absolute z-20 {segment.orientation === 'vertical'
          ? 'cursor-col-resize'
          : 'cursor-row-resize'}
          {mixedDragging && mixedDragging.lineIndex === segment.lineIndex && mixedDragging.orientation === segment.orientation
            ? 'bg-[var(--color-accent-primary)]/50'
            : 'bg-[var(--color-border)] hover:bg-[var(--color-accent-primary)]/30'}
          transition-colors"
        style="{segment.orientation === 'vertical'
          ? `left: ${(mixedXLines[segment.lineIndex - 1] ?? 0) * 100}%; top: ${(mixedYLines[segment.startIndex - 1] ?? 0) * 100}%; width: 4px; height: ${((mixedYLines[segment.endIndex - 1] ?? 0) - (mixedYLines[segment.startIndex - 1] ?? 0)) * 100}%; margin-left: -2px;`
          : `top: ${(mixedYLines[segment.lineIndex - 1] ?? 0) * 100}%; left: ${(mixedXLines[segment.startIndex - 1] ?? 0) * 100}%; height: 4px; width: ${((mixedXLines[segment.endIndex - 1] ?? 0) - (mixedXLines[segment.startIndex - 1] ?? 0)) * 100}%; margin-top: -2px;`}"
        role="separator"
        aria-orientation={segment.orientation === 'vertical' ? 'vertical' : 'horizontal'}
        aria-valuenow={50}
        aria-valuemin={0}
        aria-valuemax={100}
        tabindex="0"
        onpointerdown={(e) => handleMixedSplitterPointerDown(e, segment)}
      >
        <div
          class="absolute inset-0 flex items-center justify-center pointer-events-none"
        >
          <div
            class="{segment.orientation === 'vertical' ? 'h-8 w-1' : 'w-8 h-1'} rounded bg-[var(--color-text-tertiary)]/30"
          ></div>
        </div>
      </div>
    {/each}
  {/if}
</div>
