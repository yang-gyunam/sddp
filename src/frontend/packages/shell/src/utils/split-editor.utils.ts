/**
 * Split Editor Group Utility Functions
 * Logic extracted from SplitEditorGroup component for testability
 */

import type { EditorGroupData } from '../types/layout.types';

export type LayoutDirection = 'horizontal' | 'vertical';

/**
 * Determines layout direction based on group positions
 * Returns 'horizontal' if all groups are on the same Y axis (side by side)
 * Returns 'vertical' if groups are on different Y axes (stacked)
 */
export function getLayoutDirection(groups: EditorGroupData[]): LayoutDirection {
  if (groups.length <= 1) return 'horizontal';

  const uniqueY = new Set(groups.map((g) => g.position.y));
  return uniqueY.size === 1 ? 'horizontal' : 'vertical';
}

/**
 * Initializes splitter positions for equal distribution
 */
export function initializeSplitterPositions(groupCount: number): number[] {
  if (groupCount <= 1) return [];

  const neededSplitters = groupCount - 1;
  const positions: number[] = [];

  for (let i = 0; i < neededSplitters; i++) {
    positions.push((i + 1) / groupCount);
  }

  return positions;
}

/**
 * Generates CSS grid template based on splitter positions
 */
export function generateGridTemplate(
  groupCount: number,
  splitterPositions: number[]
): string {
  if (groupCount <= 1) return '1fr';

  const parts: string[] = [];
  let lastPos = 0;

  for (let i = 0; i < groupCount; i++) {
    const currentPos =
      i === groupCount - 1 ? 1 : (splitterPositions[i] ?? (i + 1) / groupCount);
    const size = currentPos - lastPos;
    parts.push(`${(size * 100).toFixed(2)}fr`);
    lastPos = currentPos;

    if (i < groupCount - 1) {
      parts.push('4px'); // Splitter width
    }
  }

  return parts.join(' ');
}

/**
 * Gets grid area string for a group (horizontal layout)
 */
export function getGroupGridArea(index: number): string {
  // Each group occupies column (index * 2 + 1) to account for splitters
  const col = index * 2 + 1;
  return `1 / ${col} / 2 / ${col + 1}`;
}

/**
 * Gets grid area string for a group (vertical layout)
 */
export function getGroupGridAreaVertical(index: number): string {
  return `${index * 2 + 1} / 1 / ${index * 2 + 2} / 2`;
}

/**
 * Gets grid area string for a splitter (horizontal layout)
 */
export function getSplitterGridArea(index: number): string {
  const col = index * 2 + 2; // Splitters are at even positions
  return `1 / ${col} / 2 / ${col + 1}`;
}

/**
 * Gets grid area string for a splitter (vertical layout)
 */
export function getSplitterGridAreaVertical(index: number): string {
  return `${index * 2 + 2} / 1 / ${index * 2 + 3} / 2`;
}

/**
 * Constrains splitter position within valid bounds
 */
export function constrainSplitterPosition(
  newPosition: number,
  splitterIndex: number,
  splitterPositions: number[],
  minGap: number = 0.1
): number {
  const prevPos =
    splitterIndex > 0 ? splitterPositions[splitterIndex - 1] : undefined;
  const nextPos =
    splitterIndex < splitterPositions.length - 1
      ? splitterPositions[splitterIndex + 1]
      : undefined;

  const minPos = splitterIndex === 0 ? minGap : (prevPos ?? 0) + minGap;
  const maxPos =
    splitterIndex === splitterPositions.length - 1
      ? 1 - minGap
      : (nextPos ?? 1) - minGap;

  return Math.max(minPos, Math.min(maxPos, newPosition));
}

/**
 * Calculates new splitter position from pointer event
 */
export function calculateSplitterPositionFromPointer(
  clientX: number,
  clientY: number,
  containerRect: { left: number; top: number; width: number; height: number },
  direction: LayoutDirection
): number {
  if (direction === 'horizontal') {
    return (clientX - containerRect.left) / containerRect.width;
  }
  return (clientY - containerRect.top) / containerRect.height;
}

/**
 * Generates full grid style string
 */
export function generateGridStyle(
  direction: LayoutDirection,
  template: string
): string {
  if (direction === 'horizontal') {
    return `grid-template-columns: ${template}; grid-template-rows: 1fr;`;
  }
  return `grid-template-rows: ${template}; grid-template-columns: 1fr;`;
}

/**
 * Computes group positions from non-mixed splitter positions.
 * Converts visual splitter positions back to normalized 0-1 position coordinates.
 */
export function computePositionsFromSplitters(
  groups: EditorGroupData[],
  splitterPositions: number[],
  direction: LayoutDirection
): Record<string, { x: number; y: number; width: number; height: number }> {
  const updates: Record<string, { x: number; y: number; width: number; height: number }> = {};

  if (groups.length <= 1) return updates;

  const sorted = [...groups].sort((a, b) =>
    direction === 'horizontal'
      ? a.position.x - b.position.x
      : a.position.y - b.position.y
  );

  for (let i = 0; i < sorted.length; i++) {
    const group = sorted[i]!;
    const start = i === 0 ? 0 : (splitterPositions[i - 1] ?? 0);
    const end = i === sorted.length - 1 ? 1 : (splitterPositions[i] ?? 1);
    const size = end - start;

    if (direction === 'horizontal') {
      updates[group.id] = {
        x: start,
        y: group.position.y,
        width: size,
        height: group.position.height,
      };
    } else {
      updates[group.id] = {
        x: group.position.x,
        y: start,
        width: group.position.width,
        height: size,
      };
    }
  }

  return updates;
}

/**
 * Computes group positions from mixed layout grid lines and cell mappings.
 * Converts visual grid line positions back to normalized 0-1 position coordinates.
 */
export function computePositionsFromMixedLines(
  groups: EditorGroupData[],
  mixedXLines: number[],
  mixedYLines: number[],
  mixedCells: Record<string, { colStart: number; colEnd: number; rowStart: number; rowEnd: number }>
): Record<string, { x: number; y: number; width: number; height: number }> {
  const updates: Record<string, { x: number; y: number; width: number; height: number }> = {};

  for (const group of groups) {
    const cell = mixedCells[group.id];
    if (!cell) continue;

    const x = mixedXLines[cell.colStart - 1] ?? 0;
    const xEnd = mixedXLines[cell.colEnd - 1] ?? 1;
    const y = mixedYLines[cell.rowStart - 1] ?? 0;
    const yEnd = mixedYLines[cell.rowEnd - 1] ?? 1;

    updates[group.id] = {
      x,
      y,
      width: xEnd - x,
      height: yEnd - y,
    };
  }

  return updates;
}
