<script lang="ts">
  import { LoginLayout } from '../components/layouts';
  import { Input, Button } from '@sddp/ui';
  import { changePassword } from '../stores/auth.store';

  interface Props {
    onChanged: () => void;
  }

  let { onChanged }: Props = $props();

  let currentPassword = $state('');
  let newPassword = $state('');
  let confirmPassword = $state('');
  let error = $state<string | null>(null);
  let isLoading = $state(false);

  async function handleSubmit() {
    error = null;

    if (!currentPassword) {
      error = 'Current password is required';
      return;
    }
    if (newPassword.length < 8) {
      error = 'New password must be at least 8 characters';
      return;
    }
    if (newPassword !== confirmPassword) {
      error = 'Passwords do not match';
      return;
    }

    isLoading = true;
    try {
      await changePassword(currentPassword, newPassword);
      onChanged();
    } catch (err) {
      error = err instanceof Error ? err.message : 'An unexpected error occurred';
    } finally {
      isLoading = false;
    }
  }
</script>

<LoginLayout
  title="Password Change Required"
  subtitle="You must change your password before continuing."
  helpText="Contact your administrator if you need assistance."
>
  <form
    class="flex flex-col gap-4"
    onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}
  >
    <Input
      type="password"
      label="Current Password"
      placeholder="Enter current password"
      bind:value={currentPassword}
      disabled={isLoading}
      autocomplete="current-password"
    />
    <Input
      type="password"
      label="New Password"
      placeholder="Min 8 chars, uppercase, lowercase, digit, special"
      bind:value={newPassword}
      disabled={isLoading}
      autocomplete="new-password"
    />
    <Input
      type="password"
      label="Confirm New Password"
      placeholder="Re-enter new password"
      bind:value={confirmPassword}
      disabled={isLoading}
      autocomplete="new-password"
    />

    {#if error}
      <p class="text-sm text-[var(--color-error-600)]">{error}</p>
    {/if}

    <Button variant="primary" type="submit" disabled={isLoading} class="w-full">
      {isLoading ? 'Changing...' : 'Change Password'}
    </Button>
  </form>
</LoginLayout>
