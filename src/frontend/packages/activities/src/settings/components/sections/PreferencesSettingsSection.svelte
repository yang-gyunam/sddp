<!-- Section: Settings > PreferencesSettingsSection -->
<script lang="ts">
  /**
   * Preferences Settings Page
   * User preferences and editor settings - VS Code flat style
   */

  import { untrack } from 'svelte';
  import { toast, SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState, formatDateTime, formatSmartTime } from '@sddp/shell';
  import { IconButton, Button, Spinner, RadioGroup } from '@sddp/ui';
  import SettingsService from '../../services/SettingsService';
  import type { UserPreferences, AccentColor, DateFormatPreference, ToastDurationPreference } from '../../types';
  import {
    setAccentColor,
    getAccentColor,
    setThemeMode,
    getThemeMode,
    ACCENT_COLORS,
  } from '../../../../../web/src/stores/theme.store';
  type ThemeMode = 'light' | 'dark' | 'system';
  import { SettingItem, SettingGroupHeader } from '../idioms';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();

  let preferences = $state<UserPreferences | null>(null);
  let loading = $state(false);
  let saving = $state(false);
  let pageError = $state<string | null>(null);
  let selectedAccent = $state<AccentColor>(getAccentColor());

  // Theme: status ThemeMode → preferences
  function currentThemeAsPreference(): UserPreferences['theme'] {
    const mode = getThemeMode();
    return mode === 'system' ? 'auto' : mode;
  }

  function handleAccentChange(accent: AccentColor) {
    selectedAccent = accent;
    setAccentColor(accent);
    if (preferences) {
      preferences.accentColor = accent;
    }
  }

  // Theme options
  const themeOptions = [
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
    { value: 'auto', label: 'Auto' },
  ];

  // Toast Duration options
  const toastDurationOptions = [
    { value: '2000', label: 'Short (2s)' },
    { value: '3000', label: 'Normal (3s)' },
    { value: '5000', label: 'Long (5s)' },
  ];

  // Date format options with preview
  const previewDate = new Date(Date.now() - 10 * 60 * 1000);
  const dateFormatOptions = [
    // formatSmartTime: 'relative' (formatDateByPreference settings)
    { value: 'relative', label: 'Relative', description: formatSmartTime(previewDate) },
    { value: 'absolute', label: 'Absolute', description: formatDateTime(previewDate) },
    { value: 'iso', label: 'ISO 8601', description: previewDate.toISOString() },
  ];

  // Derived string values for RadioGroup (avoids bind:value={undefined} error)
  let themeStr = $derived(preferences?.theme ?? currentThemeAsPreference());
  let toastDurationStr = $derived(String(preferences?.toastDuration ?? 3000));
  let dateFormatStr = $derived(preferences?.dateFormat ?? 'relative');

  function handleThemeChange(val: string) {
    if (preferences) {
      preferences.theme = val as UserPreferences['theme'];
      // (Save)
      const mode: ThemeMode = val === 'auto' ? 'system' : (val as ThemeMode);
      setThemeMode(mode);
    }
  }

  function handleToastDurationChange(val: string) {
    if (preferences) {
      preferences.toastDuration = Number(val) as ToastDurationPreference;
    }
  }

  function handleDateFormatChange(val: string) {
    if (preferences) {
      preferences.dateFormat = val as DateFormatPreference;
    }
  }

  // Tab State Persistence
  interface PreferencesTabState {
    preferences: UserPreferences | null;
    selectedAccent: AccentColor;
  }

  const tabStateKey = $derived(tabId || 'settings-preferences');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<PreferencesTabState>(tabStateKey);
    if (saved?.preferences) {
      preferences = saved.preferences;
      selectedAccent = saved.selectedAccent ?? getAccentColor();
    } else {
      untrack(() => loadPreferences());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<PreferencesTabState>(tabStateKey, {
      preferences: preferences ? { ...preferences } : null,
      selectedAccent,
    });
  });

  async function loadPreferences() {
    loading = true;
    pageError = null;

    try {
      const loaded = await SettingsService.getUserPreferences();
      // API, status
      loaded.theme = currentThemeAsPreference();
      preferences = loaded;
    } catch (err) {
      pageError = err instanceof Error ? err.message : 'Failed to load preferences';
      toast.error('Failed to load preferences');
    } finally {
      loading = false;
    }
  }

  async function handleSave() {
    if (!preferences) return;

    saving = true;

    try {
      await SettingsService.saveUserPreferences(preferences);
      toast.success('Preferences saved successfully');
    } catch {
      toast.error('Failed to save preferences');
    } finally {
      saving = false;
    }
  }

