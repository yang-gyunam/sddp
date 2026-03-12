<!--
  Avatar Component
  Renders a user avatar with consistent handling of all avatar types:
  - AI type (gradient)
  - Custom color (color:#hex)
  - Preset emoji (preset:id)
  - External image URL
  - Default (hash-based color + initials)
-->
<script lang="ts">
  import {
    parseAvatarUrl,
    getAvatarHexColor,
    getAvatarInitials,
    getPresetAvatarEmoji,
    AI_AVATAR_COLOR,
  } from '../../utils/avatar.utils';

  type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

  interface Props {
    /** Display name (used for initials and hash-based color) */
    name: string;
    /** Avatar URL (color:#hex, preset:id, image URL, or null) */
    avatarUrl?: string | null;
    /** Whether this is an AI user */
    isAI?: boolean;
    /** Size of the avatar */
    size?: AvatarSize;
    class?: string;
  }

  let {
    name,
    avatarUrl = null,
    isAI = false,
    size = 'sm',
    class: className = '',
  }: Props = $props();

  const sizeClasses: Record<AvatarSize, string> = {
    xs: 'w-5 h-5 text-[0.625rem]',
    sm: 'w-6 h-6 text-xs',
    md: 'w-8 h-8 text-sm',
    lg: 'w-10 h-10 text-base',
    xl: 'w-12 h-12 text-lg',
  };

  const avatar = $derived(parseAvatarUrl(avatarUrl));
  const sizeClass = $derived(sizeClasses[size]);
  const initials = $derived(getAvatarInitials(name));
</script>

{#if isAI}
  <div
    class="rounded-full flex items-center justify-center font-semibold text-white flex-shrink-0 {AI_AVATAR_COLOR} {sizeClass} {className}"
  >
    AI
  </div>
{:else if avatar.type === 'color'}
  <div
    class="rounded-full flex items-center justify-center font-semibold text-white flex-shrink-0 {sizeClass} {className}"
    style="background-color: {avatar.value}"
  >
    {initials}
  </div>
{:else if avatar.type === 'preset'}
  <div
    class="rounded-full flex items-center justify-center flex-shrink-0 bg-[var(--color-bg-tertiary)] {sizeClass} {className}"
  >
    {getPresetAvatarEmoji(avatar.value)}
  </div>
{:else if avatar.type === 'image'}
  <img
    src={avatar.value}
    alt={name}
    class="rounded-full object-cover flex-shrink-0 {sizeClass} {className}"
  />
{:else}
  <div
    class="rounded-full flex items-center justify-center font-semibold text-white flex-shrink-0 {sizeClass} {className}"
    style="background-color: {getAvatarHexColor(name)}"
  >
    {initials}
  </div>
{/if}
