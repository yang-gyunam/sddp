/**
 * Pure utility functions for Glossary form business logic.
 *
 * Extracted from GlossaryForm.svelte so both the component
 * and tests can share the same source of truth.
 */
import type { TermCategory, CreateGlossaryTermRequest } from '../types';

/**
 * Whether both required form fields (term and definition) are non-empty.
 */
export function isFormValid(term: string, definition: string): boolean {
  return term.trim().length > 0 && definition.trim().length > 0;
}

/**
 * Build a CreateGlossaryTermRequest from form data,
 * trimming strings and converting empty optionals to undefined.
 */
export function buildCreateRequest(formData: {
  term: string;
  definition: string;
  category: TermCategory;
  source?: string;
  synonyms?: string;
  abbreviation?: string;
  usageExamples?: string[];
  relatedTermIds?: string[];
  sourceSpecId?: string;
  sourceConversationId?: string;
  sourceRequirementId?: string;
  ownerUserId?: string;
}): CreateGlossaryTermRequest {
  return {
    term: formData.term.trim(),
    definition: formData.definition.trim(),
    category: formData.category,
    source: formData.source?.trim() || undefined,
    synonyms: formData.synonyms?.trim() || undefined,
    abbreviation: formData.abbreviation?.trim() || undefined,
    usageExamples: formData.usageExamples?.length ? formData.usageExamples : undefined,
    relatedTermIds: formData.relatedTermIds?.length ? formData.relatedTermIds : undefined,
    sourceSpecId: formData.sourceSpecId?.trim() || undefined,
    sourceConversationId: formData.sourceConversationId?.trim() || undefined,
    sourceRequirementId: formData.sourceRequirementId?.trim() || undefined,
    ownerUserId: formData.ownerUserId?.trim() || undefined,
  };
}
