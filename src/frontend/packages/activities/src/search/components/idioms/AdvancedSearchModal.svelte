<script lang="ts">
  /**
   * Advanced Search Section
   * Collapsible inline section for advanced search options (replaces modal)
   */

  import { Icon, Button, Input, Select } from '@sddp/ui';
  import type { AdvancedSearchOptions } from '../../types';

  interface Props {
    isOpen?: boolean;
    onSearch?: (options: AdvancedSearchOptions) => void;
    onClose?: () => void;
  }

  let { isOpen = false, onSearch, onClose }: Props = $props();

  let options = $state<AdvancedSearchOptions>({
    allWords: '',
    exactPhrase: '',
    anyWords: '',
    noneWords: '',
    titleContains: '',
  });

  function handleSearch() {
    onSearch?.(options);
    onClose?.();
  }

  function handleClose() {
    onClose?.();
  }

  const authorOptions = [
    { label: 'Select user...', value: '' },
    { label: 'John Doe', value: 'user-1' },
    { label: 'Jane Smith', value: 'user-2' },
  ];
</script>

{#if isOpen}
  <div class="border border-[var(--color-border-primary)] rounded-lg bg-[var(--color-bg-secondary)] overflow-hidden">
    <div class="flex items-center justify-between px-3 py-2 border-b border-[var(--color-border-primary)]">
      <div class="flex items-center gap-2">
        <Icon name="sliders" size="sm" class="text-[var(--color-text-tertiary)]" />
        <span class="text-sm font-medium text-[var(--color-text-primary)]">Advanced Search</span>
      </div>
      <div class="flex items-center gap-1">
        <Button variant="primary" size="sm" onclick={handleSearch}>Search</Button>
        <Button variant="ghost" size="sm" onclick={handleClose}>
          <Icon name="x" size="sm" />
        </Button>
      </div>
    </div>

    <div class="p-3 space-y-3">
      <div class="grid grid-cols-2 gap-3">
        <Input label="All of these words" bind:value={options.allWords} placeholder="authentication security" />
        <Input label="This exact phrase" bind:value={options.exactPhrase} placeholder="user authentication" />
        <Input label="Any of these words" bind:value={options.anyWords} placeholder="JWT OAuth SAML" />
        <Input label="None of these words" bind:value={options.noneWords} placeholder="deprecated legacy" />
      </div>

      <div class="border-t border-[var(--color-border-secondary)] pt-3">
        <div class="grid grid-cols-3 gap-3">
          <Input label="Title contains" bind:value={options.titleContains} />
          <Select label="Author is" bind:value={options.authorId} options={authorOptions} placeholder="" />
          <div>
            <span class="block text-xs text-[var(--color-text-muted)] mb-1">Created between</span>
            <div class="flex items-center gap-1">
              <Input
                type="date"
                value={options.dateRange?.from || ''}
                onchange={(e) => {
                  options.dateRange = {
                    from: (e.currentTarget as HTMLInputElement).value,
                    to: options.dateRange?.to || '',
                  };
                }}
              />
              <span class="text-xs text-[var(--color-text-tertiary)]">~</span>
              <Input
                type="date"
                value={options.dateRange?.to || ''}
                onchange={(e) => {
                  options.dateRange = {
                    from: options.dateRange?.from || '',
                    to: (e.currentTarget as HTMLInputElement).value,
                  };
                }}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
{/if}
