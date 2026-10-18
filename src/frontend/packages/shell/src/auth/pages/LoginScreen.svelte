<!--
  LoginScreen Page
  Complete login page combining layout and form
-->
<script lang="ts">
  import { LoginLayout } from '../components/layouts';
  import { LoginForm } from '../components/idioms';
  import { Typography } from '@sddp/ui';
  import { login, TenantSelectionRequiredError } from '../stores/auth.store';
  import type { LoginRequest } from '../types';

  interface Props {
    onLoginSuccess?: () => void;
    logoSrc?: string;
    sessionExpired?: boolean;
  }

  let { onLoginSuccess, logoSrc, sessionExpired = false }: Props = $props();

  // Login state
  let submitError = $state<string | null>(null);
  let error = $derived(
    submitError ?? (sessionExpired ? 'Your session expired. Please sign in again.' : null)
  );
  let isLoading = $state(false);
  let tenantIds = $state<string[]>([]);

  // Handle login submission
  async function handleLogin(credentials: LoginRequest) {
    submitError = null;
    isLoading = true;

    try {
      await login(credentials);
      onLoginSuccess?.();
    } catch (err) {
      tenantIds = err instanceof TenantSelectionRequiredError ? err.tenantIds : [];
      submitError = err instanceof Error ? err.message : 'An unexpected error occurred';
    } finally {
      isLoading = false;
    }
  }
</script>

<LoginLayout title="Sign in to SDDP" subtitle="Spec-Driven Design Platform" {logoSrc}>
  <LoginForm
    onSubmit={handleLogin}
    {error}
    {isLoading}
    {tenantIds}
    onCredentialsChange={() => {
      tenantIds = [];
      submitError = null;
    }}
  />

  {#snippet footer()}
    <Typography variant="body2" color="secondary">
      Don't have an account?
      <a href="#signup" class="text-[#4338ca] hover:text-[#3730a3] hover:underline font-medium">
        Sign up
      </a>
    </Typography>
  {/snippet}
</LoginLayout>
