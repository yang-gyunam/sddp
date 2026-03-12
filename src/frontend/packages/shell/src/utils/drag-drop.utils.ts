/**
 * Drag & Drop Utilities - Generic drag state management
 */

export interface DragState<T = unknown> {
  isDragging: boolean;
  draggedItem: T | null;
  dragStartPosition: { x: number; y: number } | null;
  currentPosition: { x: number; y: number } | null;
  dragDistance: number;
  dragThreshold: number;
}

export interface DropZoneConfig {
  element: HTMLElement;
  onDragEnter?: (e: DragEvent) => void;
  onDragOver?: (e: DragEvent) => void;
  onDragLeave?: (e: DragEvent) => void;
  onDrop?: (e: DragEvent) => void;
  acceptTypes?: string[];
}

const DEFAULT_THRESHOLD = 5;

/** Create initial drag state */
export function createDragState<T>(threshold = DEFAULT_THRESHOLD): DragState<T> {
  return {
    isDragging: false,
    draggedItem: null,
    dragStartPosition: null,
    currentPosition: null,
    dragDistance: 0,
    dragThreshold: threshold,
  };
}

/** Start dragging (returns new state) */
export function startDrag<T>(state: DragState<T>, item: T, x: number, y: number): DragState<T> {
  return {
    ...state,
    isDragging: true,
    draggedItem: item,
    dragStartPosition: { x, y },
    currentPosition: { x, y },
    dragDistance: 0,
  };
}

/** Update drag position (returns new state) */
export function updateDragPosition<T>(state: DragState<T>, x: number, y: number): DragState<T> {
  if (!state.isDragging || !state.dragStartPosition) return state;
  const dx = x - state.dragStartPosition.x;
  const dy = y - state.dragStartPosition.y;
  return {
    ...state,
    currentPosition: { x, y },
    dragDistance: Math.sqrt(dx * dx + dy * dy),
  };
}

/** End dragging (returns new state) */
export function endDrag<T>(state: DragState<T>): DragState<T> {
  return {
    ...state,
    isDragging: false,
    draggedItem: null,
    dragStartPosition: null,
    currentPosition: null,
    dragDistance: 0,
  };
}

/** Check if drag distance exceeds threshold (distinguish click vs drag) */
export function isDragThresholdExceeded<T>(state: DragState<T>): boolean {
  return state.dragDistance >= state.dragThreshold;
}

/** Set serialized data on DataTransfer */
export function setDragData(dataTransfer: DataTransfer, type: string, data: unknown): void {
  dataTransfer.setData(type, JSON.stringify(data));
}

/** Get deserialized data from DataTransfer */
export function getDragData<T = unknown>(dataTransfer: DataTransfer, type: string): T | null {
  const raw = dataTransfer.getData(type);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

/** Create a drop zone with event listeners, returns cleanup function */
export function createDropZone(config: DropZoneConfig): () => void {
  const { element, onDragEnter, onDragOver, onDragLeave, onDrop, acceptTypes } = config;

  const handleDragEnter = (e: DragEvent) => {
    e.preventDefault();
    onDragEnter?.(e);
  };

  const handleDragOver = (e: DragEvent) => {
    if (acceptTypes && e.dataTransfer) {
      const hasAccepted = acceptTypes.some((t) => e.dataTransfer!.types.includes(t));
      if (!hasAccepted) return;
    }
    e.preventDefault();
    onDragOver?.(e);
  };

  const handleDragLeave = (e: DragEvent) => {
    onDragLeave?.(e);
  };

  const handleDrop = (e: DragEvent) => {
    e.preventDefault();
    onDrop?.(e);
  };

  element.addEventListener('dragenter', handleDragEnter);
  element.addEventListener('dragover', handleDragOver);
  element.addEventListener('dragleave', handleDragLeave);
  element.addEventListener('drop', handleDrop);

  return () => {
    element.removeEventListener('dragenter', handleDragEnter);
    element.removeEventListener('dragover', handleDragOver);
    element.removeEventListener('dragleave', handleDragLeave);
    element.removeEventListener('drop', handleDrop);
  };
}
