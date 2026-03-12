<!-- Section: ProjectDetailPanel — Settings > System Projects -->
<script lang="ts">
  /**
   * ProjectDetailPanel
   * Right panel for viewing, editing, and creating projects
   */

  import { formatDateTime, toast } from '@sddp/shell';
  import { Icon, Input, IconButton, Textarea } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import type { Project, ProjectDetail } from '../../../projects/types/project.types';
  import { getProjectService } from '../../../projects/services/ProjectService';
  import type { CreateProjectParams, UpdateProjectParams } from '../../../projects/services/ProjectService';

  type PanelMode = 'view' | 'edit' | 'create';

  interface Props {
    mode: PanelMode;
    project: ProjectDetail | null;
    onClose: () => void;
    onEdit?: () => void;
    onCreated: (project: Project) => void;
    onSaved?: (project: ProjectDetail) => void;
  }

  let { mode, project, onClose, onEdit, onCreated, onSaved }: Props = $props();

  const projectService = getProjectService();

  let loading = $state(false);
  let errors = $state<Record<string, string>>({});

  // Create form fields
  let createForm = $state<CreateProjectParams>({
    code: '',
    name: '',
    description: '',
  });

  // Edit form fields
  let editForm = $state<{ name: string; description: string }>({
    name: '',
    description: '',
  });

  // Reset edit form when project changes or entering edit mode
  $effect(() => {
    if (project && mode === 'edit') {
      editForm = {
        name: project.name,
        description: project.description ?? '',
      };
      errors = {};
    }
  });

  // Reset create form when mode becomes create
  $effect(() => {
    if (mode === 'create') {
      createForm = { code: '', name: '', description: '' };
      errors = {};
    }
  });

  function validateCreateForm(): boolean {
    const newErrors: Record<string, string> = {};

    if (!createForm.code || createForm.code.length < 2) {
      newErrors.code = 'Code must be at least 2 characters';
    } else if (!/^[A-Z][A-Z0-9_]*$/.test(createForm.code)) {
      newErrors.code = 'Code must start with uppercase letter (A-Z, 0-9, _)';
    } else if (createForm.code.length > 30) {
      newErrors.code = 'Code must not exceed 30 characters';
    }
    if (!createForm.name || createForm.name.length < 2) {
      newErrors.name = 'Name must be at least 2 characters';
    }
    if (createForm.description && createForm.description.length > 500) {
      newErrors.description = 'Description must not exceed 500 characters';
    }

    errors = newErrors;
    return Object.keys(newErrors).length === 0;
  }

  function validateEditForm(): boolean {
    const newErrors: Record<string, string> = {};

    if (!editForm.name || editForm.name.length < 2) {
      newErrors.name = 'Name must be at least 2 characters';
    } else if (editForm.name.length > 200) {
      newErrors.name = 'Name must not exceed 200 characters';
    }
    if (editForm.description && editForm.description.length > 2000) {
      newErrors.description = 'Description must not exceed 2000 characters';
    }

    errors = newErrors;
    return Object.keys(newErrors).length === 0;
  }

  async function handleCreate() {
    if (!validateCreateForm()) return;

    loading = true;
    try {
      const created = await projectService.createProject(createForm);
      toast.success(`Project "${created.name}" created successfully`);
      onCreated(created);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create project';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleSave() {
    if (!project || !validateEditForm()) return;

    loading = true;
    try {
      const params: UpdateProjectParams = {
        name: editForm.name,
        description: editForm.description || undefined,
      };
      const updated = await projectService.updateProject(project.id, params);
      toast.success(`Project "${updated.name}" updated successfully`);
      onSaved?.(updated);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update project';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleLifecycleAction(action: 'initialize' | 'conclude' | 'reopen' | 'archive', label: string) {
    if (!project) return;
    if (!confirm(`Are you sure you want to ${label.toLowerCase()} "${project.name}"?`)) return;

    loading = true;
    try {
      let updated;
      switch (action) {
        case 'initialize': updated = await projectService.initializeProject(project.id); break;
        case 'conclude': updated = await projectService.concludeProject(project.id); break;
        case 'reopen': updated = await projectService.reopenProject(project.id); break;
        case 'archive': updated = await projectService.archiveProject(project.id); break;
      }
      toast.success(`Project "${updated.name}" ${label.toLowerCase()}d successfully`);
      onSaved?.(updated);
    } catch (err) {
      const message = err instanceof Error ? err.message : `Failed to ${label.toLowerCase()} project`;
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  function formatDateStr(dateStr?: string): string {
    if (!dateStr) return 'Never';
    return formatDateTime(dateStr, { month: 'short', year: undefined });
  }
</script>

<div class="panel">
  {#if mode === 'view' && project}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="project" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Project Details</span>
      {#snippet actions()}
        {#if onEdit}
          <IconButton icon="edit" onclick={onEdit} title="Edit project" />
        {/if}
        {#if project.status === 'planning'}
          <IconButton icon="play" variant="success" onclick={() => handleLifecycleAction('initialize', 'Initialize')} disabled={loading} title="Initialize project" />
        {:else if project.status === 'active'}
          <IconButton icon="pause" variant="warn" onclick={() => handleLifecycleAction('conclude', 'Conclude')} disabled={loading} title="Conclude project" />
        {:else if project.status === 'concluded'}
          <IconButton icon="play" variant="success" onclick={() => handleLifecycleAction('reopen', 'Reopen')} disabled={loading} title="Reopen project" />
          <IconButton icon="archive" variant="danger" onclick={() => handleLifecycleAction('archive', 'Archive')} disabled={loading} title="Archive project" />
        {/if}
        <IconButton icon="x" onclick={onClose} title="Close" />
      {/snippet}
    </DetailHeader>
  {:else if mode === 'edit'}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="edit" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Edit Project</span>
      {#snippet actions()}
        <IconButton icon="check" variant="success" onclick={handleSave} disabled={loading} title="Save changes" />
        <IconButton icon="x" onclick={onClose} title="Cancel" />
      {/snippet}
    </DetailHeader>
  {:else if mode === 'create'}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="folder-plus" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Create Project</span>
      {#snippet actions()}
        <IconButton icon="check" variant="success" onclick={handleCreate} disabled={loading} title="Create project" />
        <IconButton icon="x" onclick={onClose} title="Cancel" />
      {/snippet}
    </DetailHeader>
  {/if}

  <div class="panel-body">
    {#if mode === 'create'}
      <!-- Create Mode -->
      <div class="form-section">
        <Input
          label="Code"
          placeholder="e.g. PROJ_ALPHA"
          bind:value={createForm.code}
          disabled={loading}
          autocomplete="off"
          error={errors.code}
        />
        <Input
          label="Name"
          placeholder="e.g. Project Alpha"
          bind:value={createForm.name}
          disabled={loading}
          autocomplete="off"
          error={errors.name}
        />
        <div class="form-field">
          <label for="create-description">Description</label>
          <Textarea
            id="create-description"
            placeholder="Project description (optional)"
            bind:value={createForm.description}
            disabled={loading}
            rows={3}
            unstyled
            class="form-field-textarea"
          />
          {#if errors.description}
            <span class="field-error">{errors.description}</span>
          {/if}
        </div>
      </div>

    {:else if mode === 'edit' && project}
      <!-- Edit Mode -->
      <div class="form-section">
        <Input
          label="Name"
          bind:value={editForm.name}
          disabled={loading}
          autocomplete="off"
          error={errors.name}
        />
        <div class="form-field">
          <label for="edit-description">Description</label>
          <Textarea
            id="edit-description"
            placeholder="Project description (optional)"
            bind:value={editForm.description}
            disabled={loading}
            rows={3}
            unstyled
            class="form-field-textarea"
          />
          {#if errors.description}
            <span class="field-error">{errors.description}</span>
          {/if}
        </div>
      </div>

    {:else if mode === 'view' && project}
      <!-- View Mode -->
      <div class="project-header">
        <div class="project-icon">
          <Icon name="project" size="lg" />
        </div>
        <div class="project-info">
          <h4 class="project-name">{project.name}</h4>
          <p class="project-code">{project.code}</p>
        </div>
        <span class="status-badge" class:planning={project.status === 'planning'} class:active={project.status === 'active'} class:concluded={project.status === 'concluded'} class:archived={project.status === 'archived'}>
          {project.status}
        </span>
      </div>

      {#if project.description}
        <p class="project-description">{project.description}</p>
      {/if}

      <div class="detail-grid">
        <div class="detail-item">
          <span class="detail-label">Owner</span>
          <span class="detail-value">{project.ownerName || 'N/A'}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Created</span>
          <span class="detail-value">{formatDateStr(project.createdAt)}</span>
        </div>
      </div>

      <!-- Statistics -->
      <div class="stats-section">
        <h4 class="section-title">Statistics</h4>
        <div class="stats-grid">
          <div class="stat-item">
            <span class="stat-value">{project.statistics.conversations.total}</span>
            <span class="stat-label">Conversations</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{project.statistics.requirements.total}</span>
            <span class="stat-label">Requirements</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{project.statistics.specs.total}</span>
            <span class="stat-label">Specs</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{project.statistics.tasks.total}</span>
            <span class="stat-label">Tasks</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{project.statistics.glossary.total}</span>
            <span class="stat-label">Glossary</span>
          </div>
          <div class="stat-item">
            <span class="stat-value">{project.statistics.artifacts.total}</span>
            <span class="stat-label">Artifacts</span>
          </div>
        </div>
      </div>

      <!-- Members -->
      {#if project.members.length > 0}
        <div class="members-section">
          <h4 class="section-title">Members ({project.members.length})</h4>
          <div class="members-list">
            {#each project.members as member, memberIndex (`${member.userId}-${member.role}-${memberIndex}`)}
              <div class="member-item">
                <div class="member-avatar">{member.displayName.charAt(0).toUpperCase()}</div>
                <span class="member-name">{member.displayName}</span>
                <span class="member-role">{member.role}</span>
              </div>
            {/each}
          </div>
        </div>
      {:else}
        <div class="members-section">
          <h4 class="section-title">Members</h4>
          <p class="empty-text">No members</p>
        </div>
      {/if}

    {/if}
  </div>
</div>

<style>
  @import '../../styles/detail-panel.css';
  @import '../../styles/detail-panel-form.css';

  /* Project Header (View Mode) */
  .project-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .project-icon {
    width: 40px;
    height: 40px;
    border-radius: 8px;
    background: var(--color-accent-primary, #3b82f6);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .project-info {
    flex: 1;
    min-width: 0;
  }

  .project-name {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .project-code {
    margin: 0.125rem 0 0;
    font-size: 0.75rem;
    font-family: monospace;
    color: var(--color-text-secondary, #6b7280);
  }

  .project-description {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--color-text-secondary, #6b7280);
    line-height: 1.5;
  }

  /* Badge */
  .status-badge {
    padding: 0.125rem 0.5rem;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 500;
    text-transform: uppercase;
    background: var(--color-bg-tertiary, #f3f4f6);
    color: var(--color-text-secondary, #6b7280);
    flex-shrink: 0;
  }

  .status-badge.planning {
    background: var(--color-info-100, #dbeafe);
    color: var(--color-info-700, #1d4ed8);
  }

  .status-badge.active {
    background: var(--color-success-10);
    color: var(--color-success-500);
  }

  .status-badge.concluded {
    background: var(--color-warning-10);
    color: var(--color-warning-500);
  }

  .status-badge.archived {
    background: var(--color-neutral-100, #f3f4f6);
    color: var(--color-neutral-600, #4b5563);
  }

  /* Statistics */
  .stats-section {
    border-top: 1px solid var(--color-border-primary, #e5e7eb);
    padding-top: 1rem;
  }

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.5rem;
  }

  .stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0.5rem;
    background: var(--color-bg-secondary, #f9fafb);
    border-radius: 6px;
    gap: 0.125rem;
  }

  .stat-value {
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
  }

  .stat-label {
    font-size: 0.6875rem;
    color: var(--color-text-tertiary, #9ca3af);
  }

  /* Members */
  .members-section {
    border-top: 1px solid var(--color-border-primary, #e5e7eb);
    padding-top: 1rem;
  }

  .members-list {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .member-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.625rem;
    background: var(--color-bg-secondary, #f9fafb);
    border-radius: 4px;
    font-size: 0.8125rem;
  }

  .member-avatar {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    background: var(--color-accent-primary, #3b82f6);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.6875rem;
    font-weight: 600;
    flex-shrink: 0;
  }

  .member-name {
    flex: 1;
    color: var(--color-text-primary, #111);
  }

  .member-role {
    font-size: 0.75rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .form-field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .form-field label {
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--color-text-secondary, #6b7280);
  }

  .field-error {
    font-size: 0.75rem;
    color: var(--color-error-500, #ef4444);
  }

  .empty-text {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
  }

  /* Textarea custom */
  .form-field :global(.form-field-textarea) {
    width: 100%;
    padding: 0.5rem 0.75rem;
    background: var(--color-bg-primary, #fff);
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 4px;
    color: var(--color-text-primary, #111);
    font-size: 0.875rem;
    font-family: inherit;
    resize: vertical;
  }

  .form-field :global(.form-field-textarea:focus) {
    outline: none;
    border-color: var(--color-accent-primary, #3b82f6);
    box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.15);
  }
</style>
