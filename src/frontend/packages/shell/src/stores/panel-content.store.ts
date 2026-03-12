/**
 * Panel Content Store - Terminal, Problems, Output data management
 *
 * L-3 migration note:
 * This module keeps the public store contracts stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type {
  TerminalState,
  TerminalEntry,
  ProblemsState,
  ProblemEntry,
  ProblemSeverity,
  ProblemType,
  OutputState,
  OutputEntry,
  OutputLogLevel,
} from '../types';
import { panel } from './panel.store';
import {
  createRunesTerminalStore,
  createRunesProblemsStore,
  createRunesOutputStore,
} from './panel-content.store.runes';

export interface TerminalStoreOptions {
  now?: () => number;
  idFactory?: () => string;
}

export interface ProblemsEffects {
  updateProblemsBadge: (badge: number) => void;
}

export interface ProblemsStoreOptions {
  now?: () => number;
  idFactory?: () => string;
}

export interface OutputStoreOptions {
  now?: () => number;
  idFactory?: () => string;
}

export interface TerminalStoreLike {
  subscribe: (run: Subscriber<TerminalState>) => Unsubscriber;
  addEntry: (type: TerminalEntry['type'], content: string) => void;
  setRunning: (isRunning: boolean) => void;
  setCurrentCommand: (command: string) => void;
  clear: () => void;
  reset: () => void;
  get: () => TerminalState;
}

export interface ProblemsStoreLike {
  subscribe: (run: Subscriber<ProblemsState>) => Unsubscriber;
  addProblem: (problem: Omit<ProblemEntry, 'id' | 'timestamp'>) => void;
  addProblems: (problemList: Omit<ProblemEntry, 'id' | 'timestamp'>[]) => void;
  clearByType: (type: ProblemType) => void;
  removeProblem: (id: string) => void;
  clearBySource: (source: string) => void;
  clearBySeverity: (severity: ProblemSeverity) => void;
  clear: () => void;
  reset: () => void;
  get: () => ProblemsState;
}

export interface OutputStoreLike {
  subscribe: (run: Subscriber<OutputState>) => Unsubscriber;
  log: (level: OutputLogLevel, source: string, message: string, data?: unknown) => void;
  debug: (source: string, message: string, data?: unknown) => void;
  info: (source: string, message: string, data?: unknown) => void;
  warn: (source: string, message: string, data?: unknown) => void;
  error: (source: string, message: string, data?: unknown) => void;
  setFilter: (filter: OutputLogLevel | 'all') => void;
  clearBySource: (source: string) => void;
  clear: () => void;
  reset: () => void;
  get: () => OutputState;
}

type ProblemsEffectCall = {
  target: 'panel.updateTabBadge';
  args: ['problems', number];
};

type TerminalMismatchContext = {
  action: string;
  primaryState: TerminalState;
  shadowState: TerminalState;
};

type ProblemsMismatchContext = {
  action: string;
  primaryState: ProblemsState;
  shadowState: ProblemsState;
  primaryEffects: ProblemsEffectCall[];
  shadowEffects: ProblemsEffectCall[];
};

type OutputMismatchContext = {
  action: string;
  primaryState: OutputState;
  shadowState: OutputState;
};

const initialTerminalState: TerminalState = {
  entries: [],
  isRunning: false,
  currentCommand: '',
};

const initialProblemsState: ProblemsState = {
  entries: [],
  errorCount: 0,
  warningCount: 0,
  infoCount: 0,
};

const initialOutputState: OutputState = {
  entries: [],
  filter: 'all',
  sources: [],
};

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

function normalizeTerminalState(state: TerminalState): unknown {
  return {
    isRunning: state.isRunning,
    currentCommand: state.currentCommand,
    entries: state.entries.map((entry) => ({
      id: entry.id,
      type: entry.type,
      content: entry.content,
    })),
  };
}

function normalizeProblemsState(state: ProblemsState): unknown {
  return {
    errorCount: state.errorCount,
    warningCount: state.warningCount,
    infoCount: state.infoCount,
    entries: state.entries.map((entry) => ({
      id: entry.id,
      severity: entry.severity,
      type: entry.type ?? null,
      message: entry.message,
      source: entry.source,
      code: entry.code ?? null,
      file: entry.file ?? null,
      line: entry.line ?? null,
      column: entry.column ?? null,
    })),
  };
}

function normalizeOutputState(state: OutputState): unknown {
  return {
    filter: state.filter,
    sources: [...state.sources],
    entries: state.entries.map((entry) => ({
      id: entry.id,
      level: entry.level,
      source: entry.source,
      message: entry.message,
      data: entry.data ?? null,
    })),
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createPairedIdFactories(
  prefix: string,
  now: () => number
): { primary: () => string; shadow: () => string } {
  let counter = 0;
  const queue: string[] = [];

  return {
    primary: () => {
      const id = `${prefix}-${now()}-${++counter}`;
      queue.push(id);
      return id;
    },
    shadow: () => queue.shift() ?? `${prefix}-${now()}-${++counter}`,
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

function createWritableTerminalStore(options: TerminalStoreOptions = {}): TerminalStoreLike {
  const { subscribe, set, update } = writable<TerminalState>(cloneTerminalState(initialTerminalState));
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `term-${now()}-${++idCounter}`);

  let currentState = cloneTerminalState(initialTerminalState);
  subscribe((state) => {
    currentState = cloneTerminalState(state);
  });

  return {
    subscribe,
    addEntry: (type, content) => {
      update((state) => ({
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
      }));
    },
    setRunning: (isRunning) => {
      update((state) => ({ ...state, isRunning }));
    },
    setCurrentCommand: (command) => {
      update((state) => ({ ...state, currentCommand: command }));
    },
    clear: () => {
      update((state) => ({ ...state, entries: [] }));
    },
    reset: () => {
      set(cloneTerminalState(initialTerminalState));
    },
    get: () => cloneTerminalState(currentState),
  };
}

function createWritableProblemsStore(
  effects: ProblemsEffects,
  options: ProblemsStoreOptions = {}
): ProblemsStoreLike {
  const { subscribe, set, update } = writable<ProblemsState>(cloneProblemsState(initialProblemsState));
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `prob-${now()}-${++idCounter}`);

  let currentState = cloneProblemsState(initialProblemsState);
  subscribe((state) => {
    currentState = cloneProblemsState(state);
  });

  return {
    subscribe,
    addProblem: (problem) => {
      update((state) => {
        if (isDuplicate(state.entries, problem)) {
          return state;
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
        return { entries, ...counts };
      });
    },
    addProblems: (problemList) => {
      update((state) => {
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
          return state;
        }

        const counts = recalculateCounts(nextEntries);
        effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
        return { entries: nextEntries, ...counts };
      });
    },
    clearByType: (type) => {
      update((state) => {
        const entries = state.entries.filter((entry) => entry.type !== type);
        const counts = recalculateCounts(entries);
        effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
        return { entries, ...counts };
      });
    },
    removeProblem: (id) => {
      update((state) => {
        const entries = state.entries.filter((entry) => entry.id !== id);
        const counts = recalculateCounts(entries);
        effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
        return { entries, ...counts };
      });
    },
    clearBySource: (source) => {
      update((state) => {
        const entries = state.entries.filter((entry) => entry.source !== source);
        const counts = recalculateCounts(entries);
        effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
        return { entries, ...counts };
      });
    },
    clearBySeverity: (severity) => {
      update((state) => {
        const entries = state.entries.filter((entry) => entry.severity !== severity);
        const counts = recalculateCounts(entries);
        effects.updateProblemsBadge(counts.errorCount + counts.warningCount);
        return { entries, ...counts };
      });
    },
    clear: () => {
      effects.updateProblemsBadge(0);
      set(cloneProblemsState(initialProblemsState));
    },
    reset: () => {
      effects.updateProblemsBadge(0);
      set(cloneProblemsState(initialProblemsState));
    },
    get: () => cloneProblemsState(currentState),
  };
}

function createWritableOutputStore(options: OutputStoreOptions = {}): OutputStoreLike {
  const { subscribe, set, update } = writable<OutputState>(cloneOutputState(initialOutputState));
  const now = options.now ?? Date.now;
  let idCounter = 0;
  const idFactory = options.idFactory ?? (() => `out-${now()}-${++idCounter}`);

  let currentState = cloneOutputState(initialOutputState);
  subscribe((state) => {
    currentState = cloneOutputState(state);
  });

  function log(level: OutputLogLevel, source: string, message: string, data?: unknown): void {
    update((state) => {
      const entry: OutputEntry = {
        id: idFactory(),
        level,
        source,
        message,
        timestamp: new Date(now()),
        data,
      };
      const sources = state.sources.includes(source) ? state.sources : [...state.sources, source];
      return {
        ...state,
        entries: [...state.entries, entry],
        sources,
      };
    });
  }

  return {
    subscribe,
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
      update((state) => ({ ...state, filter }));
    },
    clearBySource: (source) => {
      update((state) => ({
        ...state,
        entries: state.entries.filter((entry) => entry.source !== source),
      }));
    },
    clear: () => {
      update((state) => ({ ...state, entries: [] }));
    },
    reset: () => {
      set(cloneOutputState(initialOutputState));
    },
    get: () => cloneOutputState(currentState),
  };
}

function createRecordingProblemsEffects(
  mode: 'active' | 'shadow',
  effectLog: ProblemsEffectCall[]
): ProblemsEffects {
  return {
    updateProblemsBadge: (badge) => {
      effectLog.push({ target: 'panel.updateTabBadge', args: ['problems', badge] });
      if (mode === 'active') {
        panel.updateTabBadge('problems', badge);
      }
    },
  };
}

function createParityAwareTerminalStore(
  primary: TerminalStoreLike,
  shadow: TerminalStoreLike,
  reportMismatch: (context: TerminalMismatchContext) => void
): TerminalStoreLike {
  const listeners = new Set<Subscriber<TerminalState>>();
  let currentState = primary.get();

  function compare(action: string): void {
    const primaryState = primary.get();
    const shadowState = shadow.get();
    if (!areEqual(normalizeTerminalState(primaryState), normalizeTerminalState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  primary.subscribe((state) => {
    currentState = cloneTerminalState(state);
    for (const listener of listeners) {
      listener(cloneTerminalState(state));
    }
  });

  return {
    subscribe: (run) => {
      run(cloneTerminalState(currentState));
      listeners.add(run);
      return () => {
        listeners.delete(run);
      };
    },
    addEntry: (type, content) => {
      primary.addEntry(type, content);
      shadow.addEntry(type, content);
      compare('addEntry');
    },
    setRunning: (isRunning) => {
      primary.setRunning(isRunning);
      shadow.setRunning(isRunning);
      compare('setRunning');
    },
    setCurrentCommand: (command) => {
      primary.setCurrentCommand(command);
      shadow.setCurrentCommand(command);
      compare('setCurrentCommand');
    },
    clear: () => {
      primary.clear();
      shadow.clear();
      compare('clear');
    },
    reset: () => {
      primary.reset();
      shadow.reset();
      compare('reset');
    },
    get: () => cloneTerminalState(currentState),
  };
}

function createParityAwareProblemsStore(
  primary: ProblemsStoreLike,
  shadow: ProblemsStoreLike,
  primaryEffectsLog: ProblemsEffectCall[],
  shadowEffectsLog: ProblemsEffectCall[],
  reportMismatch: (context: ProblemsMismatchContext) => void
): ProblemsStoreLike {
  const listeners = new Set<Subscriber<ProblemsState>>();
  let currentState = primary.get();

  function compare(action: string): void {
    const primaryState = primary.get();
    const shadowState = shadow.get();
    if (
      !areEqual(normalizeProblemsState(primaryState), normalizeProblemsState(shadowState)) ||
      !areEqual(primaryEffectsLog, shadowEffectsLog)
    ) {
      reportMismatch({
        action,
        primaryState,
        shadowState,
        primaryEffects: [...primaryEffectsLog],
        shadowEffects: [...shadowEffectsLog],
      });
    }
  }

  primary.subscribe((state) => {
    currentState = cloneProblemsState(state);
    for (const listener of listeners) {
      listener(cloneProblemsState(state));
    }
  });

  return {
    subscribe: (run) => {
      run(cloneProblemsState(currentState));
      listeners.add(run);
      return () => {
        listeners.delete(run);
      };
    },
    addProblem: (problem) => {
      primary.addProblem(problem);
      shadow.addProblem(problem);
      compare('addProblem');
    },
    addProblems: (problemList) => {
      primary.addProblems(problemList);
      shadow.addProblems(problemList);
      compare('addProblems');
    },
    clearByType: (type) => {
      primary.clearByType(type);
      shadow.clearByType(type);
      compare('clearByType');
    },
    removeProblem: (id) => {
      primary.removeProblem(id);
      shadow.removeProblem(id);
      compare('removeProblem');
    },
    clearBySource: (source) => {
      primary.clearBySource(source);
      shadow.clearBySource(source);
      compare('clearBySource');
    },
    clearBySeverity: (severity) => {
      primary.clearBySeverity(severity);
      shadow.clearBySeverity(severity);
      compare('clearBySeverity');
    },
    clear: () => {
      primary.clear();
      shadow.clear();
      compare('clear');
    },
    reset: () => {
      primary.reset();
      shadow.reset();
      compare('reset');
    },
    get: () => cloneProblemsState(currentState),
  };
}

function createParityAwareOutputStore(
  primary: OutputStoreLike,
  shadow: OutputStoreLike,
  reportMismatch: (context: OutputMismatchContext) => void
): OutputStoreLike {
  const listeners = new Set<Subscriber<OutputState>>();
  let currentState = primary.get();

  function compare(action: string): void {
    const primaryState = primary.get();
    const shadowState = shadow.get();
    if (!areEqual(normalizeOutputState(primaryState), normalizeOutputState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  primary.subscribe((state) => {
    currentState = cloneOutputState(state);
    for (const listener of listeners) {
      listener(cloneOutputState(state));
    }
  });

  return {
    subscribe: (run) => {
      run(cloneOutputState(currentState));
      listeners.add(run);
      return () => {
        listeners.delete(run);
      };
    },
    log: (level, source, message, data) => {
      primary.log(level, source, message, data);
      shadow.log(level, source, message, data);
      compare('log');
    },
    debug: (source, message, data) => {
      primary.debug(source, message, data);
      shadow.debug(source, message, data);
      compare('debug');
    },
    info: (source, message, data) => {
      primary.info(source, message, data);
      shadow.info(source, message, data);
      compare('info');
    },
    warn: (source, message, data) => {
      primary.warn(source, message, data);
      shadow.warn(source, message, data);
      compare('warn');
    },
    error: (source, message, data) => {
      primary.error(source, message, data);
      shadow.error(source, message, data);
      compare('error');
    },
    setFilter: (filter) => {
      primary.setFilter(filter);
      shadow.setFilter(filter);
      compare('setFilter');
    },
    clearBySource: (source) => {
      primary.clearBySource(source);
      shadow.clearBySource(source);
      compare('clearBySource');
    },
    clear: () => {
      primary.clear();
      shadow.clear();
      compare('clear');
    },
    reset: () => {
      primary.reset();
      shadow.reset();
      compare('reset');
    },
    get: () => cloneOutputState(currentState),
  };
}

function createStores(): {
  terminal: TerminalStoreLike;
  problems: ProblemsStoreLike;
  output: OutputStoreLike;
} {
  const enableRunesPanelContentStore = config.isRunesPanelContentStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  if (!enableRunesPanelContentStore && !enableShadowParity) {
    return {
      terminal: createWritableTerminalStore(),
      problems: createWritableProblemsStore(createRecordingProblemsEffects('active', [])),
      output: createWritableOutputStore(),
    };
  }

  const now = Date.now;
  const terminalIdFactories = createPairedIdFactories('term', now);
  const problemsIdFactories = createPairedIdFactories('prob', now);
  const outputIdFactories = createPairedIdFactories('out', now);

  const primaryProblemEffectsLog: ProblemsEffectCall[] = [];
  const shadowProblemEffectsLog: ProblemsEffectCall[] = [];

  const writableTerminalStore = createWritableTerminalStore({
    now,
    idFactory: enableRunesPanelContentStore ? terminalIdFactories.shadow : terminalIdFactories.primary,
  });
  const runesTerminalStore = createRunesTerminalStore({
    initialState: initialTerminalState,
    now,
    idFactory: enableRunesPanelContentStore ? terminalIdFactories.primary : terminalIdFactories.shadow,
  });

  const writableProblemsStore = createWritableProblemsStore(
    createRecordingProblemsEffects(enableRunesPanelContentStore ? 'shadow' : 'active', enableRunesPanelContentStore ? shadowProblemEffectsLog : primaryProblemEffectsLog),
    {
      now,
      idFactory: enableRunesPanelContentStore ? problemsIdFactories.shadow : problemsIdFactories.primary,
    }
  );
  const runesProblemsStore = createRunesProblemsStore(
    createRecordingProblemsEffects(enableRunesPanelContentStore ? 'active' : 'shadow', enableRunesPanelContentStore ? primaryProblemEffectsLog : shadowProblemEffectsLog),
    {
      initialState: initialProblemsState,
      now,
      idFactory: enableRunesPanelContentStore ? problemsIdFactories.primary : problemsIdFactories.shadow,
    }
  );

  const writableOutputStore = createWritableOutputStore({
    now,
    idFactory: enableRunesPanelContentStore ? outputIdFactories.shadow : outputIdFactories.primary,
  });
  const runesOutputStore = createRunesOutputStore({
    initialState: initialOutputState,
    now,
    idFactory: enableRunesPanelContentStore ? outputIdFactories.primary : outputIdFactories.shadow,
  });

  const primaryTerminalStore = enableRunesPanelContentStore ? runesTerminalStore : writableTerminalStore;
  const shadowTerminalStore = enableRunesPanelContentStore ? writableTerminalStore : runesTerminalStore;
  const primaryProblemsStore = enableRunesPanelContentStore ? runesProblemsStore : writableProblemsStore;
  const shadowProblemsStore = enableRunesPanelContentStore ? writableProblemsStore : runesProblemsStore;
  const primaryOutputStore = enableRunesPanelContentStore ? runesOutputStore : writableOutputStore;
  const shadowOutputStore = enableRunesPanelContentStore ? writableOutputStore : runesOutputStore;

  return {
    terminal: enableShadowParity
      ? createParityAwareTerminalStore(primaryTerminalStore, shadowTerminalStore, (context) => {
          console.error('[panel-content.store] terminal parity mismatch', context);
        })
      : primaryTerminalStore,
    problems: enableShadowParity
      ? createParityAwareProblemsStore(
          primaryProblemsStore,
          shadowProblemsStore,
          primaryProblemEffectsLog,
          shadowProblemEffectsLog,
          (context) => {
            console.error('[panel-content.store] problems parity mismatch', context);
          }
        )
      : primaryProblemsStore,
    output: enableShadowParity
      ? createParityAwareOutputStore(primaryOutputStore, shadowOutputStore, (context) => {
          console.error('[panel-content.store] output parity mismatch', context);
        })
      : primaryOutputStore,
  };
}

const stores = createStores();

export const terminal = stores.terminal;
export const problems = stores.problems;
export const output = stores.output;

export const __internal = {
  initialTerminalState,
  initialProblemsState,
  initialOutputState,
  cloneTerminalState,
  cloneProblemsState,
  cloneOutputState,
  normalizeTerminalState,
  normalizeProblemsState,
  normalizeOutputState,
  createPairedIdFactories,
  createWritableTerminalStore,
  createWritableProblemsStore,
  createWritableOutputStore,
  createRunesTerminalStore,
  createRunesProblemsStore,
  createRunesOutputStore,
  createRecordingProblemsEffects,
  createParityAwareTerminalStore,
  createParityAwareProblemsStore,
  createParityAwareOutputStore,
};
