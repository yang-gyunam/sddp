import type { Subscriber, Unsubscriber } from 'svelte/store';
import type {
  TerminalEntry,
  TerminalState,
  ProblemEntry,
  ProblemsState,
  ProblemSeverity,
  ProblemType,
  OutputEntry,
  OutputLogLevel,
  OutputState,
} from '../types';
import type {
  OutputStoreLike,
  OutputStoreOptions,
  ProblemsEffects,
  ProblemsStoreLike,
  ProblemsStoreOptions,
  TerminalStoreLike,
  TerminalStoreOptions,
} from './panel-content.store';

function cloneTerminalEntry(entry: TerminalEntry): TerminalEntry {
  return {
    ...entry,
    timestamp: new Date(entry.timestamp.getTime()),
  };
}

function cloneTerminalState(state: TerminalState): TerminalState {
  return {
    ...state,
    entries: state.entries.map(cloneTerminalEntry),
  };
}

function cloneProblemEntry(entry: ProblemEntry): ProblemEntry {
  return {
    ...entry,
    timestamp: new Date(entry.timestamp.getTime()),
  };
}

function cloneProblemsState(state: ProblemsState): ProblemsState {
  return {
    ...state,
    entries: state.entries.map(cloneProblemEntry),
  };
}

function cloneOutputEntry(entry: OutputEntry): OutputEntry {
  return {
    ...entry,
    timestamp: new Date(entry.timestamp.getTime()),
  };
}

function cloneOutputState(state: OutputState): OutputState {
  return {
    ...state,
    entries: state.entries.map(cloneOutputEntry),
    sources: [...state.sources],
  };
}

function recalculateCounts(entries: ProblemEntry[]) {
  return {
    errorCount: entries.filter((entry) => entry.severity === 'error').length,
    warningCount: entries.filter((entry) => entry.severity === 'warning').length,
    infoCount: entries.filter((entry) => entry.severity === 'info' || entry.severity === 'hint').length,
  };
}

function isDuplicate(entries: ProblemEntry[], problem: Omit<ProblemEntry, 'id' | 'timestamp'>): boolean {
  if (problem.source === 'Toast' || problem.source === 'SignalR') {
    return false;
  }

  return entries.some(
    (entry) =>
      entry.message === problem.message &&
      entry.source === problem.source &&
      entry.file === problem.file &&
      entry.line === problem.line &&
      entry.code === problem.code
  );
}

export function createRunesTerminalStore(options: {
  initialState: TerminalState;
} & TerminalStoreOptions): TerminalStoreLike {
  let state = cloneTerminalState(options.initialState);
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `term-${now()}-${++idCounter}`);
  const subscribers = new Set<Subscriber<TerminalState>>();

  function snapshot(): TerminalState {
    return cloneTerminalState(state);
  }

  function commit(nextState: TerminalState): void {
    state = cloneTerminalState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<TerminalState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    addEntry: (type, content) => {
      commit({
        ...state,
        entries: [
          ...state.entries,
          {
            id: idFactory(),
            type,
            content,
            timestamp: new Date(now()),
          },
        ],
      });
    },
    setRunning: (isRunning) => {
      commit({ ...state, isRunning });
    },
    setCurrentCommand: (command) => {
      commit({ ...state, currentCommand: command });
    },
    clear: () => {
      commit({ ...state, entries: [] });
    },
    reset: () => {
      commit(options.initialState);
    },
    get: () => snapshot(),
  };
}

export function createRunesProblemsStore(
  effects: ProblemsEffects,
  options: {
    initialState: ProblemsState;
  } & ProblemsStoreOptions
): ProblemsStoreLike {
  let state = cloneProblemsState(options.initialState);
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `prob-${now()}-${++idCounter}`);
  const subscribers = new Set<Subscriber<ProblemsState>>();

  function snapshot(): ProblemsState {
    return cloneProblemsState(state);
  }

  function commit(nextState: ProblemsState): void {
    state = cloneProblemsState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<ProblemsState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    addProblem: (problem) => {
      if (isDuplicate(state.entries, problem)) {
        return;
      }

      const entries = [
        ...state.entries,
        {
          ...problem,
          id: idFactory(),
          timestamp: new Date(now()),
        },
      ];
      const counts = recalculateCounts(entries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries, ...counts });
    },
    addProblems: (problemList) => {
      const nextEntries = [...state.entries];
      let changed = false;

      for (const problem of problemList) {
        if (isDuplicate(nextEntries, problem)) {
          continue;
        }

        nextEntries.push({
          ...problem,
          id: idFactory(),
          timestamp: new Date(now()),
        });
        changed = true;
      }

      if (!changed) {
        return;
      }

      const counts = recalculateCounts(nextEntries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries: nextEntries, ...counts });
    },
    clearByType: (type: ProblemType) => {
      const entries = state.entries.filter((entry) => entry.type !== type);
      const counts = recalculateCounts(entries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries, ...counts });
    },
    removeProblem: (id: string) => {
      const entries = state.entries.filter((entry) => entry.id !== id);
      const counts = recalculateCounts(entries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries, ...counts });
    },
    clearBySource: (source: string) => {
      const entries = state.entries.filter((entry) => entry.source !== source);
      const counts = recalculateCounts(entries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries, ...counts });
    },
    clearBySeverity: (severity: ProblemSeverity) => {
      const entries = state.entries.filter((entry) => entry.severity !== severity);
      const counts = recalculateCounts(entries);
      effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
      commit({ entries, ...counts });
    },
    clear: () => {
      effects.updateProblemsBadge(0);
      commit(options.initialState);
    },
    reset: () => {
      effects.updateProblemsBadge(0);
      commit(options.initialState);
    },
    get: () => snapshot(),
  };
}

export function createRunesOutputStore(options: {
  initialState: OutputState;
} & OutputStoreOptions): OutputStoreLike {
  let state = cloneOutputState(options.initialState);
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `out-${now()}-${++idCounter}`);
  const subscribers = new Set<Subscriber<OutputState>>();

  function snapshot(): OutputState {
    return cloneOutputState(state);
  }

  function commit(nextState: OutputState): void {
    state = cloneOutputState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  function log(level: OutputLogLevel, source: string, message: string, data?: unknown): void {
    const entry: OutputEntry = {
      id: idFactory(),
      level,
      source,
      message,
      timestamp: new Date(now()),
      data,
    };
    const sources = state.sources.includes(source) ? state.sources : [...state.sources, source];
    commit({
      ...state,
      entries: [...state.entries, entry],
      sources,
    });
  }

  return {
    subscribe: (run: Subscriber<OutputState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    log,
    debug: (source, message, data) => {
      log('debug', source, message, data);
    },
    info: (source, message, data) => {
      log('info', source, message, data);
    },
    warn: (source, message, data) => {
      log('warn', source, message, data);
    },
    error: (source, message, data) => {
      log('error', source, message, data);
    },
    setFilter: (filter) => {
      commit({ ...state, filter });
    },
    clearBySource: (source) => {
      commit({
        ...state,
        entries: state.entries.filter((entry) => entry.source !== source),
      });
    },
    clear: () => {
      commit({ ...state, entries: [] });
    },
    reset: () => {
      commit(options.initialState);
    },
    get: () => snapshot(),
  };
}
