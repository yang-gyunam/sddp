<!-- Section: SettingsSidebar -->
<script lang="ts">
  /**
   * Settings Sidebar
   * Tree navigation with expandable subcategories
   * Pattern: data-driven items + Tailwind inline (matches ProjectSection)
   */

  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Button } from '@sddp/ui';
  import { config as appConfig } from '@sddp/shell';
  import { settingsNavigationStore, setActiveCategory, setActiveProjectCategory } from '../../stores/settingsNavigation.store';

  interface SidebarItem {
    id: string;
    label: string;
    icon: string;
    scrollTarget?: string;
  }

  interface Props {
    isAdmin?: boolean;
    ownedProjects?: Array<{ id: string; name: string }>;
  }

  let { isAdmin = false, ownedProjects = [] }: Props = $props();

  let activeCategory = $state('profile');

  $effect(() => {
    const unsubscribe = settingsNavigationStore.subscribe((state) => {
      activeCategory = state.activeCategory;
    });
    return unsubscribe;
  });

  // --- Data ---

  const profileChildren: SidebarItem[] = [
    { id: 'profile:basic', label: 'Basic Information', icon: 'user', scrollTarget: 'section-basic' },
    { id: 'profile:timezone', label: 'Timezone & Language', icon: 'globe', scrollTarget: 'section-timezone' },
    { id: 'profile:preferences', label: 'Preferences', icon: 'sliders', scrollTarget: 'section-preferences' },
    // Hidden: no backend notification infrastructure yet (email server, Web Push, SignalR events)
    // { id: 'profile:notifications', label: 'Notifications', icon: 'bell', scrollTarget: 'section-notifications' },
  ];

  const projectIntegrationsEnabled = appConfig.get('enableProjectIntegrations');

  const projectCategories = $derived<SidebarItem[]>([
    { id: 'general', label: 'General', icon: 'clipboard-list' },
    { id: 'members', label: 'Members', icon: 'circle-user-round' },
    { id: 'roles', label: 'Roles', icon: 'shield' },
    ...(projectIntegrationsEnabled
      ? [{ id: 'integrations', label: 'Integrations', icon: 'link' }]
      : []),
  ]);

  const systemItems: SidebarItem[] = [
    { id: 'system:dashboard', label: 'Dashboard', icon: 'bar-chart-2' },
    { id: 'system:users', label: 'Users', icon: 'user' },
    { id: 'system:roles', label: 'Roles', icon: 'shield' },
    { id: 'system:config', label: 'System Config', icon: 'settings' },
    { id: 'system:audit', label: 'Audit Logs', icon: 'file-text' },
    { id: 'system:health', label: 'Health', icon: 'heart' },
  ];

  // --- State ---

  let profileExpanded = $state(true);
  let expandedProjects = new SvelteSet<string>();

  $effect(() => {
    if (activeCategory === 'profile' || activeCategory.startsWith('profile:')) {
      profileExpanded = true;
    }
  });

  const isProfileActive = $derived(
    activeCategory === 'profile' || activeCategory.startsWith('profile:')
  );

  // --- Handlers ---

  function selectCategory(categoryId: string) {
    setActiveCategory(categoryId);
  }

  function selectProfileChild(item: SidebarItem) {
    setActiveCategory(item.id);
    if (item.scrollTarget) {
      requestAnimationFrame(() => {
        const el = document.getElementById(item.scrollTarget!);
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    }
  }

  function toggleProfile() {
    profileExpanded = !profileExpanded;
  }

  function toggleProject(projectId: string) {
    if (expandedProjects.has(projectId)) {
      expandedProjects.delete(projectId);
    } else {
      expandedProjects.add(projectId);
    }
  }

  function selectProjectCategory(projectId: string, categoryId: string) {
    setActiveProjectCategory(projectId, categoryId);
  }
</script>

<div class="flex flex-col h-full p-4 overflow-y-auto">
  <!-- USER Section -->
  <div class="mb-6">
    <div class="text-[0.6875rem] font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-1 px-2">USER</div>

    <!-- Profile (expandable parent) -->
    <div class="mb-0.5">
      <Button
        variant="unstyled"
        class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors border
               {isProfileActive
                 ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
                 : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]'}"
        onclick={() => { selectCategory('profile'); profileExpanded = true; }}
        aria-label="Profile"
      >
        <Button
          variant="unstyled"
          class="w-4 text-[0.5625rem] text-center flex-shrink-0 bg-transparent border-none p-0 cursor-pointer leading-none text-[var(--color-text-tertiary)]"
          onclick={(e) => { e.stopPropagation(); toggleProfile(); }}
        >
          {profileExpanded ? '▼' : '▶'}
        </Button>
        <Icon
          name="user"
          size="sm"
          class="text-[var(--color-text-tertiary)]"
        />
        <span class="flex-1 text-sm truncate font-medium">Profile</span>
      </Button>

      {#if profileExpanded}
        <div class="ml-5 pl-2 border-l border-[var(--color-border-primary)]">
          {#each profileChildren as item (item.id)}
            {@const isSelected = activeCategory === item.id}
            <Button
              variant="unstyled"
              class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors border
                     {isSelected
                       ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
                       : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
              onclick={() => selectProfileChild(item)}
              aria-label={item.label}
            >
              <span class="flex-1 text-sm truncate">{item.label}</span>
            </Button>
          {/each}
        </div>
      {/if}
    </div>
  </div>

  <!-- PROJECT Section -->
  {#if ownedProjects.length > 0}
    <div class="mb-6">
      <div class="text-[0.6875rem] font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-1 px-2">PROJECT</div>

      {#each ownedProjects as project (project.id)}
        <div class="mb-0.5">
          <Button
            variant="unstyled"
            class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]"
            onclick={() => toggleProject(project.id)}
            aria-label={project.name}
          >
            <span class="w-4 text-[0.5625rem] text-center text-[var(--color-text-tertiary)] flex-shrink-0">
              {expandedProjects.has(project.id) ? '▼' : '▶'}
            </span>
            <span class="flex-1 text-sm truncate font-medium">{project.name}</span>
          </Button>

          {#if expandedProjects.has(project.id)}
            <div class="ml-5 pl-2 border-l border-[var(--color-border-primary)]">
              {#each projectCategories as item (item.id)}
                {@const isSelected = activeCategory === `project:${item.id}`}
                <Button
                  variant="unstyled"
                  class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors border
                         {isSelected
                           ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
                           : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
                  onclick={() => selectProjectCategory(project.id, item.id)}
                  aria-label={item.label}
                >
                  <Icon
                    name={item.icon}
                    size="sm"
                    class="text-[var(--color-text-tertiary)]"
                  />
                  <span class="flex-1 text-sm truncate">{item.label}</span>
                </Button>
              {/each}
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}

  <!-- SYSTEM Section -->
  {#if isAdmin}
    <div class="mb-6">
      <div class="text-[0.6875rem] font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-1 px-2">SYSTEM</div>

      {#each systemItems as item (item.id)}
        {@const isSelected = activeCategory === item.id}
        <Button
          variant="unstyled"
          class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors border
                 {isSelected
                   ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
                   : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
          onclick={() => selectCategory(item.id)}
          aria-label={item.label}
        >
          <Icon
            name={item.icon}
            size="sm"
            class="text-[var(--color-text-tertiary)]"
          />
          <span class="flex-1 text-sm truncate">{item.label}</span>
        </Button>
      {/each}
    </div>
  {/if}
</div>
