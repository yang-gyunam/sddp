import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Badge from '../Badge.svelte';

describe('Badge', () => {
  it('should render with default variant', () => {
    const { container } = render(Badge);
    const span = container.querySelector('span');
    expect(span).not.toBeNull();
    expect(span?.className).toContain('rounded-full');
  });

  it('should apply variant classes for success', () => {
    const { container } = render(Badge, { props: { variant: 'success' } });
    const span = container.querySelector('span');
    expect(span?.className).toContain('success');
  });

  it('should apply variant classes for error', () => {
    const { container } = render(Badge, { props: { variant: 'error' } });
    const span = container.querySelector('span');
    expect(span?.className).toContain('error');
  });

  it('should apply size classes for sm', () => {
    const { container } = render(Badge, { props: { size: 'sm' } });
    const span = container.querySelector('span');
    expect(span?.className).toContain('text-xs');
  });

  it('should apply size classes for lg', () => {
    const { container } = render(Badge, { props: { size: 'lg' } });
    const span = container.querySelector('span');
    expect(span?.className).toContain('py-1');
  });

  it('should render close button when closable', () => {
    const { container } = render(Badge, { props: { closable: true } });
    const closeButton = container.querySelector('button[aria-label="Remove"]');
    expect(closeButton).not.toBeNull();
  });

  it('should not render close button by default', () => {
    const { container } = render(Badge);
    const closeButton = container.querySelector('button[aria-label="Remove"]');
    expect(closeButton).toBeNull();
  });
});
