/**
 * Shared Idiom Types
 */

// StatCard health types
export type HealthStatus = 'healthy' | 'warning' | 'error';

export interface HealthMetric {
  label: string;
  value: string;
}
