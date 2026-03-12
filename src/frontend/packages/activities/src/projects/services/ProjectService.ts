/**
 * ProjectService - API Client for Project Management
 * Handles project listing, detail retrieval, and project-scoped entity queries
 */

import { fetchWithAuth } from '../../shared/api';
import type { Project, ProjectDetail, ProjectMember, ProjectWithBadges, UpdateProjectParams, ProjectOwnership, ProjectDataResetResult, TenantDataResetResult } from '../types';
export type { UpdateProjectParams } from '../types';

// ============================================
// Response Types for Project-Scoped Entities
// ============================================

export interface ProjectTopic {
  id: string;
  topic: string;
  status: string;
  participantCount: number;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectRequirement {
  id: string;
  code: string;
  title: string;
  level: 'A' | 'B' | 'C';
  priority: string;
  status: string;
  childrenCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectSpec {
  id: string;
  code: string;
  title: string;
  description?: string;
  decision?: string;
  version: string;
  status: string;
  requirementId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectGlossaryTerm {
  id: string;
  term: string;
  abbreviation?: string;
  definition: string;
  category: string;
  status: string;
  version: string;
  synonyms?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber?: number;
  page?: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

interface ApiUserRef {
  id: string;
  name: string;
  avatarUrl?: string | null;
}

interface ProjectApiResponse {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  description?: string;
  owner?: ApiUserRef;
  ownerId?: string;
  ownerName?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

interface ProjectDetailApiResponse extends ProjectApiResponse {
  statistics: ProjectDetail['statistics'];
  members: ProjectDetail['members'];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function assertRecord(value: unknown, responseName: string): asserts value is Record<string, unknown> {
  if (!isRecord(value)) {
    throw new Error(`Invalid ${responseName}: expected an object`);
  }
}

function getRequiredStringField(
  record: Record<string, unknown>,
  fieldName: string,
  responseName: string
): string {
  const value = record[fieldName];
  if (typeof value !== 'string') {
    throw new Error(`Invalid ${responseName}: "${fieldName}" must be a string`);
  }
  return value;
}

function getOptionalStringField(
  record: Record<string, unknown>,
  fieldName: string,
  responseName: string
): string | undefined {
  const value = record[fieldName];
  if (value === undefined) {
    return undefined;
  }
  if (typeof value !== 'string') {
    throw new Error(`Invalid ${responseName}: "${fieldName}" must be a string`);
  }
  return value;
}

function normalizeProjectStatus(status: string, responseName: string): Project['status'] {
  const normalized = status.toLowerCase();
  if (
    normalized === 'planning'
    || normalized === 'active'
    || normalized === 'concluded'
    || normalized === 'archived'
  ) {
    return normalized as Project['status'];
  }
  throw new Error(`Invalid ${responseName}: unsupported status "${status}"`);
}

function mapProjectApiResponse(raw: unknown, responseName = 'project response'): Project {
  assertRecord(raw, responseName);

  let ownerId: string | undefined;
  let ownerName: string | undefined;

  const owner = raw['owner'];
  if (owner !== undefined) {
    assertRecord(owner, `${responseName}.owner`);
    ownerId = getRequiredStringField(owner, 'id', `${responseName}.owner`);
    ownerName = getRequiredStringField(owner, 'name', `${responseName}.owner`);
  } else {
    ownerId = getOptionalStringField(raw, 'ownerId', responseName);
    ownerName = getOptionalStringField(raw, 'ownerName', responseName);
  }

  if (ownerId === undefined || ownerName === undefined) {
    throw new Error(`Invalid ${responseName}: owner information is required`);
  }

  const description = getOptionalStringField(raw, 'description', responseName);

  return {
    id: getRequiredStringField(raw, 'id', responseName),
    tenantId: getRequiredStringField(raw, 'tenantId', responseName),
    code: getRequiredStringField(raw, 'code', responseName),
    name: getRequiredStringField(raw, 'name', responseName),
    description,
    ownerId,
    ownerName,
    status: normalizeProjectStatus(getRequiredStringField(raw, 'status', responseName), responseName),
    createdAt: getRequiredStringField(raw, 'createdAt', responseName),
    updatedAt: getRequiredStringField(raw, 'updatedAt', responseName),
  };
}

function normalizeProjectMembers(members: unknown[], responseName: string): ProjectMember[] {
  const membersByUserId = new Map<string, ProjectMember>();

  for (let index = 0; index < members.length; index += 1) {
    const rawMember = members[index];
    if (!isRecord(rawMember)) {
      continue;
    }

    const userIdRaw = rawMember['userId'];
    if (typeof userIdRaw !== 'string' || userIdRaw.trim().length === 0) {
      continue;
    }

    const userId = userIdRaw.trim();
    const personIdRaw = rawMember['personId'];
    const displayNameRaw = rawMember['displayName'];
    const roleRaw = rawMember['role'];
    const avatarUrlRaw = rawMember['avatarUrl'];
    const lastActivityAtRaw = rawMember['lastActivityAt'];
    const isOnlineRaw = rawMember['isOnline'];

    const personId = typeof personIdRaw === 'string' && personIdRaw.trim().length > 0
      ? personIdRaw
      : userId;
    const displayName = typeof displayNameRaw === 'string' && displayNameRaw.trim().length > 0
      ? displayNameRaw
      : userId;
    const role = typeof roleRaw === 'string' && roleRaw.trim().length > 0
      ? roleRaw
      : 'Member';
    const avatarUrl = typeof avatarUrlRaw === 'string' ? avatarUrlRaw : undefined;
    const lastActivityAt = typeof lastActivityAtRaw === 'string' ? lastActivityAtRaw : undefined;
    const isOnline = typeof isOnlineRaw === 'boolean' ? isOnlineRaw : false;

    const existing = membersByUserId.get(userId);
    if (!existing) {
      membersByUserId.set(userId, {
        userId,
        personId,
        displayName,
        role,
        avatarUrl,
        lastActivityAt,
        isOnline,
      });
      continue;
    }

    // Merge role labels when API returns duplicate rows for the same user.
    if (existing.role !== role) {
      const roleParts = new Set(
        `${existing.role},${role}`
          .split(',')
          .map((part) => part.trim())
          .filter(Boolean)
      );
      existing.role = Array.from(roleParts).join(', ');
    }
    if (!existing.avatarUrl && avatarUrl) {
      existing.avatarUrl = avatarUrl;
    }
    if (!existing.lastActivityAt && lastActivityAt) {
      existing.lastActivityAt = lastActivityAt;
    }
    if (!existing.isOnline && isOnline) {
      existing.isOnline = true;
    }
  }

  if (membersByUserId.size < members.length) {
    console.warn(
      `[ProjectService] Deduplicated members in ${responseName}: ${members.length - membersByUserId.size} duplicate item(s) removed.`
    );
  }

  return Array.from(membersByUserId.values());
}

function mapProjectDetailApiResponse(raw: unknown, responseName = 'project detail response'): ProjectDetail {
  assertRecord(raw, responseName);

  const statistics = raw['statistics'];
  if (!isRecord(statistics)) {
    throw new Error(`Invalid ${responseName}: "statistics" must be an object`);
  }

  const members = raw['members'];
  if (!Array.isArray(members)) {
    throw new Error(`Invalid ${responseName}: "members" must be an array`);
  }

  return {
    ...mapProjectApiResponse(raw, responseName),
    statistics: statistics as unknown as ProjectDetail['statistics'],
    members: normalizeProjectMembers(members, responseName),
  };
}

// ============================================
// Project API Functions
// ============================================

/**
 * Get list of projects for the current tenant
 */
export async function getProjects(tenantId: string): Promise<Project[]> {
  const projects: Project[] = [];
  let pageNumber = 1;

  while (true) {
    const page = await getProjectsPage(tenantId, pageNumber, 50);
    projects.push(...page.items);

    if (!page.hasNextPage) {
      break;
    }

    pageNumber += 1;
  }

  return projects;
}

export async function getProjectsPage(
  tenantId: string,
  pageNumber: number = 1,
  pageSize: number = 20
): Promise<PagedResponse<Project>> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });

  const response = await fetchWithAuth<PagedResponse<ProjectApiResponse>>(`/projects?${params}`, { tenantId });
  return {
    ...response,
    items: response.items.map((project, index) =>
      mapProjectApiResponse(project, `projects[${index}]`)
    ),
  };
}

/**
 * Get list of projects with badge counts
 */
export async function getProjectsWithBadges(tenantId: string): Promise<ProjectWithBadges[]> {
  const projects = await getProjects(tenantId);

  return Promise.all(
    projects.map(async (project) => {
      try {
        const detail = await getProjectById(tenantId, project.id);
        return {
          ...project,
          badges: {
            conversations: detail.statistics.conversations.secondary,
            requirements: detail.statistics.requirements.secondary,
            specs: detail.statistics.specs.secondary,
            tasks: detail.statistics.tasks?.secondary || 0,
          },
        };
      } catch {
        return {
          ...project,
          badges: { conversations: 0, requirements: 0, specs: 0, tasks: 0 },
        };
      }
    })
  );
}

/**
 * Get project detail by ID (includes statistics and members)
 */
export async function getProjectById(
  tenantId: string,
  projectId: string
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}`, { tenantId });
  return mapProjectDetailApiResponse(response);
}

// ============================================
// Project-Scoped Entity API Functions
// ============================================

/**
 * Get topics (forum conversations) for a project
 */
export async function getProjectTopics(
  tenantId: string,
  projectId: string,
  page = 1,
  pageSize = 20,
  status?: string
): Promise<ProjectTopic[]> {
  const collected: ProjectTopic[] = [];
  let currentPage = page;

  while (true) {
    const result = await getProjectTopicsPage(tenantId, projectId, currentPage, pageSize, status);
    collected.push(...result.items);

    if (!result.hasNextPage) {
      break;
    }

    currentPage += 1;
  }

  return collected;
}

export async function getProjectTopicsPage(
  tenantId: string,
  projectId: string,
  page = 1,
  pageSize = 20,
  status?: string
): Promise<PagedResponse<ProjectTopic>> {
  const params = new URLSearchParams({
    pageNumber: String(page),
    pageSize: String(pageSize),
  });
  if (status) params.append('status', status);

  return fetchWithAuth<PagedResponse<ProjectTopic>>(`/conversations?${params}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get requirements for a project
 */
export async function getProjectRequirements(
  tenantId: string,
  projectId: string,
  page = 1,
  pageSize = 20,
  level?: 'A' | 'B' | 'C',
  status?: string
): Promise<PagedResponse<ProjectRequirement>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (level) params.append('level', level);
  if (status) params.append('status', status);

  return fetchWithAuth<PagedResponse<ProjectRequirement>>(`/requirements?${params}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get specs for a project
 */
export async function getProjectSpecs(
  tenantId: string,
  projectId: string,
  page = 1,
  pageSize = 20,
  status?: string
): Promise<PagedResponse<ProjectSpec>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (status) params.append('status', status);

  return fetchWithAuth<PagedResponse<ProjectSpec>>(`/specs?${params}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get glossary terms for a project
 */
export async function getProjectGlossaryTerms(
  tenantId: string,
  projectId: string,
  page = 1,
  pageSize = 20,
  category?: string,
  status?: string
): Promise<PagedResponse<ProjectGlossaryTerm>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (category) params.append('category', category);
  if (status) params.append('status', status);

  return fetchWithAuth<PagedResponse<ProjectGlossaryTerm>>(`/glossary?${params}`, {
    tenantId,
    projectId,
  });
}

export interface CreateProjectParams {
  code: string;
  name: string;
  description?: string;
}

/**
 * Create a new project
 */
export async function createProject(
  tenantId: string,
  params: CreateProjectParams
): Promise<Project> {
  const response = await fetchWithAuth<ProjectApiResponse>('/projects', {
    tenantId,
    method: 'POST',
    body: params,
  });
  return mapProjectApiResponse(response);
}

/**
 * Get project ownership data for treemap visualization
 */
export async function getProjectOwnership(
  tenantId: string,
  projectId: string
): Promise<ProjectOwnership> {
  return fetchWithAuth<ProjectOwnership>(`/projects/${projectId}/ownership`, { tenantId });
}

export interface AddProjectMemberParams {
  userId: string;
  role: string;
}

/**
 * Add a member to a project
 */
export async function addProjectMember(
  tenantId: string,
  projectId: string,
  params: AddProjectMemberParams
): Promise<ProjectMember> {
  return fetchWithAuth<ProjectMember>(`/projects/${projectId}/members`, {
    tenantId,
    method: 'POST',
    body: params,
  });
}

/**
 * Deactivate a project member (soft block)
 */
export async function deactivateProjectMember(
  tenantId: string,
  projectId: string,
  userId: string
): Promise<void> {
  await fetchWithAuth(`/projects/${projectId}/members/${userId}/deactivate`, {
    tenantId,
    method: 'PUT',
  });
}

/**
 * Remove a project member (hard delete)
 */
export async function removeProjectMember(
  tenantId: string,
  projectId: string,
  userId: string
): Promise<void> {
  await fetchWithAuth(`/projects/${projectId}/members/${userId}/remove`, {
    tenantId,
    method: 'PUT',
  });
}

/**
 * Update an existing project
 */
export async function updateProject(
  tenantId: string,
  projectId: string,
  params: UpdateProjectParams
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}`, {
    tenantId,
    method: 'PUT',
    body: params,
  });
  return mapProjectDetailApiResponse(response);
}

// ============================================
// Project Lifecycle API Functions
// ============================================

/**
 * Initialize a project (Planning → Active)
 */
export async function initializeProject(
  tenantId: string,
  projectId: string
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}/initialize`, {
    tenantId,
    method: 'POST',
  });
  return mapProjectDetailApiResponse(response);
}

/**
 * Conclude a project (Active → Concluded)
 */
export async function concludeProject(
  tenantId: string,
  projectId: string,
  reason?: string
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}/conclude`, {
    tenantId,
    method: 'POST',
    body: { reason },
  });
  return mapProjectDetailApiResponse(response);
}

/**
 * Reopen a project (Concluded → Active)
 */
export async function reopenProject(
  tenantId: string,
  projectId: string,
  reason?: string
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}/reopen`, {
    tenantId,
    method: 'POST',
    body: { reason },
  });
  return mapProjectDetailApiResponse(response);
}

