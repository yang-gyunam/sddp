import type { StatusReading } from '../types/status-monitor.types';

export const statusHistoryLimit = 12;

const states = {
  Healthy: { label: 'Operational', variant: 'success', icon: 'check', tone: 'up' },
  Warning: { label: 'Degraded', variant: 'warning', icon: 'alert-triangle', tone: 'warn' },
  Unhealthy: { label: 'Unavailable', variant: 'error', icon: 'x-circle', tone: 'down' },
  Disabled: { label: 'Disabled', variant: 'secondary', icon: 'pause', tone: 'idle' },
  NotConfigured: { label: 'Not configured', variant: 'secondary', icon: 'minus', tone: 'idle' },
  Unknown: { label: 'Unknown', variant: 'secondary', icon: 'help-circle', tone: 'idle' },
} as const;
export function presentation(status: string) {
  return states[status as keyof typeof states] ?? states.Unknown;
}

export function needsAttention(s: StatusReading) {
  return (
    ['Unhealthy', 'Warning', 'Unknown'].includes(s.status) ||
    (s.status === 'NotConfigured' && s.required)
  );
}
