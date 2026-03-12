// Dashboard Sections
export { default as DashboardSidebar } from './DashboardSidebar.svelte';
export { default as MyDashboardPanel } from './MyDashboardPanel.svelte';
export { default as SystemDashboardPanel } from './SystemDashboardPanel.svelte';

// Complex UI components
export { default as ActivityLog } from './ActivityLog.svelte';
export { default as TeamMemberList } from './TeamMemberList.svelte';

// Dashboard Phase 1 Widget Sections
export { default as SpecHealthWidget } from './SpecHealthWidget.svelte';
export { default as SignOffQueueWidget } from './SignOffQueueWidget.svelte';
export { default as ContributionWidget } from './ContributionWidget.svelte';
export { default as ProjectSpotlightWidget } from './ProjectSpotlightWidget.svelte';
export { default as DueDateWidget } from './DueDateWidget.svelte';
export { default as EffortTrackerWidget } from './EffortTrackerWidget.svelte';

// Re-export type for dashboard menu items
export interface DashboardMenuItem {
  id: string;
  label: string;
  icon: string;
  section: string;
}
