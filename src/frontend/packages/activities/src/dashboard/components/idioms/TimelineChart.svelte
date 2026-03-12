<script lang="ts">
  /**
   * Timeline Chart
   * SVG-based horizontal due-date timeline chart.
   * Center = Today, left = overdue, right = upcoming.
   * Tasks are clamped to [-30, +30] days range.
   */
  import type { TimelineTask } from '../../types';

  interface Props {
    tasks?: TimelineTask[];
    title?: string;
    onTaskClick?: (taskId: string) => void;
  }

  let { tasks = [], title = '', onTaskClick }: Props = $props();

  // ---- Layout constants ------------------------------------------------
  const SVG_HEIGHT = 120;
  const PADDING_X = 48;   // horizontal padding for axis labels
  const PADDING_TOP = 28; // space above the axis line (for labels)
  const AXIS_Y = 72;      // y position of the centre axis line
  const DOT_RADIUS = 6;
  const DOT_RADIUS_HOVER = 8;
  const DAY_MIN = -30;
  const DAY_MAX = 30;

  // Axis tick marks shown as labels
  const AXIS_TICKS = [-30, -14, -7, 0, 7, 14, 30];

  // ---- Reactive width tracking ----------------------------------------
  let svgEl = $state<SVGSVGElement | null>(null);
  let svgWidth = $state(600);

  // ResizeObserver to keep svgWidth in sync
  $effect(() => {
    if (!svgEl) return;
    const obs = new ResizeObserver((entries) => {
      for (const entry of entries) {
        svgWidth = entry.contentRect.width || 600;
      }
    });
    obs.observe(svgEl);
    return () => obs.disconnect();
  });

  // ---- Helper: map daysFromToday → SVG x coordinate ------------------
  function dayToX(days: number): number {
    const clamped = Math.max(DAY_MIN, Math.min(DAY_MAX, days));
    const ratio = (clamped - DAY_MIN) / (DAY_MAX - DAY_MIN);
    return PADDING_X + ratio * (svgWidth - PADDING_X * 2);
  }

  // ---- Derived: position + colour for each task dot ------------------
  const dotData = $derived(
    tasks.map((task) => {
      const x = dayToX(task.daysFromToday);
      let fill: string;
      if (task.isOverdue) {
        fill = 'var(--color-error-500)';
      } else if (task.daysFromToday <= 3) {
        fill = 'var(--color-warning-500)';
      } else {
        fill = 'var(--color-success-500)';
      }
      return { task, x, fill };
    })
  );

  // ---- Tooltip state --------------------------------------------------
  let hoveredTaskId = $state<string | null>(null);

  const hoveredDot = $derived(
    hoveredTaskId ? dotData.find((d) => d.task.taskId === hoveredTaskId) ?? null : null
  );

  // Format ISO date string to readable form (YYYY-MM-DD → MM/DD)
  function formatDate(dateStr: string | null): string {
    if (!dateStr) return 'No due date';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  // ---- Axis tick helpers ----------------------------------------------
  function tickLabel(days: number): string {
    if (days === 0) return 'Today';
    return days > 0 ? `+${days}d` : `${days}d`;
  }
</script>

<div class="timeline-chart">
  {#if title}
    <h3>{title}</h3>
  {/if}

  {#if tasks.length > 0}
    <div class="svg-wrapper">
      <svg
        bind:this={svgEl}
        width="100%"
        height={SVG_HEIGHT}
        role="img"
        aria-label="Due date timeline chart"
        onmouseleave={() => { hoveredTaskId = null; }}
      >
        <!-- Axis baseline -->
        <line
          x1={PADDING_X}
          y1={AXIS_Y}
          x2={svgWidth - PADDING_X}
          y2={AXIS_Y}
          stroke="var(--color-border)"
          stroke-width="1"
        />

        <!-- Overdue region shading -->
        <rect
          x={PADDING_X}
          y={PADDING_TOP}
          width={dayToX(0) - PADDING_X}
          height={AXIS_Y - PADDING_TOP}
          fill="var(--color-error-500)"
          fill-opacity="0.04"
          rx="2"
        />

        <!-- Axis ticks and labels -->
        {#each AXIS_TICKS as days (days)}
          {@const tx = dayToX(days)}
          {@const isToday = days === 0}
          <line
            x1={tx}
            y1={AXIS_Y - (isToday ? 10 : 5)}
            x2={tx}
            y2={AXIS_Y + (isToday ? 10 : 5)}
            stroke={isToday ? 'var(--color-accent-primary)' : 'var(--color-border)'}
            stroke-width={isToday ? 2 : 1}
          />
          <text
            x={tx}
            y={AXIS_Y + 22}
            text-anchor="middle"
            class="axis-label {isToday ? 'today-label' : ''}"
          >
            {tickLabel(days)}
          </text>
        {/each}

        <!-- "Today" vertical line -->
        <line
          x1={dayToX(0)}
          y1={PADDING_TOP - 8}
          x2={dayToX(0)}
          y2={AXIS_Y - 10}
          stroke="var(--color-accent-primary)"
          stroke-width="1.5"
          stroke-dasharray="3 2"
        />

        <!-- Task dots -->
        {#each dotData as { task, x, fill } (task.taskId)}
          <circle
            cx={x}
            cy={AXIS_Y}
            r={hoveredTaskId === task.taskId ? DOT_RADIUS_HOVER : DOT_RADIUS}
            {fill}
            stroke="var(--color-bg-primary)"
            stroke-width="2"
            class="dot"
            role="button"
            aria-label="{task.title} — due {formatDate(task.dueDate)}"
            tabindex="0"
            onmouseenter={() => {
              hoveredTaskId = task.taskId;
            }}
            onclick={() => onTaskClick?.(task.taskId)}
            onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onTaskClick?.(task.taskId); } }}
          />
        {/each}

        <!-- Tooltip (rendered last so it draws on top) -->
        {#if hoveredDot}
          {@const { task, x } = hoveredDot}
          {@const TW = 180}
          {@const TH = 74}
          {@const TX = Math.max(PADDING_X, Math.min(x - TW / 2, svgWidth - PADDING_X - TW))}
          {@const TY = AXIS_Y - DOT_RADIUS_HOVER - 8 - TH}

          <!-- Tooltip shadow + background -->
          <rect
            x={TX}
            y={TY}
            width={TW}
            height={TH}
            rx="6"
            fill="var(--color-bg-elevated)"
            stroke="var(--color-border)"
            stroke-width="1"
            filter="drop-shadow(0 2px 6px rgba(0,0,0,0.18))"
          />

          <!-- Tooltip content: title -->
          <text x={TX + 10} y={TY + 16} class="tooltip-title">
            {task.title.length > 22 ? task.title.slice(0, 22) + '…' : task.title}
          </text>

          <!-- Tooltip content: status / priority -->
          <text x={TX + 10} y={TY + 32} class="tooltip-meta">
            {task.status} · {task.priority}
          </text>

          <!-- Tooltip content: due date -->
          <text x={TX + 10} y={TY + 48} class="tooltip-meta">
            Due: {formatDate(task.dueDate)}
          </text>

          <!-- Tooltip content: project -->
          {#if task.projectName}
            <text x={TX + 10} y={TY + 64} class="tooltip-project">
              {task.projectName.length > 24 ? task.projectName.slice(0, 24) + '…' : task.projectName}
            </text>
          {/if}

          <!-- Connector line from tooltip to dot -->
          <line
            x1={x}
            y1={TY + TH}
            x2={x}
            y2={AXIS_Y - DOT_RADIUS_HOVER}
            stroke="var(--color-border)"
            stroke-width="1"
            stroke-dasharray="2 2"
          />
        {/if}
      </svg>
    </div>

    <!-- Summary counts -->
    <div class="summary-row">
      <span class="summary-badge overdue">
        <span class="dot-indicator"></span>
        {tasks.filter((t) => t.isOverdue).length} overdue
      </span>
      <span class="summary-badge upcoming-soon">
        <span class="dot-indicator"></span>
        {tasks.filter((t) => !t.isOverdue && t.daysFromToday <= 3).length} due soon
      </span>
      <span class="summary-badge upcoming">
        <span class="dot-indicator"></span>
        {tasks.filter((t) => !t.isOverdue && t.daysFromToday > 3).length} upcoming
      </span>
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-calendar"></span>
      <span>No tasks with due dates</span>
    </div>
  {/if}
</div>

<style>
  .timeline-chart {
    padding: 1.5rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
    min-height: 180px;
  }

  h3 {
    margin: 0 0 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
  }

  .svg-wrapper {
    width: 100%;
    overflow: visible;
  }

  /* SVG text styles via global selectors scoped to this component */
  .axis-label {
    font-size: 10px;
    fill: var(--color-text-tertiary);
    font-family: var(--font-family, sans-serif);
    user-select: none;
  }

  .today-label {
    fill: var(--color-accent-primary);
    font-weight: 600;
  }

  .dot {
    cursor: pointer;
    transition: r 0.15s ease;
  }

  .dot:focus {
    outline: none;
  }

  .dot:focus-visible {
    outline: 2px solid var(--color-accent-primary);
    outline-offset: 3px;
  }

  .tooltip-title {
    font-size: 11px;
    font-weight: 600;
    fill: var(--color-text-primary);
    font-family: var(--font-family, sans-serif);
  }

  .tooltip-meta {
    font-size: 10px;
    fill: var(--color-text-secondary);
    font-family: var(--font-family, sans-serif);
  }

  .tooltip-project {
    font-size: 10px;
    fill: var(--color-text-tertiary);
    font-family: var(--font-family, sans-serif);
    font-style: italic;
  }

  /* Summary row below chart */
  .summary-row {
    display: flex;
    gap: 1rem;
    margin-top: 0.75rem;
    flex-wrap: wrap;
  }

  .summary-badge {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.75rem;
    color: var(--color-text-secondary);
  }

  .dot-indicator {
    display: inline-block;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .overdue .dot-indicator {
    background: var(--color-error-500);
  }

  .upcoming-soon .dot-indicator {
    background: var(--color-warning-500);
  }

  .upcoming .dot-indicator {
    background: var(--color-success-500);
  }

  /* Empty state */
  .empty-chart {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 120px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
  }

  .empty-chart .codicon {
    font-size: 2rem;
    opacity: 0.5;
  }
</style>