/**
 * Archive a project (Concluded → Archived)
 */
export async function archiveProject(
  tenantId: string,
  projectId: string
): Promise<ProjectDetail> {
  const response = await fetchWithAuth<ProjectDetailApiResponse>(`/projects/${projectId}/archive`, {
    tenantId,
    method: 'POST',
  });
  return mapProjectDetailApiResponse(response);
}

// ============================================
// Admin Data Reset API Functions
// ============================================

/**
 * Reset project data (admin only)
 */
export async function resetProjectData(
  tenantId: string,
  projectId: string,
  confirmationCode: string
): Promise<ProjectDataResetResult> {
  return fetchWithAuth<ProjectDataResetResult>(`/admin/projects/${projectId}/reset`, {
    tenantId,
    method: 'POST',
    body: { confirmationCode },
  });
}

/**
 * Reset all tenant data (admin only)
 */
export async function resetTenantData(
  tenantId: string,
  confirmationToken: string
): Promise<TenantDataResetResult> {
  return fetchWithAuth<TenantDataResetResult>('/admin/tenants/reset', {
    tenantId,
    method: 'POST',
    body: { confirmationToken },
  });
}

// ============================================
// Singleton Service Class
// ============================================

export class ProjectService {
  private tenantId = '';
  private projectId = '';

