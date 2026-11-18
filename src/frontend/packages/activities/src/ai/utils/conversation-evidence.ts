import type { ConversationEvidenceReference as Reference } from '../types/evidence.types';

export function parseEvidenceReferences(references: string[]): Reference[] {
  const result: Reference[] = [];
  for (const value of references) {
    const raw = value.trim();
    if (!raw) continue;
    const match =
      /^(spec|glossary|decision|artifact):([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i.exec(
        raw
      );
    const normalized = match ? raw.toLowerCase() : raw;
    if (result.some((item) => item.raw === normalized)) continue;
    result.push(
      match
        ? {
            raw: normalized,
            type: match[1]!.toLowerCase(),
            id: match[2]!.toLowerCase(),
          }
        : { raw }
    );
  }
  return result;
}
