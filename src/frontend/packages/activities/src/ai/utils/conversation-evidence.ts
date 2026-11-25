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
    const entity =
      /^(spec|task|requirement):([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})@[0-9a-f]{64}$/i.exec(
        raw
      );
    const typed = match ?? entity;
    const normalized = typed ? raw.toLowerCase() : raw;
    if (result.some((item) => item.raw === normalized)) continue;
    result.push(
      typed
        ? {
            raw: normalized,
            type: typed[1]!.toLowerCase(),
            id: typed[2]!.toLowerCase(),
          }
        : { raw }
    );
  }
  return result;
}