</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if pageError}
    <div class="flex-1 flex items-center justify-center">
      <div class="error-state">
        <p>{pageError}</p>
        <Button variant="secondary" onclick={loadPreferences}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Preferences" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={loadPreferences} />
      <IconButton icon="check" variant="success" size="sm" title="Save" onclick={handleSave} disabled={saving} />
    {/snippet}
  </PageHeader>
  <SingleColumnLayout>
  {#if preferences}
    <!-- ================================================================ -->
    <!-- Appearance -->
    <!-- ================================================================ -->
    <SettingGroupHeader title="Appearance" />

      <SettingItem
        category="Appearance"
        title="Theme"
        description="Controls the color theme of the interface."
      >
        <fieldset class="contents">
          <legend class="sr-only">Theme</legend>
          <RadioGroup
            options={themeOptions}
            value={themeStr}
            onchange={handleThemeChange}
            name="theme"
            size="sm"
          />
        </fieldset>
      </SettingItem>

      <SettingItem
        category="Appearance"
        title="Accent Color"
        description="Controls the accent color used for highlights and focus indicators."
      >
        <fieldset class="contents">
          <legend class="sr-only">Accent Color</legend>
          <div class="accent-grid" role="radiogroup" aria-label="Accent Color">
            {#each ACCENT_COLORS as { id, label, color } (id)}
              <Button
                variant="unstyled"
                class="accent-swatch {selectedAccent === id ? 'selected' : ''}"
                style="--swatch-color: {color}"
                title={label}
                role="radio"
                aria-checked={selectedAccent === id}
                onclick={() => handleAccentChange(id)}
              >
                {#if selectedAccent === id}
                  <svg viewBox="0 0 16 16" fill="currentColor" width="12" height="12">
                    <path d="M13.78 4.22a.75.75 0 0 1 0 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L2.22 9.28a.75.75 0 0 1 1.06-1.06L6 10.94l6.72-6.72a.75.75 0 0 1 1.06 0Z" />
                  </svg>
                {/if}
              </Button>
            {/each}
          </div>
          <span class="accent-label">{ACCENT_COLORS.find((c: { id: string; label: string }) => c.id === selectedAccent)?.label ?? ''}</span>
        </fieldset>
      </SettingItem>

      <!-- ================================================================ -->
      <!-- Behavior -->
      <!-- ================================================================ -->
      <SettingGroupHeader title="Behavior" />

      <SettingItem
        category="Behavior"
        title="Toast Duration"
        description="Controls how long notification toasts are displayed."
      >
        <fieldset class="contents">
          <legend class="sr-only">Toast Duration</legend>
          <RadioGroup
            options={toastDurationOptions}
            value={toastDurationStr}
            onchange={handleToastDurationChange}
            name="toastDuration"
            size="sm"
          />
        </fieldset>
      </SettingItem>

      <SettingItem
        category="Behavior"
        title="Date Format"
        description="Controls how timestamps are displayed across the application."
      >
        <fieldset class="contents">
          <legend class="sr-only">Date Format</legend>
          <RadioGroup
            options={dateFormatOptions}
            value={dateFormatStr}
            onchange={handleDateFormatChange}
            name="dateFormat"
            orientation="vertical"
            size="sm"
          />
        </fieldset>
      </SettingItem>

  {/if}
  </SingleColumnLayout>
  {/if}
</PageShell>

<style>
  .error-state {
    padding: 4rem;
    text-align: center;
    color: var(--color-error-600, #dc2626);
  }

  .error-state p {
    margin-bottom: 1rem;
  }

  /* Accent color swatches */
  .accent-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  :global(.accent-swatch) {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    border: 2px solid transparent;
    background-color: var(--swatch-color);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    padding: 0;
    cursor: pointer;
    transition: transform 0.15s ease, border-color 0.15s ease;
  }

  :global(.accent-swatch:hover) {
    transform: scale(1.15);
  }

  :global(.accent-swatch.selected) {
    border-color: var(--color-text-primary, #111);
    box-shadow: 0 0 0 2px var(--color-bg-primary, #fff), 0 0 0 4px var(--swatch-color);
  }

  .accent-label {
    display: block;
    margin-top: 0.375rem;
    font-size: 0.75rem;
    color: var(--color-text-secondary, #6b7280);
  }

</style>
