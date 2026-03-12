import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Icon from '../Icon.svelte';

describe('Icon', () => {
  it('should render SVG element', () => {
    const { container } = render(Icon, { props: { name: 'check' } });
    const svg = container.querySelector('svg');
    expect(svg).not.toBeNull();
  });

  it('should apply default md size class', () => {
    const { container } = render(Icon, { props: { name: 'check' } });
    const svg = container.querySelector('svg');
    expect(svg?.className.baseVal).toContain('w-5');
  });

  it('should apply sm size class', () => {
    const { container } = render(Icon, { props: { name: 'check', size: 'sm' } });
    const svg = container.querySelector('svg');
    expect(svg?.className.baseVal).toContain('w-4');
  });

  it('should apply lg size class', () => {
    const { container } = render(Icon, { props: { name: 'check', size: 'lg' } });
    const svg = container.querySelector('svg');
    expect(svg?.className.baseVal).toContain('w-6');
  });

  it('should render title element when title is provided', () => {
    const { container } = render(Icon, { props: { name: 'check', title: 'Checkmark' } });
    const title = container.querySelector('title');
    expect(title?.textContent).toBe('Checkmark');
  });

  it('should fallback to x icon for unknown icon name', () => {
    const { container } = render(Icon, { props: { name: 'nonexistent-icon-xyz' } });
    // Should render fallback 'x' icon path (M18 6L6 18M6 6l12 12)
    const paths = container.querySelectorAll('path');
    expect(paths.length).toBeGreaterThan(0);
  });

  it('should set aria-hidden on SVG', () => {
    const { container } = render(Icon, { props: { name: 'check' } });
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('aria-hidden')).toBe('true');
  });

  it('should force lucide source when specified', () => {
    const { container } = render(Icon, { props: { name: 'check', source: 'lucide' } });
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('viewBox')).toBe('0 0 24 24');
  });
});
