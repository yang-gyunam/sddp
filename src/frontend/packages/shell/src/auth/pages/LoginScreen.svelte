<!--
  LoginScreen Page
  Complete login page combining layout and form
-->
<script lang="ts">
  import { LoginLayout } from '../components/layouts';
  import { LoginForm } from '../components/idioms';
  import { Typography } from '@sddp/ui';
  import { login } from '../stores/auth.store';
  import type { LoginRequest } from '../types';

  interface Props {
    onLoginSuccess?: () => void;
    logoSrc?: string;
  }

  let { onLoginSuccess, logoSrc }: Props = $props();

  // Login state
  let error = $state<string | null>(null);
  let isLoading = $state(false);

  // Handle login submission
  async function handleLogin(credentials: LoginRequest) {
    error = null;
    isLoading = true;

    try {
      await login(credentials);
      onLoginSuccess?.();
    } catch (err) {
      error = err instanceof Error ? err.message : 'An unexpected error occurred';
    } finally {
      isLoading = false;
    }
  }
</script>

<LoginLayout
  title="Sign in to SDDP"
  subtitle="Spec-Driven Design Platform"
  {logoSrc}
>
  <LoginForm
    onSubmit={handleLogin}
    {error}
    {isLoading}
  />

  {#snippet footer()}
    <Typography variant="body2" color="secondary">
      Don't have an account?
      <a
        href="#signup"
        class="text-[#4338ca] hover:text-[#3730a3] hover:underline font-medium"
      >
        Sign up
      </a>
    </Typography>
  {/snippet}
</LoginLayout>
