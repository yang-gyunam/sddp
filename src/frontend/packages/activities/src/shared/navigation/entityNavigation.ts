import { RouterService } from '@sddp/shell';

const routes: Record<string, { menu: string; standalone?: string }> = {
  spec: { menu: 'specs', standalone: 'spec' },
  requirement: { menu: 'requirements', standalone: 'requirement' },
  conversation: { menu: 'conversations', standalone: 'conversation' },
  glossary: { menu: 'glossary', standalone: 'glossary' },
  glossaryterm: { menu: 'glossary', standalone: 'glossary' },
  artifact: { menu: 'artifacts' },
  task: { menu: 'tasks' },
};

/** Route through the app's registered views; never create a tab without a component. */
export function navigateToEntity(projectId: string | undefined, type: string, id: string): boolean {
  const route = Object.hasOwn(routes, type.toLowerCase()) ? routes[type.toLowerCase()] : undefined;
  if (!route || !id) return false;
  if (projectId) {
    RouterService.navigate(
      `/project/${encodeURIComponent(projectId)}/${route.menu}?id=${encodeURIComponent(id)}`
    );
    return true;
  }
  // Global references may lack project context. Only these standalone views are registered.
  if (route.standalone) {
    RouterService.navigate(`/${route.standalone}/${encodeURIComponent(id)}`);
    return true;
  }
  return false;
}
