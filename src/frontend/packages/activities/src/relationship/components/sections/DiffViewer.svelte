<!-- Section: DiffViewer -->
<script lang="ts">
  import type { SpecDiffResult, DiffChangeType } from '../../types';
  import { DIFF_CHANGE_TYPE_STYLES } from '../../types';
  import { Icon, Button, Checkbox } from '@sddp/ui';

  interface Props {
    diff: SpecDiffResult;
    mode?: 'side-by-side' | 'inline';
    showUnchanged?: boolean;
    class?: string;
  }

  let {
    diff,
    mode = 'side-by-side',
    showUnchanged = false,
    class: className = '',
  }: Props = $props();

  let currentMode = $derived(mode);

  const displayedChanges = $derived(
    showUnchanged
      ? diff.changes
      : diff.changes.filter((c) => c.changeType !== 'Unchanged')
  );

  const changedCount = $derived(diff.addedCount + diff.removedCount + diff.modifiedCount);

  function getChangeStyle(changeType: DiffChangeType) {
    return DIFF_CHANGE_TYPE_STYLES[changeType];
  }
</script>

<div class="flex flex-col {className}">
  <!-- Header -->
  <div class="flex items-center justify-between mb-4 pb-3 border-b border-gray-200 dark:border-gray-700">
    <div class="flex items-center gap-4">
      <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300">
        Version Comparison
      </h3>
      <div class="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
        <span class="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded">
          v{diff.spec1Version}
        </span>
        <Icon name="arrow-right" size="xs" />
        <span class="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded">
          v{diff.spec2Version}
        </span>
      </div>
    </div>

    <div class="flex items-center gap-3">
      <!-- Stats -->
      <div class="flex items-center gap-2 text-xs">
        {#if diff.addedCount > 0}
          <span class="flex items-center gap-1 text-green-600 dark:text-green-400">
            <Icon name="plus" size="xs" />
            {diff.addedCount} added
          </span>
        {/if}
        {#if diff.removedCount > 0}
          <span class="flex items-center gap-1 text-red-600 dark:text-red-400">
            <Icon name="minus" size="xs" />
            {diff.removedCount} removed
          </span>
        {/if}
        {#if diff.modifiedCount > 0}
          <span class="flex items-center gap-1 text-amber-600 dark:text-amber-400">
            <Icon name="edit-2" size="xs" />
            {diff.modifiedCount} modified
          </span>
        {/if}
      </div>

      <!-- Mode toggle -->
      <div class="flex items-center border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
        <Button
          variant="unstyled"
          class="px-2 py-1 text-xs transition-colors
            {currentMode === 'side-by-side'
              ? 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300'
              : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-750'}"
          onclick={() => (currentMode = 'side-by-side')}
        >
          Side by Side
        </Button>
        <Button
          variant="unstyled"
          class="px-2 py-1 text-xs transition-colors border-l border-gray-200 dark:border-gray-700
            {currentMode === 'inline'
              ? 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300'
              : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-750'}"
          onclick={() => (currentMode = 'inline')}
        >
          Inline
        </Button>
      </div>

      <!-- Show unchanged toggle -->
      <label class="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 cursor-pointer">
        <Checkbox
          unstyled
          bind:checked={showUnchanged}
          class="rounded text-blue-500"
        />
        Show unchanged
      </label>
    </div>
  </div>

  <!-- Empty state -->
  {#if changedCount === 0}
    <div class="text-center py-8 text-gray-500 dark:text-gray-400">
      <Icon name="check-circle" size="xl" class="mx-auto mb-2 text-green-500" />
      <p>No differences found</p>
      <p class="text-xs mt-1">Both versions are identical</p>
    </div>
  {:else if displayedChanges.length === 0}
    <div class="text-center py-8 text-gray-500 dark:text-gray-400">
      <p class="text-sm">All fields are unchanged</p>
      <p class="text-xs mt-1">Enable "Show unchanged" to see all fields</p>
    </div>
  {:else}
    <!-- Diff content -->
    <div class="space-y-3 overflow-auto">
      {#each displayedChanges as change (change.fieldName)}
        {@const style = getChangeStyle(change.changeType)}
        <div
          class="rounded-lg border {style.borderColor} overflow-hidden"
        >
          <!-- Field header -->
          <div class="flex items-center justify-between px-3 py-2 {style.bgColor}">
            <div class="flex items-center gap-2">
              <Icon name={style.icon} size="sm" class={style.color} />
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
                {change.fieldLabel}
              </span>
            </div>
            <span class="text-xs {style.color}">
              {style.label}
            </span>
          </div>

          <!-- Field content -->
          {#if currentMode === 'side-by-side'}
            <div class="grid grid-cols-2 divide-x divide-gray-200 dark:divide-gray-700">
              <!-- Old value -->
              <div class="p-3">
                <div class="text-xs text-gray-400 mb-1">Previous (v{diff.spec1Version})</div>
                {#if change.oldValue}
                  <div
                    class="text-sm text-gray-600 dark:text-gray-400 whitespace-pre-wrap break-words
                      {change.changeType === 'Removed' || change.changeType === 'Modified'
                        ? 'bg-red-50 dark:bg-red-950 p-2 rounded border-l-2 border-red-300 dark:border-red-700'
                        : ''}"
                  >
                    {change.oldValue}
                  </div>
                {:else}
                  <div class="text-sm text-gray-400 italic">Empty</div>
                {/if}
              </div>
              <!-- New value -->
              <div class="p-3">
                <div class="text-xs text-gray-400 mb-1">Current (v{diff.spec2Version})</div>
                {#if change.newValue}
                  <div
                    class="text-sm text-gray-600 dark:text-gray-400 whitespace-pre-wrap break-words
                      {change.changeType === 'Added' || change.changeType === 'Modified'
                        ? 'bg-green-50 dark:bg-green-950 p-2 rounded border-l-2 border-green-300 dark:border-green-700'
                        : ''}"
                  >
                    {change.newValue}
                  </div>
                {:else}
                  <div class="text-sm text-gray-400 italic">Empty</div>
                {/if}
              </div>
            </div>
          {:else}
            <!-- Inline mode -->
            <div class="p-3 space-y-2">
              {#if change.changeType === 'Modified' || change.changeType === 'Removed'}
                <div class="flex items-start gap-2">
                  <span class="shrink-0 w-5 h-5 flex items-center justify-center bg-red-100 dark:bg-red-900 text-red-600 dark:text-red-400 rounded text-xs">
                    -
                  </span>
                  <div class="text-sm text-gray-600 dark:text-gray-400 bg-red-50 dark:bg-red-950 p-2 rounded flex-1 whitespace-pre-wrap break-words">
                    {change.oldValue || '(empty)'}
                  </div>
                </div>
              {/if}
              {#if change.changeType === 'Modified' || change.changeType === 'Added'}
                <div class="flex items-start gap-2">
                  <span class="shrink-0 w-5 h-5 flex items-center justify-center bg-green-100 dark:bg-green-900 text-green-600 dark:text-green-400 rounded text-xs">
                    +
                  </span>
                  <div class="text-sm text-gray-600 dark:text-gray-400 bg-green-50 dark:bg-green-950 p-2 rounded flex-1 whitespace-pre-wrap break-words">
                    {change.newValue || '(empty)'}
                  </div>
                </div>
              {/if}
              {#if change.changeType === 'Unchanged'}
                <div class="text-sm text-gray-500 dark:text-gray-400 whitespace-pre-wrap break-words">
                  {change.oldValue || '(empty)'}
                </div>
              {/if}
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>
