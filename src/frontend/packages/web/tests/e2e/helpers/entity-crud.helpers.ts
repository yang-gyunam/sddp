/**
 * E2E Entity CRUD Helpers
 * API test entity create/get/update/delete utility
 */

import { APIRequestContext } from '@playwright/test';
import { API_BASE } from './constants';
import { loginAsAdmin, getAuthHeaders } from './auth.helpers';
import { withRetry } from './retry.helpers';

/**
 * Create test Spec via API
 */
export async function createTestSpec(
  request: APIRequestContext,
  token: string,
  data: {
    code: string;
    title: string;
    description: string;
    decision: string;
    context?: string;
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/specs`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      code: data.code,
      title: data.title,
      description: data.description,
      decision: data.decision,
      context: data.context || 'Test context',
      scope: 'Test scope',
      outOfScope: 'Out of scope items',
      definitions: 'Test definitions',
      acceptanceCriteria: 'Acceptance criteria for testing',
      risks: 'Test risks',
      assumptions: 'Test assumptions',
      owners: 'Test Team',
      reviewTrigger: 'Manual review',
    },
    failOnStatusCode: false,
  });

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create spec: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Transition Spec to InReview status
 */
export async function submitSpecForReview(
  request: APIRequestContext,
  token: string,
  specId: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/specs/${specId}/submit-review`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  });

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to submit spec for review: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Approve Spec (InReview → Approved)
 */
export async function approveSpec(
  request: APIRequestContext,
  token: string,
  specId: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/specs/${specId}/approve`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  });

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to approve spec: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Submit a sign-off for a spec in review
 */
export async function submitSpecSignOff(
  request: APIRequestContext,
  token: string,
  specId: string,
  data?: {
    decision?: 'Approved' | 'Conditional' | 'Rejected';
    comments?: string;
    conditions?: string;
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/specs/${specId}/sign-off`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      decision: data?.decision ?? 'Approved',
      comments: data?.comments ?? 'Approved for E2E transition flow',
      conditions: data?.conditions,
    },
    failOnStatusCode: false,
  });

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to submit sign-off: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Lock Spec (Approved → Locked)
 */
export async function lockSpec(
  request: APIRequestContext,
  token: string,
  specId: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/specs/${specId}/lock`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  });

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to lock spec: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create test Conversation via API
 */
export async function createTestConversation(
  request: APIRequestContext,
  token: string,
  name: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      name: name,
    },
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create conversation: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create test Forum conversation via API
 */
export async function createTestForum(
  request: APIRequestContext,
  token: string,
  name: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      name: name,
      conversationType: 'Forum',
      visibility: 'Public',
    },
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create forum: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create topic in a Forum
 */
export async function createTestTopic(
  request: APIRequestContext,
  token: string,
  forumId: string,
  title: string,
  initialMessageContent?: string,
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/conversations/${forumId}/topics`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      title,
      initialMessageContent,
    },
    failOnStatusCode: false,
  });

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create topic: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Post message to a Topic
 */
export async function postTopicMessage(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string,
  data: { type: string; content: string },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/messages`,
    {
      headers: getAuthHeaders(token, tenantId, projectId),
      data: {
        type: data.type,
        content: data.content,
      },
      failOnStatusCode: false,
    }
  );

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to post topic message: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Post message to Conversation
 */
export async function postMessage(
  request: APIRequestContext,
  token: string,
  conversationId: string,
  data: {
    type: string;
    content: string;
    references?: string[];
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations/${conversationId}/messages`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      type: data.type,
      content: data.content,
      references: data.references || [],
    },
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to post message: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create test Glossary term via API
 */
export async function createTestGlossaryTerm(
  request: APIRequestContext,
  token: string,
  data: {
    term: string;
    definition: string;
    category: string;
    synonyms?: string;
    abbreviation?: string;
  }
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/glossary`, {
    headers: getAuthHeaders(token),
    data: {
      term: data.term,
      definition: data.definition,
      category: data.category,
      synonyms: data.synonyms,
      abbreviation: data.abbreviation,
      usageExamples: [],
      relatedTermIds: [],
    },
    failOnStatusCode: false,
  });

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create glossary term: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create Relationship via API
 */
export async function createTestRelationship(
  request: APIRequestContext,
  token: string,
  data: {
    sourceEntityType: string;
    sourceEntityId: string;
    targetEntityType: string;
    targetEntityId: string;
    relationType: string;
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/relationships`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      fromEntityType: data.sourceEntityType,
      fromEntityId: data.sourceEntityId,
      toEntityType: data.targetEntityType,
      toEntityId: data.targetEntityId,
      type: data.relationType,
    },
    failOnStatusCode: false,
  });

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create relationship: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Validate Relationship (circular reference check)
 */
