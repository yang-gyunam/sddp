<!-- Section: Settings > ProfileSettingsSection -->
<script lang="ts">
  /**
   * User Profile Settings Page
   * VS Code-style flat settings layout for profile information only.
   */

  import { untrack } from 'svelte';
  import {
    toast, SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState,
    getAvatarHexColor, getAvatarInitials, parseAvatarUrl, getPresetAvatarEmoji,
    PRESET_AVATARS, AVATAR_HEX_COLORS,
  } from '@sddp/shell';
  import { IconButton, Input, Textarea, Button, Spinner } from '@sddp/ui';
  import { getSettingsService } from '../../services/SettingsService';
  import SettingsService from '../../services/SettingsService';
  import type { UserProfile } from '../../types';
  import { SettingItem, SettingGroupHeader } from '../idioms';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();

  let profile = $state<UserProfile | null>(null);
  let loading = $state(false);
  let saving = $state(false);
  let error = $state<string | null>(null);

  // Avatar preview state
  const avatarParsed = $derived(profile ? parseAvatarUrl(profile.avatarUrl) : null);

  function handleAvatarColor(hex: string) {
    if (profile) {
      profile.avatarUrl = `color:${hex}`;
    }
  }

  function handleAvatarPreset(presetId: string) {
    if (profile) {
      profile.avatarUrl = `preset:${presetId}`;
    }
  }

  function handleAvatarReset() {
    if (profile) {
      profile.avatarUrl = undefined;
    }
  }

  // Tab State Persistence
  interface ProfileTabState {
    profile: UserProfile | null;
  }

  const tabStateKey = $derived(tabId || 'settings-profile');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProfileTabState>(tabStateKey);
    if (saved?.profile) {
      profile = saved.profile;
    } else {
      untrack(() => loadProfile());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<ProfileTabState>(tabStateKey, {
      profile: profile ? { ...profile } : null,
    });
  });

  async function loadProfile() {
    loading = true;
    error = null;

    try {
      profile = await SettingsService.getUserProfile();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load profile';
      toast.error('Failed to load profile');
    } finally {
      loading = false;
    }
  }

  async function handleSave() {
    saving = true;

    try {
      if (profile) await SettingsService.saveUserProfile(profile);
      toast.success('Profile saved successfully');
    } catch {
      toast.error('Failed to save profile');
    } finally {
      saving = false;
    }
  }

  // Password change
  const passwordService = getSettingsService();
  let currentPassword = $state('');
  let newPassword = $state('');
  let confirmPassword = $state('');
  let passwordError = $state<string | null>(null);
  let changingPassword = $state(false);

  async function handleChangePassword() {
    passwordError = null;

    if (!currentPassword) {
      passwordError = 'Current password is required';
      return;
    }
    if (newPassword.length < 8) {
      passwordError = 'Password must be at least 8 characters';
      return;
    }
    if (newPassword !== confirmPassword) {
      passwordError = 'Passwords do not match';
      return;
    }

    changingPassword = true;
    try {
      await passwordService.changePassword(currentPassword, newPassword);
      toast.success('Password changed successfully');
      currentPassword = '';
      newPassword = '';
      confirmPassword = '';
    } catch (err) {
      passwordError = err instanceof Error ? err.message : 'Failed to change password';
    } finally {
      changingPassword = false;
    }
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="error-state">
        <p>{error}</p>
        <Button variant="secondary" onclick={loadProfile}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Profile" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={loadProfile} />
      <IconButton icon="check" variant="success" size="sm" title="Save" onclick={handleSave} disabled={saving} />
    {/snippet}
  </PageHeader>

  <SingleColumnLayout>
  {#if profile}
    <!-- ================================================================ -->
    <!-- Avatar -->
    <!-- ================================================================ -->
    <SettingGroupHeader id="section-avatar" title="Avatar" />

    <SettingItem
      category="Avatar"
      title="Profile Avatar"
      description="Choose an avatar color or preset image. This is displayed next to your name."
    >
      <div class="avatar-section">
        <!-- Preview -->
        <div class="avatar-preview">
          {#if avatarParsed?.type === 'color'}
            <div class="avatar-circle" style="background-color: {avatarParsed.value}">
              <span class="avatar-initials">{getAvatarInitials(profile.name)}</span>
            </div>
          {:else if avatarParsed?.type === 'preset'}
            <div class="avatar-circle avatar-circle--preset">
              <span class="avatar-emoji">{getPresetAvatarEmoji(avatarParsed.value)}</span>
            </div>
          {:else}
            <div class="avatar-circle" style="background-color: {getAvatarHexColor(profile.name)}">
              <span class="avatar-initials">{getAvatarInitials(profile.name)}</span>
            </div>
          {/if}
          {#if profile.avatarUrl}
            <IconButton icon="x" variant="ghost" size="sm" onclick={handleAvatarReset} title="Reset to default" />
          {/if}
        </div>

        <!-- Color Picker -->
        <fieldset class="contents">
          <legend class="sr-only">Avatar Color</legend>
          <div class="avatar-picker-group">
            <span class="picker-label">Color</span>
            <div class="color-grid" role="radiogroup" aria-label="Avatar Color">
              {#each AVATAR_HEX_COLORS as hex (hex)}
                <Button
                  variant="unstyled"
                  class="color-swatch {avatarParsed?.type === 'color' && avatarParsed.value === hex ? 'selected' : ''}"
                  style="background-color: {hex}"
                  title="Avatar color {hex}"
                  role="radio"
                  aria-checked={avatarParsed?.type === 'color' && avatarParsed.value === hex}
                  onclick={() => handleAvatarColor(hex)}
                ></Button>
              {/each}
            </div>
          </div>
        </fieldset>

        <!-- Preset Picker -->
        <fieldset class="contents">
          <legend class="sr-only">Avatar Preset</legend>
          <div class="avatar-picker-group">
            <span class="picker-label">Preset</span>
            <div class="preset-grid" role="radiogroup" aria-label="Avatar Preset">
              {#each PRESET_AVATARS as preset (preset.id)}
                <Button
                  variant="unstyled"
                  class="preset-swatch {avatarParsed?.type === 'preset' && avatarParsed.value === preset.id ? 'selected' : ''}"
                  title={preset.label}
                  role="radio"
                  aria-checked={avatarParsed?.type === 'preset' && avatarParsed.value === preset.id}
                  onclick={() => handleAvatarPreset(preset.id)}
                >
                  {preset.emoji}
                </Button>
              {/each}
            </div>
          </div>
        </fieldset>
      </div>
    </SettingItem>

    <!-- ================================================================ -->
    <!-- Basic Information -->
    <!-- ================================================================ -->
    <SettingGroupHeader id="section-basic" title="Basic Information" />

    <SettingItem
      category="Basic Information"
      title="Name"
      description="Your display name shown to other users across the platform."
    >
      <Input type="text" bind:value={profile.name} />
    </SettingItem>

    <SettingItem
      category="Basic Information"
      title="Email"
      description="Your email address for notifications and account recovery."
    >
      <Input type="email" bind:value={profile.email} />
    </SettingItem>

    <SettingItem
      category="Basic Information"
      title="Username"
      description="Your unique identifier. Used for @mentions."
    >
      <Input type="text" bind:value={profile.username} />
    </SettingItem>

    <SettingItem
      category="Basic Information"
      title="Bio"
      description="A short description about yourself."
    >
      <Textarea value={profile.bio ?? ''} oninput={(e) => { if (profile) profile.bio = (e.target as HTMLTextAreaElement).value; }} rows={3} />
    </SettingItem>

    <!-- ================================================================ -->
    <!-- Security -->
    <!-- ================================================================ -->
    <SettingGroupHeader id="section-security" title="Security" />

    <SettingItem
      category="Security"
      title="Change Password"
      description="Update your password. Requires at least 8 characters with uppercase, lowercase, digit, and special character."
    >
      <fieldset class="contents">
        <legend class="sr-only">Change Password</legend>
        <div class="flex flex-col gap-3">
          <Input type="password" placeholder="Current password" bind:value={currentPassword} disabled={changingPassword} autocomplete="current-password" />
          <Input type="password" placeholder="New password" bind:value={newPassword} disabled={changingPassword} autocomplete="new-password" />
          <Input type="password" placeholder="Confirm new password" bind:value={confirmPassword} disabled={changingPassword} autocomplete="new-password" />
          {#if passwordError}
            <p class="text-xs text-[var(--color-error-600)]">{passwordError}</p>
          {/if}
          <div class="flex justify-end">
            <Button variant="primary" onclick={handleChangePassword} disabled={changingPassword}>
              {changingPassword ? 'Changing...' : 'Change Password'}
            </Button>
          </div>
        </div>
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

  /* Avatar Section */
  .avatar-section {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .avatar-preview {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .avatar-circle {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .avatar-circle--preset {
    background: var(--color-bg-tertiary, #f3f4f6);
  }

  .avatar-initials {
    color: white;
    font-size: 1.25rem;
    font-weight: 600;
    line-height: 1;
  }

  .avatar-emoji {
    font-size: 2rem;
    line-height: 1;
  }

  .avatar-picker-group {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .picker-label {
    font-size: 0.75rem;
    color: var(--color-text-tertiary, #9ca3af);
    font-weight: 500;
  }

  .color-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
  }

  :global(.color-swatch) {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    border: 2px solid transparent;
    padding: 0;
    cursor: pointer;
    transition: transform 0.15s ease, border-color 0.15s ease;
  }

  :global(.color-swatch:hover) {
    transform: scale(1.2);
  }

  :global(.color-swatch.selected) {
    border-color: var(--color-text-primary, #111);
    box-shadow: 0 0 0 2px var(--color-bg-primary, #fff), 0 0 0 3px currentColor;
  }

  .preset-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
  }

  :global(.preset-swatch) {
    width: 36px;
    height: 36px;
    border-radius: 8px;
    border: 2px solid transparent;
    background: var(--color-bg-tertiary, #f3f4f6);
    padding: 0;
    cursor: pointer;
    font-size: 1.25rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: transform 0.15s ease, border-color 0.15s ease;
  }

  :global(.preset-swatch:hover) {
    transform: scale(1.1);
    background: var(--color-bg-secondary, #f9fafb);
  }

  :global(.preset-swatch.selected) {
    border-color: var(--color-accent-primary, #6366f1);
    background: var(--color-accent-primary, #6366f1)/10;
  }
</style>
