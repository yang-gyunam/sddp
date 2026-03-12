<!--
  LoginForm Molecule
  Login form with validation and error handling
-->
<script lang="ts">
  import { Button, Checkbox, Input } from '@sddp/ui';
  import { validateField, required, minLength } from '../../../utils/validation.utils';
  import type { LoginRequest } from '../../types';

  interface Props {
    onSubmit: (credentials: LoginRequest) => Promise<void>;
    error?: string | null;
    isLoading?: boolean;
  }

  let { onSubmit, error = null, isLoading = false }: Props = $props();

  // Form state
  let username = $state('');
  let password = $state('');

  // Validation state
  let usernameError = $state('');
  let passwordError = $state('');
  let touched = $state({ username: false, password: false });

  // Validation rules
  const usernameRules = [
    required('Username is required'),
    minLength(3, 'Username must be at least 3 characters'),
  ];
  const passwordRules = [
    required('Password is required'),
    minLength(6, 'Password must be at least 6 characters'),
  ];

  // Validate form fields
  function validateUsername(): boolean {
    const errors = validateField(username, usernameRules);
    usernameError = errors[0] || '';
    return errors.length === 0;
  }

  function validatePassword(): boolean {
    const errors = validateField(password, passwordRules);
    passwordError = errors[0] || '';
    return errors.length === 0;
  }

  function validate(): boolean {
    const isUsernameValid = validateUsername();
    const isPasswordValid = validatePassword();
    return isUsernameValid && isPasswordValid;
  }

  // Handle form submission
  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault();
    touched = { username: true, password: true };

    if (!validate()) {
      return;
    }

    await onSubmit({ username: username.trim(), password });
  }

  // Handle input blur for field-level validation
  function handleUsernameBlur() {
    touched.username = true;
    validateUsername();
  }

  function handlePasswordBlur() {
    touched.password = true;
    validatePassword();
  }
</script>

<form onsubmit={handleSubmit} class="space-y-4">
  <!-- Server Error Message -->
  {#if error}
    <div
      class="p-3 rounded-md bg-[var(--color-error-50)] dark:bg-[var(--color-error-900)]/10 border border-[var(--color-error-200)] dark:border-[var(--color-error-800)]"
      role="alert"
    >
      <p class="text-sm text-[var(--color-error-700)] dark:text-[var(--color-error-400)]">
        {error}
      </p>
    </div>
  {/if}

  <!-- Username Field -->
  <Input
    type="text"
    label="Username"
    placeholder="Enter your username"
    bind:value={username}
    error={touched.username ? usernameError : ''}
    disabled={isLoading}
    required
    autocomplete="username"
    onblur={handleUsernameBlur}
  />

  <!-- Password Field -->
  <Input
    type="password"
    label="Password"
    placeholder="Enter your password"
    bind:value={password}
    error={touched.password ? passwordError : ''}
    disabled={isLoading}
    required
    autocomplete="current-password"
    onblur={handlePasswordBlur}
  />

  <!-- Remember Me & Forgot Password -->
  <div class="flex items-center justify-between text-sm">
    <label class="flex items-center gap-2 cursor-pointer">
      <Checkbox
        unstyled
        class="rounded border-[var(--color-border)] text-[var(--color-accent-primary)] focus:border-[var(--color-accent-primary)] focus:outline-none"
      />
      <span class="text-[var(--color-text-secondary)]">Remember me</span>
    </label>
    <a
      href="#forgot-password"
      class="text-[#4338ca] hover:text-[#3730a3] hover:underline"
    >
      Forgot password?
    </a>
  </div>

  <!-- Submit Button -->
  <Button
    type="submit"
    variant="unstyled"
    fullWidth
    loading={isLoading}
    disabled={isLoading}
    class="inline-flex items-center justify-center w-full rounded-md px-4 py-2 text-sm font-medium
      bg-[#4338ca] text-white hover:bg-[#3730a3]
      focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[#4338ca]
      disabled:opacity-50 disabled:cursor-not-allowed"
  >
    {isLoading ? 'Signing in...' : 'Sign in'}
  </Button>
</form>
