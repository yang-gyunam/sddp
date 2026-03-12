/**
 * Tab Bar Scroll Utilities
 * Pure functions for tab bar horizontal scrolling logic
 */

export interface TabScrollState {
  scrollPosition: number;
  maxScrollPosition: number;
  hasOverflow: boolean;
}

export interface ScrollConfig {
  step?: number;
  padding?: number;
}

const DEFAULT_CONFIG: Required<ScrollConfig> = {
  step: 150,
  padding: 8,
};

/**
 * Calculate overflow state based on wrapper and inner widths
 */
export function calculateOverflow(
  wrapperWidth: number,
  innerWidth: number
): { hasOverflow: boolean; maxScrollPosition: number } {
  const hasOverflow = innerWidth > wrapperWidth;
  const maxScrollPosition = Math.max(0, innerWidth - wrapperWidth);

  return { hasOverflow, maxScrollPosition };
}

/**
 * Clamp scroll position within valid bounds
 */
export function clampScrollPosition(
  position: number,
  maxScrollPosition: number
): number {
  return Math.max(0, Math.min(maxScrollPosition, position));
}

/**
 * Calculate new scroll position when scrolling left
 */
export function scrollLeftPosition(
  currentPosition: number,
  config?: ScrollConfig
): number {
  const step = config?.step ?? DEFAULT_CONFIG.step;
  return Math.max(0, currentPosition - step);
}

/**
 * Calculate new scroll position when scrolling right
 */
export function scrollRightPosition(
  currentPosition: number,
  maxScrollPosition: number,
  config?: ScrollConfig
): number {
  const step = config?.step ?? DEFAULT_CONFIG.step;
  return Math.min(maxScrollPosition, currentPosition + step);
}

/**
 * Calculate scroll position to make a tab visible
 */
export function calculateScrollToTab(
  tabLeft: number,
  tabRight: number,
  wrapperWidth: number,
  currentScrollPosition: number,
  maxScrollPosition: number,
  config?: ScrollConfig
): number {
  const padding = config?.padding ?? DEFAULT_CONFIG.padding;

  // If tab is out of view on the left
  if (tabLeft < currentScrollPosition) {
    return Math.max(0, tabLeft - padding);
  }

  // If tab is out of view on the right
  if (tabRight > currentScrollPosition + wrapperWidth) {
    return Math.min(maxScrollPosition, tabRight - wrapperWidth + padding);
  }

  // Tab is already visible
  return currentScrollPosition;
}

/**
 * Calculate new scroll position from wheel delta
 */
export function calculateWheelScroll(
  currentPosition: number,
  delta: number,
  maxScrollPosition: number
): number {
  return clampScrollPosition(currentPosition + delta, maxScrollPosition);
}

/**
 * Check if can scroll left
 */
export function canScrollLeft(scrollPosition: number): boolean {
  return scrollPosition > 0;
}

/**
 * Check if can scroll right
 */
export function canScrollRight(
  scrollPosition: number,
  maxScrollPosition: number
): boolean {
  return scrollPosition < maxScrollPosition;
}
