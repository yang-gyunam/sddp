<!-- Section: VersionTimeline -->
<script lang="ts">
  import type { Spec } from '../../../specs/types';
  import { SPEC_STATUS_STYLES } from '../../../specs/types';
  import { Icon, Button } from '@sddp/ui';
  import { formatDate as formatDateUtil, formatTime as formatTimeUtil } from '@sddp/shell';

  interface Props {
    versions: Spec[];
    currentVersionId?: string;
    onVersionSelect?: (version: Spec) => void;
    onCompare?: (version1: Spec, version2: Spec) => void;
    class?: string;
  }

  let {
    versions,
    currentVersionId,
    onVersionSelect,
    onCompare,
    class: className = '',
  }: Props = $props();

  let selectedForCompare: Spec | null = $state(null);

  // Sort versions by version number (descending - newest first)
  const sortedVersions = $derived(
    [...versions].sort((a, b) => {
      const versionA = parseFloat(a.version) || 0;
      const versionB = parseFloat(b.version) || 0;
      return versionB - versionA;
    })
  );

  function handleVersionClick(version: Spec) {
    if (selectedForCompare) {
      if (selectedForCompare.id !== version.id) {
        onCompare?.(selectedForCompare, version);
      }
      selectedForCompare = null;
    } else {
      onVersionSelect?.(version);
    }
  }

  function handleCompareClick(e: Event, version: Spec) {
    e.stopPropagation();
    if (selectedForCompare?.id === version.id) {
      selectedForCompare = null;
    } else {
      selectedForCompare = version;
    }
  }

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short' });
  }

  function formatTimeStr(dateStr: string): string {
    return formatTimeUtil(dateStr);
  }
</script>

<div class="flex flex-col {className}">
  {#if selectedForCompare}
    <div class="mb-4 p-3 bg-blue-50 dark:bg-blue-950 border border-blue-200 dark:border-blue-800 rounded-lg">
      <div class="flex items-center justify-between">
        <div class="text-sm text-blue-700 dark:text-blue-300">
          <Icon name="git-compare" size="md" class="inline mr-1" />
          Select another version to compare with v{selectedForCompare.version}
        </div>
        <Button
          variant="unstyled"
          class="text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-200"
          onclick={() => (selectedForCompare = null)}
        >
          Cancel
        </Button>
      </div>
    </div>
  {/if}

  <!-- Horizontal timeline -->
  <div class="relative overflow-x-auto pb-4">
    <div class="flex items-start gap-0 min-w-max">
      {#each sortedVersions as version, index (version.id)}
        {@const style = SPEC_STATUS_STYLES[version.status]}
        {@const isCurrent = version.id === currentVersionId}
        {@const isSelected = selectedForCompare?.id === version.id}
        {@const isLast = index === sortedVersions.length - 1}

        <div class="flex items-start">
          <!-- Version node -->
          <div
            class="flex flex-col items-center cursor-pointer group"
            role="button"
            tabindex="0"
            aria-label="Version {version.version}"
            onclick={() => handleVersionClick(version)}
            onkeydown={(e) => e.key === 'Enter' && handleVersionClick(version)}
          >
            <!-- Circle -->
            <div
              class="w-8 h-8 rounded-full flex items-center justify-center border-2 transition-all
                {isCurrent
                  ? 'border-blue-500 bg-blue-500 text-white'
                  : isSelected
                    ? 'border-amber-500 bg-amber-500 text-white'
                    : `${style.borderColor} ${style.bgColor} ${style.textColor} group-hover:scale-110`}"
            >
              <Icon name={style.icon} size="sm" />
            </div>

            <!-- Version info -->
            <div class="mt-2 text-center w-24">
              <div class="text-sm font-medium text-gray-700 dark:text-gray-300">
                v{version.version}
              </div>
              <div class="text-xs text-gray-500 dark:text-gray-400">
                {formatDateStr(version.createdAt)}
              </div>
              <div class="text-xs text-gray-400 dark:text-gray-500">
                {formatTimeStr(version.createdAt)}
              </div>
              <div class="mt-1">
                <span class="text-xs px-1.5 py-0.5 rounded {style.bgColor} {style.textColor} {style.borderColor} border">
                  {style.label}
                </span>
              </div>
              {#if isCurrent}
                <div class="mt-1 text-xs text-blue-600 dark:text-blue-400 font-medium">
                  Current
                </div>
              {/if}

              <!-- Compare button -->
              {#if onCompare}
                <Button
                  variant="unstyled"
                  class="mt-2 text-xs px-2 py-1 rounded border transition-colors
                    {isSelected
                      ? 'border-amber-500 bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300'
                      : 'border-gray-200 hover:border-gray-300 text-gray-500 hover:text-gray-700 dark:border-gray-700 dark:hover:border-gray-600 dark:text-gray-400 dark:hover:text-gray-300'}"
                  onclick={(e) => handleCompareClick(e, version)}
                >
                  {isSelected ? 'Cancel' : 'Compare'}
                </Button>
              {/if}
            </div>
          </div>

          <!-- Connector line -->
          {#if !isLast}
            <div class="flex items-center h-8 px-2">
              <div class="w-16 h-0.5 bg-gray-200 dark:bg-gray-700"></div>
              <Icon name="chevron-left" size="xs" class="text-gray-300 dark:text-gray-600 -ml-1" />
            </div>
          {/if}
        </div>
      {/each}
    </div>
  </div>

  <!-- Empty state -->
  {#if versions.length === 0}
    <div class="text-center py-8 text-gray-500 dark:text-gray-400">
      <Icon name="git-branch" size="xl" class="mx-auto mb-2 opacity-50" />
      <p>No version history</p>
    </div>
  {/if}
</div>