export async function validateRelationship(
  request: APIRequestContext,
  token: string,
  data: {
    sourceEntityType: string;
    sourceEntityId: string;
    targetEntityType: string;
    targetEntityId: string;
    relationType: string;
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await request.post(`${API_BASE}/relationships/validate`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: {
      fromEntityType: data.sourceEntityType,
      fromEntityId: data.sourceEntityId,
      toEntityType: data.targetEntityType,
      toEntityId: data.targetEntityId,
      type: data.relationType,
    },
    failOnStatusCode: false,
  });

  const result = await response.json();
  return result.data || result;
}

/**
 * Create a Requirement via API
 */
export async function createTestRequirement(
  request: APIRequestContext,
  token: string,
  data: {
    code: string;
    title: string;
    description: string;
    level?: number;
    parentId?: string;
  },
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const body: Record<string, unknown> = {
    code: data.code,
    title: data.title,
    description: data.description,
    level: data.level ?? 0,
  };
  if (data.parentId) {
    body.parentId = data.parentId;
  }
  const response = await request.post(`${API_BASE}/requirements`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: body,
    failOnStatusCode: false,
  });

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create requirement: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Seed test data via API for UI tests. Returns null if backend is unavailable.
 */
export async function seedTestData(
  request: APIRequestContext
): Promise<{
  token: string;
  specId: string;
  requirementId: string;
  glossaryTermId: string;
  conversationId: string;
} | null> {
  try {
    const token = await loginAsAdmin(request);
    const ts = Date.now();

    const spec: Record<string, unknown> = await createTestSpec(request, token, {
      code: `SPEC_UI_${ts}`,
      title: 'UI Test Spec',
      description: 'Created for UI E2E tests',
      decision: 'Test decision',
    }) as Record<string, unknown>;

    const req: Record<string, unknown> = await createTestRequirement(request, token, {
      code: `REQ_UI_${ts}`,
      title: 'UI Test Requirement',
      description: 'Created for UI E2E tests',
    }) as Record<string, unknown>;

    const term: Record<string, unknown> = await createTestGlossaryTerm(request, token, {
      term: `UITerm_${ts}`,
      definition: 'Term for UI E2E tests',
      category: 'Testing',
    }) as Record<string, unknown>;

    const conversation: Record<string, unknown> = await createTestConversation(
      request,
      token,
      `UI Test Conversation ${ts}`
    ) as Record<string, unknown>;

    return {
      token,
      specId: spec.id as string,
      requirementId: req.id as string,
      glossaryTermId: term.id as string,
      conversationId: conversation.id as string,
    };
  } catch {
    return null;
  }
}

/**
 * Conclude a channel (Active → Concluded)
 */
export async function concludeChannel(
  request: APIRequestContext,
  token: string,
  conversationId: string,
  decisionSpecId?: string,
  tenantId?: string,
  projectId?: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations/${conversationId}/conclude`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: decisionSpecId ? { decisionSpecId: decisionSpecId } : {},
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to conclude channel: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Add members to a conversation
 */
export async function addMembers(
  request: APIRequestContext,
  token: string,
  conversationId: string,
  userIds: string[],
  tenantId?: string,
  projectId?: string
): Promise<unknown> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations/${conversationId}/members`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    data: { userIds },
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to add members: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return result.data || result;
}

/**
 * Create or get DirectMessage with a target user
 */
