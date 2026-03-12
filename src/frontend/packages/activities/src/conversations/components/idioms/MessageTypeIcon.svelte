<!--
  MessageTypeIcon Component
  Displays an icon based on message type with appropriate styling
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { MESSAGE_TYPE_STYLES, type MessageType } from '../../types';

  interface Props {
    type: MessageType;
    size?: 'sm' | 'md' | 'lg';
    class?: string;
  }

  let { type, size = 'md', class: className = '' }: Props = $props();

  const style = $derived(MESSAGE_TYPE_STYLES[type] ?? MESSAGE_TYPE_STYLES.Normal);
</script>

{#if style}
<span
  class="inline-flex items-center justify-center rounded-full {style.bgColor ?? ''} {style.textColor ?? ''} {className}"
  class:p-1={size === 'sm'}
  class:p-1.5={size === 'md'}
  class:p-2={size === 'lg'}
>
  <Icon
    name={style.icon ?? 'message-circle'}
    size={size === 'sm' ? 'xs' : size === 'md' ? 'sm' : 'md'}
  />
</span>
{/if}