  setTenantId(tenantId: string): void {
    this.tenantId = tenantId;
  }

  setProjectId(projectId: string): void {
    this.projectId = projectId;
  }

  /** Set tenant/project context in one call */
  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId ?? '';
  }

  getProjects(): Promise<Project[]> {
    return getProjects(this.tenantId);
  }

  getProjectsPage(
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<Project>> {
    return getProjectsPage(this.tenantId, pageNumber, pageSize);
  }

  getProjectsWithBadges(): Promise<ProjectWithBadges[]> {
    return getProjectsWithBadges(this.tenantId);
  }

  getProjectById(projectId: string = this.projectId): Promise<ProjectDetail> {
    return getProjectById(this.tenantId, projectId);
  }

  createProject(params: CreateProjectParams): Promise<Project> {
    return createProject(this.tenantId, params);
  }

  updateProject(projectId: string, params: UpdateProjectParams): Promise<ProjectDetail> {
    return updateProject(this.tenantId, projectId, params);
  }

  addProjectMember(projectId: string, params: AddProjectMemberParams): Promise<ProjectMember> {
    return addProjectMember(this.tenantId, projectId, params);
  }

  deactivateProjectMember(projectId: string, userId: string): Promise<void> {
    return deactivateProjectMember(this.tenantId, projectId, userId);
  }

  removeProjectMember(projectId: string, userId: string): Promise<void> {
    return removeProjectMember(this.tenantId, projectId, userId);
  }

  getProjectOwnership(projectId: string = this.projectId): Promise<ProjectOwnership> {
    return getProjectOwnership(this.tenantId, projectId);
  }

  // Lifecycle methods
  initializeProject(projectId: string = this.projectId): Promise<ProjectDetail> {
    return initializeProject(this.tenantId, projectId);
  }

  concludeProject(projectId: string = this.projectId, reason?: string): Promise<ProjectDetail> {
    return concludeProject(this.tenantId, projectId, reason);
  }

  reopenProject(projectId: string = this.projectId, reason?: string): Promise<ProjectDetail> {
    return reopenProject(this.tenantId, projectId, reason);
  }

  archiveProject(projectId: string = this.projectId): Promise<ProjectDetail> {
    return archiveProject(this.tenantId, projectId);
  }

  // Admin methods
  resetProjectData(projectId: string, confirmationCode: string): Promise<ProjectDataResetResult> {
    return resetProjectData(this.tenantId, projectId, confirmationCode);
  }

  resetTenantData(confirmationToken: string): Promise<TenantDataResetResult> {
    return resetTenantData(this.tenantId, confirmationToken);
  }

  // Project-scoped entity methods
  getProjectTopics(
    projectId: string = this.projectId,
    page = 1,
    pageSize = 20,
    status?: string
  ): Promise<ProjectTopic[]> {
    return getProjectTopics(this.tenantId, projectId, page, pageSize, status);
  }

  getProjectTopicsPage(
    projectId: string = this.projectId,
    page = 1,
    pageSize = 20,
    status?: string
  ): Promise<PagedResponse<ProjectTopic>> {
    return getProjectTopicsPage(this.tenantId, projectId, page, pageSize, status);
  }

  getProjectRequirements(
    projectId: string = this.projectId,
    page = 1,
    pageSize = 20,
    level?: 'A' | 'B' | 'C',
    status?: string
  ): Promise<PagedResponse<ProjectRequirement>> {
    return getProjectRequirements(this.tenantId, projectId, page, pageSize, level, status);
  }

  getProjectSpecs(
    projectId: string = this.projectId,
    page = 1,
    pageSize = 20,
    status?: string
  ): Promise<PagedResponse<ProjectSpec>> {
    return getProjectSpecs(this.tenantId, projectId, page, pageSize, status);
  }

  getProjectGlossaryTerms(
    projectId: string = this.projectId,
    page = 1,
    pageSize = 20,
    category?: string,
    status?: string
  ): Promise<PagedResponse<ProjectGlossaryTerm>> {
    return getProjectGlossaryTerms(this.tenantId, projectId, page, pageSize, category, status);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: ProjectService | null = null;

export function getProjectService(): ProjectService {
  if (!instance) {
    instance = new ProjectService();
  }
  return instance;
}

export function resetProjectService(): void {
  instance = null;
}
