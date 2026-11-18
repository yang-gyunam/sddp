export interface NodeBounds {
  x: number;
  y: number;
  width: number;
  height: number;
}
export const resizeHandles = ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w'] as const;
export type ResizeHandle = (typeof resizeHandles)[number];
export const nodeSize = { width: 160, height: 50, minWidth: 80, minHeight: 32, max: 10000 };
const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));
/** Keep the opposite boundary fixed; crossing a boundary clamps rather than flips. */
export function resizedBounds(
  start: NodeBounds,
  handle: ResizeHandle,
  dx: number,
  dy: number
): NodeBounds {
  const next = { ...start };
  if (handle.includes('w')) {
    const right = start.x + start.width;
    next.width = clamp(
      start.width - dx,
      Math.max(nodeSize.minWidth, right - 100000),
      Math.min(nodeSize.max, right + 100000)
    );
    next.x = right - next.width;
  } else if (handle.includes('e'))
    next.width = clamp(start.width + dx, nodeSize.minWidth, nodeSize.max);
  if (handle.includes('n')) {
    const bottom = start.y + start.height;
    next.height = clamp(
      start.height - dy,
      Math.max(nodeSize.minHeight, bottom - 100000),
      Math.min(nodeSize.max, bottom + 100000)
    );
    next.y = bottom - next.height;
  } else if (handle.includes('s'))
    next.height = clamp(start.height + dy, nodeSize.minHeight, nodeSize.max);
  return next;
}
export function resizeHandlePoint(node: NodeBounds, handle: ResizeHandle) {
  return {
    x: node.x + (handle.includes('w') ? 0 : handle.includes('e') ? node.width : node.width / 2),
    y: node.y + (handle.includes('n') ? 0 : handle.includes('s') ? node.height : node.height / 2),
  };
}
