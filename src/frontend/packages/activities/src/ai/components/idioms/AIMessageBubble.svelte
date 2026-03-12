<!--
  AIMessageBubble Component
  Displays an AI message in a chat-like bubble format
-->
<script lang="ts">
  import { Icon, Spinner } from '@sddp/ui';
  import { formatTime as formatTimeUtil } from '@sddp/shell';
  import type { AiMessage } from '../../types';
  import AiAnalysisTypeBadge from './AiAnalysisTypeBadge.svelte';

  interface Props {
    message: AiMessage;
    showTimestamp?: boolean;
    showMetadata?: boolean;
    class?: string;
  }

  let {
    message,
    showTimestamp = true,
    showMetadata = false,
    class: className = '',
  }: Props = $props();

  const isUser = $derived(message.role === 'user');
  const isSystem = $derived(message.role === 'system');
  const isStreaming = $derived(message.status === 'streaming');
  const isError = $derived(message.status === 'error');

  function formatTimestamp(timestamp: string): string {
    return formatTimeUtil(timestamp, { locale: 'en-US' });
  }
</script>

<div
  class="flex {isUser ? 'justify-end' : 'justify-start'} {className}"
>
  <div
    class="max-w-[85%] rounded-lg px-3 py-2
      {isUser
        ? 'bg-blue-600 text-white'
        : isSystem
          ? 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400 text-sm italic'
          : 'bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200'}
      {isError ? 'border border-red-300 dark:border-red-700' : ''}"
  >
    <!-- Role indicator for assistant -->
    {#if !isUser && !isSystem}
      <div class="flex items-center gap-1.5 mb-1 text-xs text-gray-500 dark:text-gray-400">
        <Icon name="cpu" size="sm" class="text-purple-500" />
        <span>AI Assistant</span>
        {#if message.metadata?.analysisType}
          <span>·</span>
          <AiAnalysisTypeBadge
            type={message.metadata.analysisType as import('../../../ai/types/base.types').AiAnalysisType}
            size="sm"
            showLabel={false}
          />
        {/if}
      </div>
    {/if}

    <!-- Content -->
    <div class="whitespace-pre-wrap break-words">
      {message.content}
      {#if isStreaming}
        <span class="inline-block w-1.5 h-4 bg-current animate-pulse ml-0.5"></span>
      {/if}
    </div>

    <!-- Error indicator -->
    {#if isError}
      <div class="flex items-center gap-1 mt-1 text-xs text-red-500 dark:text-red-400">
        <Icon name="alert-circle" size="sm" />
        <span>Failed to complete</span>
      </div>
    {/if}

    <!-- Footer -->
    <div class="flex items-center justify-between gap-2 mt-1.5">
      {#if showTimestamp}
        <span class="text-xs {isUser ? 'text-blue-200' : 'text-gray-400'}">
          {formatTimestamp(message.timestamp ?? message.createdAt)}
        </span>
      {/if}

      {#if showMetadata && message.metadata}
        <div class="flex items-center gap-2 text-xs {isUser ? 'text-blue-200' : 'text-gray-400'}">
          {#if message.metadata.model}
            <span>{message.metadata.model}</span>
          {/if}
          {#if message.metadata.tokens}
            <span>·</span>
            <span>{message.metadata.tokens} tokens</span>
          {/if}
        </div>
      {/if}

      <!-- Status indicator -->
      {#if message.status === 'pending'}
        <Spinner size="sm" />
      {/if}
    </div>
  </div>
</div>
