import { RouterService } from '@sddp/shell';
import type { ProjectSearchHit } from '../services/ProjectSearchService';

/** Reuse the reference tab contract. Project-only viewers carry the source in their URL for reload/back. */
export function openProjectSearchHit(projectId: string, hit: ProjectSearchHit): void {
  if (['spec', 'requirement', 'glossary', 'conversation'].includes(hit.kind)) {
    window.dispatchEvent(
      new CustomEvent('sddp:navigate', {
        detail: { type: hit.kind, id: hit.target, label: hit.title, projectId },
      })
    );
    return;
  }
  const params = new URLSearchParams({ id: hit.target });
  if (hit.revision) params.set('revision', hit.revision);
  const base = '/project/' + encodeURIComponent(projectId);
  if (hit.menu === 'files') {
    params.set('revisionPath', hit.target);
    if (hit.line) params.set('line', String(hit.line));
    RouterService.navigate(base + '/files?' + params);
    return;
  }
  params.set('kind', hit.kind);
  if (hit.createdAt) params.set('at', hit.createdAt);
  RouterService.navigate(
    base + '/view/' + hit.menu + '/' + encodeURIComponent(hit.key) + '?' + params
  );
}
