<!--
  TypingIndicator Component
  Shows who is currently typing
-->
<script lang="ts">
  interface Props {
    typingUsers: Map<string, string>;
    class?: string;
  }

  let { typingUsers, class: className = '' }: Props = $props();

  const displayText = $derived.by(() => {
    const names = Array.from(typingUsers.values());
    if (names.length === 0) return '';
    if (names.length === 1) return `${names[0]} is typing...`;
    if (names.length === 2) return `${names[0]} and ${names[1]} are typing...`;
    return `${names[0]} and ${names.length - 1} others are typing...`;
  });

  const isVisible = $derived(typingUsers.size > 0);
</script>

{#if isVisible}
  <div
    class="flex items-center gap-2 px-4 py-2 text-sm text-[var(--color-text-muted)] {className}"
  >
    <!-- Animated dots -->
    <div class="flex gap-0.5">
      <span class="w-1.5 h-1.5 rounded-full bg-[var(--color-text-muted)] animate-bounce" style="animation-delay: 0ms;"></span>
      <span class="w-1.5 h-1.5 rounded-full bg-[var(--color-text-muted)] animate-bounce" style="animation-delay: 150ms;"></span>
      <span class="w-1.5 h-1.5 rounded-full bg-[var(--color-text-muted)] animate-bounce" style="animation-delay: 300ms;"></span>
    </div>
    <span>{displayText}</span>
  </div>
{/if}

<style>
  @keyframes bounce {
    0%, 60%, 100% {
      transform: translateY(0);
    }
    30% {
      transform: translateY(-4px);
    }
  }

  .animate-bounce {
    animation: bounce 1.4s infinite ease-in-out;
  }
</style>
