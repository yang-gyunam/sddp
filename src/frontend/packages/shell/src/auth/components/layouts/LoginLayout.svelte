<!--
  LoginLayout Template
  Centered authentication layout with branding
-->
<script lang="ts">
  import { Typography } from '@sddp/ui';

  interface Props {
    title?: string;
    subtitle?: string;
    appName?: string;
    helpText?: string;
    logoSrc?: string;
    children: import('svelte').Snippet;
    footer?: import('svelte').Snippet;
  }

  let {
    title = 'Sign in to SDDP',
    subtitle,
    appName = 'SDDP',
    helpText = 'Need help? Contact your system administrator',
    logoSrc,
    children,
    footer,
  }: Props = $props();

  const currentYear = new Date().getFullYear();
</script>

<div class="min-h-screen flex flex-col bg-[var(--color-bg-page)]">
  <!-- Main Content -->
  <main class="flex-1 flex items-center justify-center p-4">
    <div class="w-full max-w-md">
      <!-- Logo and Title -->
      <div class="text-center mb-8">
        {#if logoSrc}
          <img src={logoSrc} alt={appName} class="h-16 mb-4 inline-block" />
        {/if}
        <Typography as="h1" variant="h3" class="mb-2">
          {title}
        </Typography>
        {#if subtitle}
          <Typography variant="body2" color="secondary">
            {subtitle}
          </Typography>
        {/if}
      </div>

      <!-- Card Container -->
      <div class="bg-[var(--color-bg-card)] rounded-lg border border-[var(--color-border)] shadow-lg p-6">
        {@render children()}
      </div>

      <!-- Footer -->
      {#if footer}
        <div class="mt-6 text-center">
          {@render footer()}
        </div>
      {/if}

      <!-- Help Text -->
      {#if helpText}
        <div class="mt-4 text-center">
          <Typography variant="caption" color="secondary">
            {helpText}
          </Typography>
        </div>
      {/if}
    </div>
  </main>

  <!-- Bottom Bar -->
  <footer class="py-4 border-t border-[var(--color-border)]">
    <div class="flex items-center justify-center gap-1 text-[var(--color-text-secondary)]">
      <span class="text-xs">© {currentYear} {appName}</span>
      <span class="text-xs opacity-50">│</span>
      <a href="#privacy" class="text-xs hover:text-[var(--color-text-primary)] hover:underline">Privacy</a>
      <span class="text-xs opacity-50">│</span>
      <a href="#terms" class="text-xs hover:text-[var(--color-text-primary)] hover:underline">Terms</a>
    </div>
  </footer>
</div>
