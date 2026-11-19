<script lang="ts">
  import { Button, Spinner } from '@sddp/ui';
  import { fetchWithAuth } from '../../../shared/api';
  import SettingItem from '../idioms/SettingItem.svelte';

  interface View {
    enabled: boolean;
    hasSavedConsent?: boolean;
    recipient: string | null;
    retentionDays: number | null;
    week: string;
    internalTotals: { action: string; events: number; actors: number }[];
    transferPreview: { counts: Record<string, number> };
  }

  let { tenantId }: { tenantId: string } = $props();
  let view = $state<View | null>(null);
  let error = $state('');
  let busy = $state(false);
  let reload = $state(0);

  $effect(() => {
    const tenant = tenantId;
    void reload;
    let active = true;
    view = null;
    error = '';
    if (tenant) {
      fetchWithAuth<View>('/system/statistics', { tenantId: tenant })
        .then((data) => {
          if (active) view = data;
        })
        .catch(() => {
          if (active) error = 'Could not load usage statistics.';
        });
    }
    return () => {
      active = false;
    };
  });

  async function setSharing(enabled: boolean) {
    const current = view;
    const tenant = tenantId;
    if (!current || busy) return;
    busy = true;
    error = '';
    try {
      await fetchWithAuth('/system/statistics/consent', {
        tenantId: tenant,
        method: 'PUT',
        body: {
          enabled,
          recipient: current.recipient,
          retentionDays: current.retentionDays,
        },
        beforeDispatch: () => {
          if (tenant !== tenantId) throw new Error('Tenant changed');
        },
      });
      if (tenant === tenantId) reload++;
    } catch {
      if (tenant === tenantId) {
        error = enabled
          ? 'Could not update sharing. Reload to review the recipient.'
          : 'Could not withdraw permission. Reload to check the sharing state.';
      }
    } finally {
      busy = false;
    }
  }
</script>

{#if error}
  <p role="alert">{error}</p>
  <Button size="sm" variant="secondary" onclick={() => reload++}>Reload</Button>
{:else if !view}
  <Spinner size="sm" />
{:else}
  <SettingItem
    category="Usage"
    title="Local activity"
    description={'Week starting ' + view.week + ' (UTC)'}
  >
    {#each view.internalTotals as cell (cell.action)}
      <p>{cell.action} · {cell.events} events · {cell.actors} contributors</p>
    {:else}
      <p>No eligible activity.</p>
    {/each}
  </SettingItem>
  <SettingItem
    category="Usage"
    title="Weekly product improvement statistics"
    description="Sharing starts with the first full UTC week after opt-in. Withdrawal stops pending transfers."
  >
    <p>{view.enabled ? 'Sharing enabled' : 'Sharing off'}</p>
    {#if view.recipient}
      <p>Recipient · {view.recipient}</p>
      <p>Retention · {view.retentionDays} days</p>
      <p>Counts are rounded down to five. Each action needs at least five distinct contributors.</p>
      {#each Object.entries(view.transferPreview.counts) as [action, count] (action)}
        <p>{action} · {count}</p>
      {:else}
        <p>No counts qualify for transfer this week.</p>
      {/each}
    {:else}
      <p>The installation has no configured recipient.</p>
    {/if}
    {#if view.recipient && !view.enabled}
      <Button size="sm" variant="secondary" onclick={() => setSharing(true)} disabled={busy}>
        Allow weekly transfer
      </Button>
    {/if}
    {#if view.enabled || view.hasSavedConsent}
      <Button size="sm" variant="secondary" onclick={() => setSharing(false)} disabled={busy}>
        {view.enabled ? 'Withdraw permission' : 'Withdraw saved permission'}
      </Button>
    {/if}
  </SettingItem>
{/if}
