<script lang="ts">
  import { onMount, onDestroy, untrack, tick } from 'svelte';
  import { Button, Input, Spinner, Select } from '@sddp/ui';
  import {
    createConfirmation,
    getTabState,
    setTabState,
    registerTabCloseGuard,
    tabActions,
    SurfaceCard,
    CodeDiffViewer,
    PageBody,
  } from '@sddp/shell';
  import { authStore } from '@sddp/shell/auth';
  import { createProjectCapabilityStore } from '../../../shared/state/projectCapabilities';
  import { ApiRequestError, AccessDeniedError } from '../../../shared/api/fetchWithAuth';
  import {
    createDiagram2 as createDiagram,
    saveDiagram2 as saveDiagram,
    getDiagramVersions2 as getDiagramVersions,
    getDiagramVersion2 as getDiagramVersion,
    compareDiagramVersions2 as compareDiagramVersions,
    decodeDiagramYaml2 as decodeDiagramYaml,
    encodeDiagramYaml2 as encodeDiagramYaml,
    projectDiagram2 as projectDiagram,
    previewDiagramSource2,
    listDiagramParsers2,
  } from '../../services/DiagramService2';
  import { diagramFileContent } from '../../services/DiagramService';
  import {
    commonEditor2 as commonEditor,
    stencils2,
    type DiagramCommand2 as DiagramCommand,
    type DiagramEditorAdapter2 as DiagramEditorAdapter,
  } from '../../model/editor2';
  import {
    clone2 as cloneModel,
    emptyModel2 as emptyModel,
    elements2,
    getElement2,
    owner2,
    promoteModel1,
    refKey2,
    validate2,
  } from '../../model/model2';
  import { modelKey } from '../../model/editor';
  import { projectScene2, connectable2 } from '../../model/scene2';
  import {
    diagramRenderers2 as diagramRenderers,
    resolveRenderer2 as resolveRenderer,
  } from '../../model/renderers2';
  import type {
    ElementRef as DiagramSelection,
    DiagramV2Dto as DiagramDto,
    DiagramRevision2 as DiagramRevision,
    DiagramV2ComparisonDto as DiagramComparisonDto,
    DiagramIdentityPreview2,
    DiagramIdentityResolution2,
    DiagramSourcePreviewDto,
    DiagramType2,
    DiagramParserCapability2,
  } from '../../types2';
  import type { DiagramVersionListItemDto } from '../../types';
  import {
    searchEntities,
    type EntitySearchResult,
  } from '../../../relationship/services/RelationshipService';
  import type { EntityType } from '../../../relationship/types';
  import type { DiagramOrigin } from '../../types';
  import DiagramDataTools from '../idioms/DiagramDataTools.svelte';
  import DiagramInspector from '../idioms/DiagramInspector2.svelte';
  import DiagramActions from '../idioms/DiagramActions.svelte';
  import DiagramPalette from '../idioms/DiagramPalette2.svelte';

  import DiagramSourceTools2 from '../idioms/DiagramSourceTools2.svelte';
  let {
    diagram,
    projectId,
    tabId = '',
    onsaved,
    onreaddenied,
    editor = commonEditor,
  }: {
    diagram?: DiagramDto;
    projectId: string;
    tabId?: string;
    onsaved: (saved: DiagramDto) => void;
    onreaddenied?: () => void;
    editor?: DiagramEditorAdapter;
  } = $props();
  interface Draft {
    scope: string;
    title: string;
    model: DiagramRevision;
    baseTitle: string;
    baseModel: DiagramRevision;
    renderer: string;
    selection: DiagramSelection | null;
    undo?: DiagramRevision[];
    redo?: DiagramRevision[];
    kind?: 'authored' | 'derived';
    origins?: DiagramOrigin[];
    yaml?: string;
    sourceText?: string;
    originLabels?: Record<string, string>;
  }
  let undoModels = $state<DiagramRevision[]>([]);
  let redoModels = $state<DiagramRevision[]>([]);
  let kind = $state<'authored' | 'derived'>('authored');
  let origins = $state<DiagramOrigin[]>([]);
  let yaml = $state('');
  let sourceText = $state('');
  let originLabels = $state<Record<string, string>>({});
  let results = $state<EntitySearchResult[]>([]);
  let searching = $state(false);
  let searchGeneration = 0;
  const originTypes: Record<string, number> = {
    Spec: 0,
    Requirement: 1,
    Topic: 2,
    GlossaryTerm: 3,
    Conversation: 4,
    Artifact: 5,
    Task: 6,
  };
  function readOrigins(value: DiagramRevision): DiagramOrigin[] {
    if (value.source.format !== 'sddp-projection') return [];
    try {
      return JSON.parse(value.source.text).origins ?? [];
    } catch {
      return [];
    }
  }
  let tool = $state<'select' | 'connect'>('select');
  let connectFrom = $state<DiagramSelection | null>(null);
  let inspector: { focusLabel: () => void } | undefined = $state();
  let title = $state('');
  const initialModel = emptyModel();
  let model = $state<DiagramRevision>(cloneModel(initialModel));
  let baseTitle = $state('');
  let baseModel = $state<DiagramRevision>(cloneModel(initialModel));
  let rendererId = $state('sddp-svg-v2');
  let selection = $state<DiagramSelection | null>(null);
  let initialized = $state('');
  let busy = $state(false);
  let error = $state('');
  let history = $state<DiagramVersionListItemDto[]>([]);
  let historyOpen = $state(false);
  let historyLoading = $state(false);
  let leftRevision = $state(1);
  let rightRevision = $state(1);
  let comparison = $state<{
    left: DiagramRevision;
    right: DiagramRevision;
    diff: DiagramComparisonDto;
  } | null>(null);
  let generation = 0;
  let compareGeneration = 0;
  let comparisonView = $state<'visual' | 'yaml' | 'source'>('visual');
  let comparisonYaml = $state<{ left: string; right: string } | null>(null);
  let textLoading = $state(false);
  let textError = $state('');
  let textGeneration = 0;
  $effect(() => {
    // A new pair (or a closed comparison) owns a fresh text-view lifetime.
    void comparison;
    untrack(() => {
      ++textGeneration;
      comparisonView = 'visual';
      comparisonYaml = null;
      textLoading = false;
      textError = '';
    });
  });
  onDestroy(() => {
    ++generation;
    ++compareGeneration;
    ++searchGeneration;
    ++textGeneration;
    ++parserGeneration;
  });
  const tenantId = $derived($authStore.user?.tenantId ?? '');
  const userId = $derived($authStore.user?.id ?? '');
  const scope = $derived(JSON.stringify([tenantId, userId, projectId, diagram?.id ?? 'new']));
  const stateKey = $derived(tabId || `diagram-${projectId}-${diagram?.id ?? 'new'}`);
  // Until the saved detail mounts, subsequent effects must persist under the server identity.
  let createdId = $state('');
  const draftScope = $derived(
    JSON.stringify([tenantId, userId, projectId, diagram?.id ?? (createdId || 'new')])
  );
  const draftStateKey = $derived(
    tabId || `diagram-${projectId}-${diagram?.id ?? (createdId || 'new')}`
  );
  const pendingText = $derived(!!sourceText || !!yaml);
  const projectionPending = $derived(
    kind === 'derived' &&
      modelKey(origins.map((item) => `${item.type}:${item.id}`).sort()) !==
        modelKey(
          readOrigins(model)
            .map((item) => `${item.type}:${item.id}`)
            .sort()
        )
  );
  const dirty = $derived(
    title !== baseTitle ||
      modelKey(model) !== modelKey(baseModel) ||
      pendingText ||
      projectionPending
  );
  const readDenied = (e: unknown) =>
    e instanceof AccessDeniedError || (e instanceof ApiRequestError && e.status === 404);
  const adapter = $derived(resolveRenderer(rendererId, model.diagramType));
  const Renderer = $derived(adapter.component);
  const capabilities = createProjectCapabilityStore();
  const allowed = $derived(
    $capabilities.data?.projectId === projectId &&
      $capabilities.data.tenantId === tenantId &&
      $capabilities.userId === userId &&
      $capabilities.data.canWrite
      ? $capabilities.data.permissions
      : []
  );
  const canSave = $derived(allowed.includes(diagram ? 'diagram:update' : 'diagram:create'));
  const editable = $derived(canSave && !busy && !comparison && kind === 'authored');
  const { chooseAction } = createConfirmation(() => scope);
  const capabilityScope = $derived(JSON.stringify([projectId, tenantId, userId]));
  function forgetDeniedRead() {
    initialized = '';
    sourcePreview = null;
    sourceRequest = null;
    sourceText = '';
    undoModels = [];
    redoModels = [];
    origins = [];
    originLabels = {};
    results = [];
    yaml = '';
    ++searchGeneration;
    connectFrom = null;
    tool = 'select';
    model = emptyModel();
    baseModel = emptyModel();
    title = '';
    baseTitle = '';
    comparison = null;
    history = [];
    selection = null;
    setTabState(stateKey, {
      ...getTabState(stateKey),
      diagramWorkspace2: undefined,
      diagramWorkspace: undefined,
    });
    onreaddenied?.();
  }
  function downloadDraft() {
    const url = URL.createObjectURL(
      new Blob(
        [JSON.stringify({ title, kind, model: cloneModel(model), sourceText, yaml }, null, 2)],
        {
          type: 'application/json',
        }
      )
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = 'diagram-draft.json';
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
  $effect(() => {
    const [project, tenant, user] = JSON.parse(capabilityScope) as [string, string, string];
    untrack(() => {
      void capabilities.load(project, tenant, user);
    });
    return () => capabilities.clear();
  });
  $effect(() => {
    const key = scope;
    untrack(() => {
      if (initialized === key) return;
      ++generation;
      ++compareGeneration;
      createdId = '';
      const saved = readDraft();
      const restore =
        saved?.scope === key &&
        (saved.title !== saved.baseTitle ||
          modelKey(saved.model) !== modelKey(saved.baseModel) ||
          !!saved.sourceText ||
          !!saved.yaml ||
          (saved.kind === 'derived' &&
            modelKey(saved.origins ?? []) !== modelKey(readOrigins(saved.model))));
      const incoming = diagram?.model ?? emptyModel();
      title = restore ? saved.title : (diagram?.title ?? '');
      model = cloneModel(restore ? saved.model : incoming);
      baseTitle = restore ? saved.baseTitle : (diagram?.title ?? '');
      baseModel = cloneModel(restore ? saved.baseModel : incoming);
      kind =
        saved?.scope === key
          ? (saved.kind ?? diagram?.kind ?? 'authored')
          : (diagram?.kind ?? 'authored');
      origins = saved?.scope === key ? (saved.origins ?? readOrigins(model)) : readOrigins(model);
      originLabels =
        saved?.scope === key
          ? (saved.originLabels ?? {})
          : Object.fromEntries(
              Object.entries(model.diagramType === 'graph' ? model.content.nodes : {}).map(
                ([id, node]) => [id, node.label]
              )
            );
      yaml = saved?.scope === key ? (saved.yaml ?? '') : '';
      sourceText = saved?.scope === key ? (saved.sourceText ?? '') : '';
      sourcePreview = null;
      sourceRequest = null;
      results = [];
      searching = false;
      ++searchGeneration;
      rendererId = saved?.scope === key ? saved.renderer : 'sddp-svg-v2';
      selection = saved?.scope === key ? saved.selection : null;
      const restoreHistory =
        saved?.scope === key && (restore || saved.baseModel.revision === diagram?.revision);
      undoModels = restoreHistory ? (saved.undo ?? []).map(cloneModel) : [];
      redoModels = restoreHistory ? (saved.redo ?? []).map(cloneModel) : [];
      tool = 'select';
      connectFrom = null;
      error = '';
      busy = false;
      history = [];
      comparison = null;
      historyOpen = false;
      initialized = key;
    });
  });
  $effect(() => {
    if (initialized !== scope) return;
    const state: Draft = {
      scope: draftScope,
      title,
      model: cloneModel(model),
      baseTitle,
      baseModel: cloneModel(baseModel),
      renderer: rendererId,
      selection,
      undo: undoModels.map(cloneModel),
      redo: redoModels.map(cloneModel),
      kind,
      originLabels: { ...originLabels },
      origins: origins.map((item) => ({ ...item })),
      yaml,
      sourceText,
    };
    const isDirty = dirty;
    untrack(() => {
      setTabState(draftStateKey, { ...getTabState(draftStateKey), diagramWorkspace2: state });
      if (tabId) tabActions.updateTab(tabId, { dirty: isDirty, dirtyMarker: 'asterisk' });
    });
  });
  $effect(() =>
    registerTabCloseGuard(tabId, {
      isDirty: () => dirty || busy,
      describe: () => title || 'New diagram',
      save,
    })
  );
  onMount(() => {
    const beforeUnload = (event: BeforeUnloadEvent) => {
      if (dirty || busy) {
        event.preventDefault();
        event.returnValue = '';
      }
    };
    let wasBlurred = false;
    const rememberBlur = () => {
      wasBlurred = true;
    };
    const refreshCapabilities = () => {
      if (!wasBlurred) return;
      wasBlurred = false;
      void capabilities.load(projectId, tenantId, userId);
    };
    window.addEventListener('beforeunload', beforeUnload);
    window.addEventListener('focus', refreshCapabilities);
    window.addEventListener('blur', rememberBlur);
    return () => {
      window.removeEventListener('beforeunload', beforeUnload);
      window.removeEventListener('focus', refreshCapabilities);
      window.removeEventListener('blur', rememberBlur);
    };
  });
  export async function confirmLeave(): Promise<boolean> {
    if (busy) return false;
    if (!dirty) return true;
    const action = await chooseAction({
      title: 'Unsaved diagram',
      message: 'Save your changes before leaving this diagram?',
      choices: [
        { value: 'save', label: 'Save', variant: 'primary' },
        { value: 'discard', label: 'Discard changes', variant: 'danger' },
      ],
    });
    if (action === 'save') return save();
    if (action === 'discard') {
      sourceText = '';
      yaml = '';
      sourcePreview = null;
      sourceRequest = null;
      title = baseTitle;
      model = cloneModel(baseModel);
      origins = readOrigins(baseModel);
      results = [];
      undoModels = [];
      redoModels = [];
      connectFrom = null;
      tool = 'select';
      return true;
    }
    return false;
  }
  function command(value: DiagramCommand) {
    if (!editable) return;
    try {
      if (!editor.commands.includes(value.type))
        throw new Error(`The editor does not support ${value.type}.`);
      const next = editor.apply(model, value, baseModel.revision);
      if (modelKey(next) === modelKey(model)) {
        error = '';
        return;
      }
      undoModels = [...undoModels.slice(-99), cloneModel(model)];
      redoModels = [];
      model = next;
      error = '';
      if (value.type === 'remove') selection = null;
      if (selection && !getElement2(model, selection)) selection = null;
    } catch (e) {
      error = e instanceof Error ? e.message : 'Invalid diagram edit';
    }
  }
  function addNode(stencil = stencils2(model)[0]?.id ?? 'node', x?: number, y?: number) {
    const before = new Set(elements2(model).map((v) => refKey2(v.ref)));
    const owner = selection?.collection === 'branches' ? selection : undefined;
    command({ type: 'add', stencil, owner, ...(x === undefined ? {} : { x, y }) });
    selection = elements2(model).find((v) => !before.has(refKey2(v.ref)))?.ref ?? selection;
    tool = 'select';
    connectFrom = null;
  }
  function connectNodes(from: DiagramSelection, to: DiagramSelection) {
    if (!editable) return;
    const before = new Set(elements2(model).map((v) => refKey2(v.ref)));
    command({ type: 'connect', from, to });
    selection = elements2(model).find((v) => !before.has(refKey2(v.ref)))?.ref ?? selection;
    connectFrom = null;
    tool = 'select';
  }
  function selectElement(value: DiagramSelection) {
    if (tool === 'connect' && editable && connectable2(model, value)) {
      if (connectFrom) {
        connectNodes(connectFrom, value);
        return;
      }
      connectFrom = value;
    }
    selection = value;
  }
  async function renameNode(value: DiagramSelection) {
    if (!editable || !editor.commands.includes('set')) return;
    selection = value;
    await tick();
    inspector?.focusLabel();
  }
  function moveElement(target: DiagramSelection, x: number, y: number) {
    if (model.diagramType === 'sequence' && target.collection === 'events') {
      const owner = owner2(model, target);
      const list =
        owner.collection === 'diagram'
          ? model.content.timeline
          : (model.content.branches[owner.id]?.events ?? []);
      const positions = projectScene2(model).eventY;
      const others = list.filter((id) => id !== target.id);
      const index = others.filter((id) => (positions[id] ?? 0) < y).length;
      command({ type: 'reorder', target, owner, index });
    } else command({ type: 'move', target, x, y });
  }
  function undo() {
    if (!editable || !undoModels.length) return;
    redoModels = [...redoModels, cloneModel(model)];
    model = cloneModel(undoModels[undoModels.length - 1]!);
    undoModels = undoModels.slice(0, -1);
    error = '';
  }
  function redo() {
    if (!editable || !redoModels.length) return;
    undoModels = [...undoModels, cloneModel(model)];
    model = cloneModel(redoModels[redoModels.length - 1]!);
    redoModels = redoModels.slice(0, -1);
    error = '';
  }
  function handleKey(event: KeyboardEvent) {
    if (
      !editable ||
      (event.target instanceof Element &&
        event.target.closest('input,textarea,select,[contenteditable="true"]'))
    )
      return;
    const keepFocus = () => {
      event.preventDefault();
      event.stopPropagation();
      // Undo or deletion can remove the focused SVG element. Keep the next shortcut
      // inside this workbench without adding its container to the Tab sequence.
      if (event.currentTarget instanceof HTMLElement)
        event.currentTarget.focus({ preventScroll: true });
    };
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
      keepFocus();
      if (event.shiftKey) redo();
      else undo();
    }
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'y') {
      keepFocus();
      redo();
    }
    if ((event.key === 'Delete' || event.key === 'Backspace') && selection) {
      if (editor.commands.includes('remove')) {
        keepFocus();
        command({ type: 'remove', target: selection });
      }
    }
  }
  async function replaceCandidate(
    candidate: DiagramRevision,
    candidateKind: 'authored' | 'derived',
    key: string
  ) {
    if (key !== scope || comparison || !canSave) return false;
    validate2(candidate);
    if (
      diagram &&
      (candidate.diagramType !== model.diagramType ||
        (candidate.diagramType === 'c4' &&
          model.diagramType === 'c4' &&
          candidate.content.view !== model.content.view))
    )
      throw new Error('The saved diagram type and C4 view cannot change.');
    if (diagram && candidateKind !== diagram.kind)
      throw new Error('Imported YAML must match this diagram kind.');
    const hasContent =
      elements2(model).length > 0 ||
      !!model.source.text ||
      Object.keys(model.presentation.properties).length > 0 ||
      model.presentation.order.length > 0;
    if (hasContent && modelKey(model) !== modelKey(baseModel)) {
      const action = await chooseAction({
        title: 'Replace diagram draft',
        message: 'Replace your unsaved diagram with this validated candidate?',
        choices: [{ value: 'replace', label: 'Replace draft', variant: 'danger' }],
      });
      if (action !== 'replace' || key !== scope) return false;
    }
    if (candidateKind === 'authored' && kind === 'authored')
      undoModels = [...undoModels.slice(-99), cloneModel(model)];
    else undoModels = [];
    redoModels = [];
    model = { ...cloneModel(candidate), diagramId: model.diagramId, revision: baseModel.revision };
    kind = candidateKind;
    origins = readOrigins(model);
    originLabels = Object.fromEntries(
      Object.entries(model.diagramType === 'graph' ? model.content.nodes : {}).map(([id, node]) => [
        id,
        node.label,
      ])
    );
    selection = null;
    connectFrom = null;
    return true;
  }
  async function importYaml(file?: File) {
    if (!canSave || busy || comparison) return;
    const key = scope;
    const request = ++generation;
    busy = true;
    error = '';
    const current = () => key === scope && request === generation;
    try {
      const body = file ? await diagramFileContent(file) : { text: yaml };
      if (!current()) return;
      const result = await decodeDiagramYaml(tenantId, projectId, body, () => {
        if (!current()) throw new Error('The diagram scope has changed.');
      });
      if (
        current() &&
        (await replaceCandidate(
          result.model,
          result.model.source.format === 'sddp-projection' ? 'derived' : 'authored',
          key
        ))
      )
        yaml = '';
    } catch (e) {
      if (current()) error = e instanceof Error ? e.message : 'Invalid YAML';
    } finally {
      if (current()) busy = false;
    }
  }
  async function exportYaml() {
    if (busy) return;
    const key = scope;
    const request = ++generation;
    busy = true;
    error = '';
    const current = () => key === scope && request === generation;
    try {
      const result = await encodeDiagramYaml(
        tenantId,
        projectId,
        cloneModel(comparison?.right ?? model),
        () => {
          if (!current()) throw new Error('The diagram scope has changed.');
        }
      );
      if (!current()) return;
      const url = URL.createObjectURL(
        new Blob([result.text], { type: 'application/yaml;charset=utf-8' })
      );
      const link = document.createElement('a');
      link.href = url;
      link.download = 'diagram.yaml';
      link.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (e) {
      if (current()) {
        if (kind === 'derived' && readDenied(e)) forgetDeniedRead();
        error = e instanceof Error ? e.message : 'Failed to export YAML';
      }
    } finally {
      if (current()) busy = false;
    }
  }
  async function searchOrigins(query: string, type: EntityType) {
    const key = scope;
    const request = ++searchGeneration;
    searching = true;
    results = [];
    error = '';
    try {
      const found = await searchEntities(tenantId, projectId, query, type, 15);
      if (key === scope && request === searchGeneration)
        results = found.filter(
          (item) =>
            originTypes[item.entityType] !== undefined &&
            !origins.some(
              (origin) => origin.type === originTypes[item.entityType] && origin.id === item.id
            )
        );
    } catch (e) {
      if (key === scope && request === searchGeneration)
        error = e instanceof Error ? e.message : 'Search failed';
    } finally {
      if (key === scope && request === searchGeneration) searching = false;
    }
  }
  function addOrigin(result: EntitySearchResult) {
    const type = originTypes[result.entityType];
    if (type === undefined || origins.some((item) => item.type === type && item.id === result.id))
      return;
    if (origins.length >= 100) {
      error = 'Select at most 100 origins.';
      return;
    }
    origins = [...origins, { type, id: result.id }];
    originLabels = { ...originLabels, [`${type}:${result.id}`]: result.label };
    results = results.filter((item) => item !== result);
  }
  async function refreshProjection() {
    if (!canSave || busy || comparison || !origins.length) return;
    const key = scope;
    const request = ++generation;
    busy = true;
    error = '';
    const current = () => key === scope && request === generation;
    try {
      const result = await projectDiagram(
        tenantId,
        projectId,
        origins.map((item) => ({ ...item })),
        () => {
          if (!current()) throw new Error('The diagram scope has changed.');
        }
      );
      if (!current()) return;
      const candidate = cloneModel(result.model);
      candidate.presentation = cloneModel(model.presentation);
      if (candidate.diagramType === 'graph' && model.diagramType === 'graph') {
        for (const [id, node] of Object.entries(candidate.content.nodes))
          if (model.content.nodes[id])
            node.presentation = cloneModel(model.content.nodes[id]!.presentation);
        for (const [id, edge] of Object.entries(candidate.content.edges))
          if (model.content.edges[id])
            edge.presentation = cloneModel(model.content.edges[id]!.presentation);
      }
      await replaceCandidate(candidate, 'derived', key);
    } catch (e) {
      if (current()) {
        if (e instanceof AccessDeniedError || (e instanceof ApiRequestError && e.status === 404))
          forgetDeniedRead();
        error = e instanceof Error ? e.message : 'Projection failed';
      }
    } finally {
      if (current()) busy = false;
    }
  }
  async function changeKind(next: 'authored' | 'derived') {
    if (diagram || busy || next === kind) return;
    const key = scope;
    if (dirty) {
      const action = await chooseAction({
        title: 'Change diagram kind',
        message: 'Discard this draft and change its kind?',
        choices: [{ value: 'discard', label: 'Discard changes', variant: 'danger' }],
      });
      if (action !== 'discard' || key !== scope) return;
    }
    kind = next;
    model = emptyModel(next === 'derived' ? 'graph' : 'flowchart');
    origins = [];
    results = [];
    undoModels = [];
    redoModels = [];
    selection = null;
  }
  async function save(): Promise<boolean> {
    if (!canSave || busy || !title.trim()) return false;
    if (pendingText) {
      error = 'Validate and apply the source or YAML input before saving, or clear that input.';
      return false;
    }
    if (kind === 'derived' && (model.source.format !== 'sddp-projection' || projectionPending)) {
      error = 'Preview the selected origins before saving.';
      return false;
    }
    const key = scope;
    const request = ++generation;
    busy = true;
    error = '';
    const candidate = cloneModel(model);
    const isCurrent = () => key === scope && request === generation;
    const beforeDispatch = () => {
      if (!isCurrent()) throw new Error('The diagram scope has changed.');
    };
    try {
      const saved = diagram
        ? await saveDiagram(
            tenantId,
            projectId,
            diagram.id,
            {
              title,
              model: candidate,
              expectedRevision: baseModel.revision,
            },
            beforeDispatch
          )
        : await createDiagram(
            tenantId,
            projectId,
            { title, kind, model: candidate },
            beforeDispatch
          );
      if (!isCurrent()) return false;
      baseTitle = saved.title;
      title = saved.title;
      baseModel = cloneModel(saved.model);
      model = cloneModel(saved.model);
      undoModels = undoModels.map((item) => ({
        ...cloneModel(item),
        diagramId: saved.id,
        revision: saved.revision,
      }));
      redoModels = redoModels.map((item) => ({
        ...cloneModel(item),
        diagramId: saved.id,
        revision: saved.revision,
      }));
      // Creation replaces this section with the saved detail. Transfer its history
      // to the server-assigned identity before the parent mounts that detail.
      if (!diagram) {
        createdId = saved.id;
        if (draftStateKey !== stateKey)
          setTabState(stateKey, {
            ...getTabState(stateKey),
            diagramWorkspace2: undefined,
            diagramWorkspace: undefined,
          });
      }
      setTabState(draftStateKey, {
        ...getTabState(draftStateKey),
        diagramWorkspace2: {
          scope: JSON.stringify([tenantId, userId, projectId, saved.id]),
          title,
          model: cloneModel(model),
          baseTitle,
          baseModel: cloneModel(baseModel),
          renderer: rendererId,
          selection,
          undo: undoModels.map(cloneModel),
          redo: redoModels.map(cloneModel),
          kind,
          originLabels: { ...originLabels },
          origins: origins.map((item) => ({ ...item })),
          yaml,
          sourceText,
        } satisfies Draft,
      });
      comparison = null;
      history = [];
      historyOpen = false;
      onsaved(saved);
      return true;
    } catch (e) {
      if (isCurrent()) {
        if (kind === 'derived' && readDenied(e)) forgetDeniedRead();
        error =
          e instanceof ApiRequestError && e.status === 409
            ? 'A newer revision exists. Your draft is preserved. Download it before discarding your changes or reloading.'
            : e instanceof Error
              ? e.message
              : 'Failed to save diagram';
      }
      return false;
    } finally {
      if (isCurrent()) busy = false;
    }
  }
  async function loadHistory() {
    if (!diagram || busy) return;
    const key = scope;
    const request = ++compareGeneration;
    historyOpen = true;
    historyLoading = true;
    error = '';
    try {
      const versions = await getDiagramVersions(tenantId, projectId, diagram.id);
      if (key !== scope || request !== compareGeneration) return;
      history = versions;
      rightRevision = versions[0]?.revision ?? 1;
      leftRevision = versions[1]?.revision ?? rightRevision;
    } catch (e) {
      if (key === scope && request === compareGeneration) {
        if (e instanceof AccessDeniedError || (kind === 'derived' && readDenied(e)))
          forgetDeniedRead();
        error = e instanceof Error ? e.message : 'Failed to read history';
      }
    } finally {
      if (key === scope && request === compareGeneration) historyLoading = false;
    }
  }
  async function compare() {
    if (!diagram) return;
    const key = scope;
    const request = ++compareGeneration;
    historyLoading = true;
    error = '';
    comparison = null;
    try {
      const [left, right, diff] = await Promise.all([
        getDiagramVersion(tenantId, projectId, diagram.id, leftRevision),
        getDiagramVersion(tenantId, projectId, diagram.id, rightRevision),
        compareDiagramVersions(tenantId, projectId, diagram.id, leftRevision, rightRevision),
      ]);
      if (key !== scope || request !== compareGeneration) return;
      comparison = { left: left.model, right: right.model, diff };
    } catch (e) {
      if (key === scope && request === compareGeneration) {
        if (e instanceof AccessDeniedError || (kind === 'derived' && readDenied(e)))
          forgetDeniedRead();
        error = e instanceof Error ? e.message : 'Failed to compare revisions';
      }
    } finally {
      if (key === scope && request === compareGeneration) historyLoading = false;
    }
  }

  function readDraft(): Draft | undefined {
    const state = getTabState<{
      diagramWorkspace2?: Draft;
      diagramWorkspace?: Omit<Draft, 'model' | 'baseModel' | 'selection' | 'undo' | 'redo'> & {
        model: import('../../types').DiagramRevision;
        baseModel: import('../../types').DiagramRevision;
        selection: { target: 'node' | 'edge'; id: string } | null;
        undo?: import('../../types').DiagramRevision[];
        redo?: import('../../types').DiagramRevision[];
      };
    }>(stateKey);
    if (state?.diagramWorkspace2) return state.diagramWorkspace2;
    const old = state?.diagramWorkspace;
    if (!old) return undefined;
    return {
      ...old,
      model: promoteModel1(old.model),
      baseModel: promoteModel1(old.baseModel),
      undo: old.undo?.map(promoteModel1),
      redo: old.redo?.map(promoteModel1),
      selection: old.selection
        ? { collection: old.selection.target === 'node' ? 'nodes' : 'edges', id: old.selection.id }
        : null,
      renderer: old.renderer === 'sddp-table-v1' ? 'sddp-table-v2' : 'sddp-svg-v2',
    };
  }
  const typeOptions = [
    { value: 'flowchart', label: 'Flowchart' },
    { value: 'sequence', label: 'Sequence' },
    { value: 'class', label: 'Class' },
    { value: 'state', label: 'State' },
    { value: 'er', label: 'ERD' },
    { value: 'c4.context', label: 'C4 Context' },
    { value: 'c4.container', label: 'C4 Container' },
  ];
  async function changeType(value: string) {
    if (diagram || kind !== 'authored' || busy || comparison) return;
    const key = scope;
    if (elements2(model).length) {
      const action = await chooseAction({
        title: 'Change diagram type',
        message: 'Replace the current diagram content with an empty diagram of this type?',
        choices: [{ value: 'replace', label: 'Replace content', variant: 'danger' }],
      });
      if (action !== 'replace' || key !== scope) return false;
    }
    const [type, view] = value.split('.');
    const next = emptyModel(type as DiagramType2, view === 'container' ? 'container' : 'context');
    next.diagramId = model.diagramId;
    next.revision = baseModel.revision;
    model = next;
    undoModels = [];
    redoModels = [];
    selection = null;
    connectFrom = null;
    sourcePreview = null;
  }
  let sourceAdapters = $state<DiagramParserCapability2[]>([]);
  let sourceAdapter = $state('');
  let parserStatus = $state('');
  let parserGeneration = 0;
  $effect(() => {
    const key = scope;
    untrack(() => {
      const request = ++parserGeneration;
      sourceAdapters = [];
      sourceAdapter = '';
      parserStatus = 'Loading source parsers…';
      void listDiagramParsers2(tenantId, projectId)
        .then((adapters) => {
          if (key !== scope || request !== parserGeneration) return;
          if (
            !Array.isArray(adapters) ||
            adapters.some(
              (a) =>
                !a.id ||
                !a.version ||
                !Array.isArray(a.profiles) ||
                typeof a.generatesEditedSource !== 'boolean'
            )
          )
            throw new Error('Invalid parser capabilities');
          sourceAdapters = adapters;
          sourceAdapter =
            adapters.find((a) => a.id === model.sourceMap?.adapter.id)?.id ?? adapters[0]?.id ?? '';
          parserStatus = adapters.length
            ? ''
            : 'No source parser is available. YAML and visual editing remain available.';
        })
        .catch((e) => {
          if (key === scope && request === parserGeneration) {
            if (readDenied(e)) forgetDeniedRead();
            else
              parserStatus =
                'Source parser capabilities are unavailable. Retry by reopening this diagram.';
          }
        });
    });
  });
  let sourcePreview = $state<DiagramIdentityPreview2 | null>(null);
  let sourceRequest: DiagramSourcePreviewDto | null = null;
  let sourceDraftKey = '';
  let sourceScope = '';
  async function previewSource(resolution?: DiagramIdentityResolution2) {
    if (!editable || !sourceAdapter) return;
    const key = scope;
    const request = ++generation;
    const draft = modelKey(model);
    const input = sourceText;
    if (resolution && (!sourceRequest || sourceDraftKey !== draft || sourceScope !== key)) {
      error = 'The draft changed. Preview the source again.';
      return;
    }
    const body: DiagramSourcePreviewDto = resolution
      ? { ...cloneModel(sourceRequest!), resolution }
      : {
          text: input,
          adapterId: sourceAdapter,
          baseline: cloneModel(model),
          ...(diagram ? { diagramId: diagram.id, expectedRevision: baseModel.revision } : {}),
        };
    busy = true;
    error = '';
    const current = () =>
      key === scope && request === generation && draft === modelKey(model) && input === sourceText;
    try {
      const result = await previewDiagramSource2(tenantId, projectId, body, () => {
        if (!current()) throw new Error('Source preview changed.');
      });
      if (current()) {
        sourcePreview = result;
        sourceRequest = body;
        sourceDraftKey = draft;
        sourceScope = key;
      }
    } catch (e) {
      if (current()) {
        if (readDenied(e)) forgetDeniedRead();
        error = e instanceof Error ? e.message : 'Source preview failed';
      }
    } finally {
      if (key === scope && request === generation) busy = false;
    }
  }
  async function applySource() {
    if (!sourcePreview?.candidate || sourceDraftKey !== modelKey(model) || sourceScope !== scope)
      return;
    try {
      if (await replaceCandidate(sourcePreview.candidate, 'authored', scope)) {
        sourcePreview = null;
        sourceText = '';
        sourceRequest = null;
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Invalid source candidate';
    }
  }
  async function showComparison(view: 'visual' | 'yaml' | 'source') {
    comparisonView = view;
    const pair = comparison;
    const key = scope;
    const request = ++textGeneration;
    const isCurrent = () => key === scope && pair === comparison && request === textGeneration;
    textLoading = false;
    textError = '';
    if (view !== 'yaml' || !pair || comparisonYaml) return;
    textLoading = true;
    try {
      const beforeDispatch = () => {
        if (!isCurrent()) throw new Error('Comparison changed');
      };
      const [left, right] = await Promise.all([
        encodeDiagramYaml(tenantId, projectId, cloneModel(pair.left), beforeDispatch),
        encodeDiagramYaml(tenantId, projectId, cloneModel(pair.right), beforeDispatch),
      ]);
      if (isCurrent()) comparisonYaml = { left: left.text, right: right.text };
    } catch (e) {
      if (!isCurrent()) return;
      if (e instanceof AccessDeniedError || (kind === 'derived' && readDenied(e)))
        forgetDeniedRead();
      else textError = e instanceof Error ? e.message : 'Failed to read comparison YAML';
    } finally {
      if (isCurrent()) textLoading = false;
    }
  }
</script>

<section
  class="flex h-full min-h-0 flex-col p-4"
  aria-label="Diagram content"
  tabindex="-1"
  onkeydowncapture={handleKey}
>
  <PageBody noPadding class="space-y-4">
    <div class="flex items-center justify-between gap-3">
      <p class="text-xs text-[var(--color-text-tertiary)]">
        {kind === 'derived' ? 'Derived' : 'Authored'} · {title || 'New diagram'}{#if dirty}<span
            aria-label="Unsaved changes"
            title="Not saved. Use Save to persist this draft."
            class="font-semibold">*</span
          >{/if} · Revision {baseModel.revision}{dirty ? ' · Unsaved changes' : ''}
      </p>
      <DiagramActions
        {canSave}
        saveTitle={kind === 'derived' ? 'Save current state' : 'Save diagram'}
        dirty={dirty && !!title.trim() && !projectionPending && !pendingText}
        {busy}
        comparing={!!comparison}
        hasDiagram={!!diagram}
        onSave={() => {
          void save();
        }}
        onHistory={() => {
          void loadHistory();
        }}
        onDownload={downloadDraft}
        canUndo={undoModels.length > 0}
        canRedo={redoModels.length > 0}
        onUndo={undo}
        onRedo={redo}
      />
    </div>
    <div
      class="grid grid-cols-1 sm:grid-cols-[minmax(0,1fr)_auto] gap-3"
      role="group"
      aria-label="Diagram metadata"
    >
      <Input
        label="Diagram title"
        value={title}
        maxlength={200}
        disabled={!canSave || busy || !!comparison}
        oninput={(e) => (title = (e.target as HTMLInputElement).value)}
      />
      <Select
        label="View"
        bind:value={rendererId}
        placeholder=""
        options={diagramRenderers.map((item) => ({ value: item.id, label: item.label }))}
      />
    </div>
    {#if kind === 'authored'}
      <Select
        label="Diagram type"
        value={model.diagramType === 'c4' ? `c4.${model.content.view}` : model.diagramType}
        placeholder=""
        options={model.diagramType === 'graph'
          ? [{ value: 'graph', label: 'Graph (schema 1 compatibility)' }, ...typeOptions]
          : typeOptions}
        disabled={!!diagram || busy || !!comparison || !canSave}
        onchange={(value) => {
          void changeType(value);
        }}
      />
      <DiagramSourceTools2
        adapters={sourceAdapters}
        adapterId={sourceAdapter}
        {parserStatus}
        onadapter={(id) => {
          sourceAdapter = id;
          sourcePreview = null;
          sourceRequest = null;
        }}
        text={sourceText}
        {busy}
        disabled={!editable}
        preview={sourcePreview}
        ontext={(text) => {
          sourceText = text;
          sourcePreview = null;
          sourceRequest = null;
        }}
        onpreview={() => {
          void previewSource();
        }}
        onresolve={(resolution) => {
          void previewSource(resolution);
        }}
        onapply={() => {
          void applySource();
        }}
      />
    {/if}
    <DiagramDataTools
      {kind}
      hasDiagram={!!diagram}
      {busy}
      canChange={canSave && !comparison}
      {yaml}
      {origins}
      pending={projectionPending}
      labels={originLabels}
      {results}
      {searching}
      onkind={(next) => {
        void changeKind(next);
      }}
      onyaml={(text) => (yaml = text)}
      onimport={() => {
        void importYaml();
      }}
      onfile={(file) => {
        void importYaml(file);
      }}
      onexport={() => {
        void exportYaml();
      }}
      onsearch={(query, type) => {
        void searchOrigins(query, type);
      }}
      onadd={addOrigin}
      onremove={(origin) => (origins = origins.filter((item) => item !== origin))}
      onproject={() => {
        void refreshProjection();
      }}
    />
    {#if error}<p role="alert" class="text-sm text-[var(--color-error-500)]">{error}</p>{/if}
    {#if historyOpen}
      <section aria-label="Diagram revision history">
        <SurfaceCard class="space-y-2">
          {#if historyLoading}<Spinner size="sm" />{/if}
          {#each ['Left', 'Right'] as side (side)}
            <Select
              label={`${side} revision`}
              value={String(side === 'Left' ? leftRevision : rightRevision)}
              placeholder=""
              options={history.map((version) => ({
                value: String(version.revision),
                label: `Revision ${version.revision} · ${version.title}`,
              }))}
              onchange={(value) => {
                ++compareGeneration;
                historyLoading = false;
                comparison = null;
                if (side === 'Left') leftRevision = Number(value);
                else rightRevision = Number(value);
              }}
            />
          {/each}
          <Button size="sm" disabled={historyLoading || history.length === 0} onclick={compare}
            >Compare revisions</Button
          >
          <Button
            size="sm"
            variant="secondary"
            onclick={() => {
              ++compareGeneration;
              historyLoading = false;
              comparison = null;
              historyOpen = false;
            }}>Return to draft</Button
          >
        </SurfaceCard>
      </section>
    {/if}
    {#if comparison}
      <Select
        label="Comparison view"
        value={comparisonView}
        placeholder=""
        options={[
          { value: 'visual', label: 'Visual' },
          { value: 'yaml', label: 'YAML' },
          { value: 'source', label: 'Source' },
        ]}
        onchange={(value) => {
          void showComparison(value as 'visual' | 'yaml' | 'source');
        }}
      />
    {/if}
    {#if comparison && comparisonView !== 'visual'}
      <section aria-label="Diagram text diff" class="space-y-2">
        <p class="text-xs text-[var(--color-text-secondary)]">
          Text changes are separate from structural changes.{comparisonView === 'yaml'
            ? ' YAML includes the saved revision and source metadata.'
            : ' Visual edits preserve the original source.'}
        </p>
        {#if textLoading}<Spinner size="sm" />
          <p role="status">Preparing saved YAML…</p>{/if}
        {#if textError}<p role="alert" class="text-sm">{textError}</p>{/if}
        {#if comparisonView === 'source' || comparisonYaml}
          <CodeDiffViewer
            original={comparisonView === 'source'
              ? comparison.left.source.text
              : comparisonYaml!.left}
            modified={comparisonView === 'source'
              ? comparison.right.source.text
              : comparisonYaml!.right}
            language={comparisonView === 'yaml' ? 'yaml' : 'plaintext'}
            originalLabel={`Revision ${comparison.diff.leftRevision}${comparisonView === 'source' ? ` · ${comparison.left.source.format} · ${comparison.left.source.provider} ${comparison.left.source.providerVersion}` : ' · YAML'}`}
            modifiedLabel={`Revision ${comparison.diff.rightRevision}${comparisonView === 'source' ? ` · ${comparison.right.source.format} · ${comparison.right.source.provider} ${comparison.right.source.providerVersion}` : ' · YAML'}`}
          />
        {/if}
      </section>
    {:else if comparison}
      <div aria-label="Diagram visual diff" class="space-y-3">
        <p class="text-sm">
          Revision {comparison.diff.leftRevision} → {comparison.diff.rightRevision} · Semantic (amber)
          · Presentation (blue) · Removed (red dashed)
        </p>
        <details>
          <summary class="text-sm cursor-pointer">Left saved revision</summary><Renderer
            model={comparison.left}
          />
        </details>
        <Renderer
          model={comparison.right}
          left={comparison.left}
          changes={comparison.diff.changes}
        />
        {#if comparison.diff.changes.length === 0}<p class="text-sm">No structural changes</p>{/if}
        <details>
          <summary class="text-xs"
            >Source / provenance changes ({comparison.diff.provenanceChanges.length})</summary
          >
          <pre class="text-xs whitespace-pre-wrap break-all">{JSON.stringify(
              comparison.diff.provenanceChanges,
              null,
              2
            )}</pre>
        </details>
        <ul class="text-sm space-y-1" aria-label="Diagram changes">
          {#each comparison.diff.changes as change (JSON.stringify( [change.target, change.category, change.field] ))}<li
            >
              {change.category} · {change.target.collection}
              {change.target.id} · {change.kind}
              {change.field?.join('.') ?? ''}
              <pre class="text-xs whitespace-pre-wrap break-all">{JSON.stringify(
                  change.before
                )} → {JSON.stringify(change.after)}</pre>
            </li>{/each}
        </ul>
      </div>
    {:else}
      {#if editable}<p class="text-xs text-[var(--color-text-tertiary)]">
          {tool === 'connect'
            ? connectFrom
              ? 'Click the destination node.'
              : 'Click the source node, then the destination.'
            : 'Drag nodes to move. Double-click to rename, or drag a connection point to another node.'}
        </p>{/if}
      <div class="flex gap-3 min-w-0">
        {#if editable}<DiagramPalette
            stencils={stencils2(model)}
            canAdd={editor.commands.includes('add')}
            canConnect={editor.commands.includes('connect')}
            {tool}
            onadd={(stencil) => addNode(stencil)}
            ontool={(next) => {
              tool = next;
              connectFrom = null;
            }}
          />{/if}
        <div class="flex-1 min-w-0">
          <Renderer
            {model}
            {selection}
            onselect={selectElement}
            onmove={editable && adapter.moveNodes && editor.commands.includes('move')
              ? moveElement
              : undefined}
            onresize={editable && adapter.resizeNodes && editor.commands.includes('resize')
              ? (target, bounds) => command({ type: 'resize', target, ...bounds })
              : undefined}
            oncreateNode={editable && editor.commands.includes('add')
              ? (x, y, stencil) => addNode(stencil, x, y)
              : undefined}
            onconnect={editable && editor.commands.includes('connect') ? connectNodes : undefined}
            onconnectstart={editable
              ? (id) => {
                  tool = 'connect';
                  connectFrom = id;
                  selection = id;
                }
              : undefined}
            onrename={editable ? renameNode : undefined}
          />
        </div>
      </div>
      <DiagramInspector
        bind:this={inspector}
        {model}
        {selection}
        disabled={!editable}
        commands={editor.commands}
        onselect={selectElement}
        oncommand={command}
      />
    {/if}
    {#if !comparison}<details>
        <summary class="text-sm cursor-pointer">Source</summary>
        <pre
          class="text-sm whitespace-pre-wrap break-words rounded p-3 bg-[var(--color-bg-secondary)]">{model
            .source.text || 'No source text'}</pre>
        <p class="text-xs text-[var(--color-text-tertiary)]">
          {model.sourceState} · {model.source.format} · {model.source.provider}
          {model.source.providerVersion}. Visual edits preserve the original source.
        </p>
      </details>{/if}
  </PageBody>
</section>
