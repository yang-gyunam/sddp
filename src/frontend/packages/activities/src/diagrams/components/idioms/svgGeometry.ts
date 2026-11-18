interface Position {
  x: number;
  y: number;
  width?: number;
  height?: number;
}
/** SVG adapter detail: no path or port data enters the common model. */
export function svgConnectionGeometry(start: Position, end: Position, self: boolean) {
  const sw = start.width ?? 160,
    sh = start.height ?? 50;
  const ew = end.width ?? 160,
    eh = end.height ?? 50;
  if (self)
    return {
      path: `M ${start.x + sw} ${start.y + sh * 0.2} C ${start.x + sw + 80} ${start.y - 50} ${start.x + sw + 80} ${start.y + sh + 50} ${start.x + sw} ${start.y + sh * 0.8}`,
      labelX: start.x + sw + 68,
      labelY: start.y + sh / 2 + 4,
      anchor: 'start' as const,
    };
  const dx = end.x + ew / 2 - start.x - sw / 2;
  const dy = end.y + eh / 2 - start.y - sh / 2;
  const horizontal = Math.abs(dx) / ((sw + ew) / 2) >= Math.abs(dy) / ((sh + eh) / 2);
  const sign = (horizontal ? dx : dy) < 0 ? -1 : 1;
  const from = horizontal
    ? { x: start.x + (sign > 0 ? sw : 0), y: start.y + sh / 2 }
    : { x: start.x + sw / 2, y: start.y + (sign > 0 ? sh : 0) };
  const to = horizontal
    ? { x: end.x + (sign > 0 ? 0 : ew), y: end.y + eh / 2 }
    : { x: end.x + ew / 2, y: end.y + (sign > 0 ? 0 : eh) };
  const bend = Math.max(20, Math.abs(horizontal ? to.x - from.x : to.y - from.y) / 2);
  const first = horizontal
    ? { x: from.x + sign * bend, y: from.y }
    : { x: from.x, y: from.y + sign * bend };
  const last = horizontal ? { x: to.x - sign * bend, y: to.y } : { x: to.x, y: to.y - sign * bend };
  return {
    path: `M ${from.x} ${from.y} C ${first.x} ${first.y} ${last.x} ${last.y} ${to.x} ${to.y}`,
    labelX: (from.x + to.x) / 2 + (horizontal ? 0 : 8),
    labelY: (from.y + to.y) / 2 + (horizontal ? -8 : 4),
    anchor: horizontal ? ('middle' as const) : ('start' as const),
  };
}