export async function createDirectMessage(
  request: APIRequestContext,
  token: string,
  targetUserId: string,
  tenantId?: string,
  projectId?: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations/dm/${targetUserId}`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200 && response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create DM: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Get DM list for current user
 */
export async function getDirectMessages(
  request: APIRequestContext,
  token: string,
  tenantId?: string,
  projectId?: string
): Promise<Record<string, unknown>[]> {
  const response = await withRetry(
    () => request.get(`${API_BASE}/conversations/dm`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to get DMs: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  const data = result.data || result;
  return Array.isArray(data) ? data : ((data.items || []) as Record<string, unknown>[]);
}

/**
 * Get conversation list (project-scoped)
 */
export async function getConversations(
  request: APIRequestContext,
  token: string,
  params?: { scope?: string; tenantId?: string; projectId?: string }
): Promise<Record<string, unknown>[]> {
  const url = params?.scope
    ? `${API_BASE}/conversations?scope=${params.scope}`
    : `${API_BASE}/conversations`;

  const response = await withRetry(
    () => request.get(url, {
    headers: getAuthHeaders(token, params?.tenantId, params?.projectId),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to get conversations: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  const data = result.data || result;
  return Array.isArray(data) ? data : ((data.items || []) as Record<string, unknown>[]);
}

/**
 * Get tenant-wide conversation list
 */
export async function getGlobalConversations(
  request: APIRequestContext,
  token: string,
  tenantId?: string,
  projectId?: string
): Promise<Record<string, unknown>[]> {
  const response = await withRetry(
    () => request.get(`${API_BASE}/conversations/global`, {
    headers: getAuthHeaders(token, tenantId, projectId),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to get global conversations: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  const data = result.data || result;
  return Array.isArray(data) ? data : ((data.items || []) as Record<string, unknown>[]);
}

/**
 * Get unread counts for current user
 */
export async function getUnreadCounts(
  request: APIRequestContext,
  token: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.get(`${API_BASE}/conversations/unread`, {
    headers: getAuthHeaders(token),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to get unread counts: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Get invitable users for a conversation
 */
export async function getInvitableUsers(
  request: APIRequestContext,
  token: string,
  conversationId: string,
  search?: string
): Promise<Record<string, unknown>[]> {
  const url = search
    ? `${API_BASE}/conversations/${conversationId}/invitable-users?search=${encodeURIComponent(search)}`
    : `${API_BASE}/conversations/${conversationId}/invitable-users`;

  const response = await withRetry(
    () => request.get(url, {
    headers: getAuthHeaders(token),
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to get invitable users: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>[];
}

/**
 * Reopen a closed topic
 */
export async function reopenTopic(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/reopen`,
    {
      headers: getAuthHeaders(token),
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to reopen topic: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Archive a closed topic
 */
export async function archiveTopic(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/archive`,
    {
      headers: getAuthHeaders(token),
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to archive topic: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Toggle topic pin
 */
export async function toggleTopicPin(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/pin`,
    {
      headers: getAuthHeaders(token),
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to toggle topic pin: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Toggle topic lock
 */
export async function toggleTopicLock(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/lock`,
    {
      headers: getAuthHeaders(token),
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to toggle topic lock: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Update topic title
 */
export async function updateTopicTitle(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string,
  title: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.put(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}`,
    {
      headers: getAuthHeaders(token),
      data: { title },
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to update topic title: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Close a topic
 */
export async function closeTopic(
  request: APIRequestContext,
  token: string,
  forumId: string,
  topicId: string,
  decisionSpecId?: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(
    `${API_BASE}/conversations/${forumId}/topics/${topicId}/close`,
    {
      headers: getAuthHeaders(token),
      data: decisionSpecId ? { decisionSpecId } : {},
      failOnStatusCode: false,
    }
  )
  );

  if (response.status() !== 200) {
    const error = await response.text();
    throw new Error(`Failed to close topic: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Create a private channel
 */
export async function createPrivateChannel(
  request: APIRequestContext,
  token: string,
  name: string
): Promise<Record<string, unknown>> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/conversations`, {
    headers: getAuthHeaders(token),
    data: {
      name: name,
      conversationType: 'Channel',
      visibility: 'Private',
    },
    failOnStatusCode: false,
  })
  );

  if (response.status() !== 201) {
    const error = await response.text();
    throw new Error(`Failed to create private channel: ${response.status()} - ${error}`);
  }

  const result = await response.json();
  return (result.data || result) as Record<string, unknown>;
}

/**
 * Cleanup test data (optional, if backend provides cleanup endpoints)
 */
export async function cleanup(
  request: APIRequestContext,
  token: string,
  entityIds: { specs?: string[]; conversations?: string[]; glossary?: string[] }
): Promise<void> {
  // Delete specs
  if (entityIds.specs) {
    for (const id of entityIds.specs) {
      await request.delete(`${API_BASE}/specs/${id}`, {
        headers: getAuthHeaders(token),
        failOnStatusCode: false,
      });
    }
  }

  // Delete conversations (if supported)
  if (entityIds.conversations) {
    for (const id of entityIds.conversations) {
      await request.delete(`${API_BASE}/conversations/${id}`, {
        headers: getAuthHeaders(token),
        failOnStatusCode: false,
      });
    }
  }

  // Delete glossary terms (if supported)
  if (entityIds.glossary) {
    for (const id of entityIds.glossary) {
      await request.delete(`${API_BASE}/glossary/${id}`, {
        headers: getAuthHeaders(token),
        failOnStatusCode: false,
      });
    }
  }
}
