import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Icon from '../Icon.svelte';
import FileIcon from '../FileIcon.svelte';

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
    expect(paths).toHaveLength(1);
    expect(paths[0]?.getAttribute('d')).toBe('M18 6L6 18M6 6l12 12');
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

describe('FileIcon', () => {
  it.each([
    ['src/API.CS', 'C#'],
    ['src/view.tsx', 'TypeScript'],
    ['scripts/build.mjs', 'JavaScript'],
    ['src/App.svelte', 'Svelte'],
    ['docs/README.MD', 'Markdown'],
    ['config/app.json', 'JSON'],
    ['web/package.json', 'Package manifest'],
    ['.env.local', 'Configuration'],
    ['deploy/Dockerfile.prod', 'Container build'],
    ['styles/site.css', 'Stylesheet'],
    ['db/migration.sql', 'Database'],
    ['assets/logo.PNG', 'Image'],
    ['dist/bundle.tar.gz', 'Archive'],
    ['unknown.xyz', 'File'],
    ['parent.cs/NOTICE', 'File'],
  ])('identifies %s by basename and file type', (path, title) => {
    const { container } = render(FileIcon, { path });
    const svg = container.querySelector('svg')!;
    expect(svg.querySelector('title')?.textContent).toBe(title);
    expect(svg.classList.contains('w-4')).toBe(true);
    // All resolved names must exist in the shared catalog, not render Icon's unknown-name X.
    expect(svg.querySelector('path')?.getAttribute('d')).not.toBe('M18 6L6 18M6 6l12 12');
  });

  it('prioritizes Git entry kind and updates when the selected file changes', async () => {
    const { container, rerender } = render(FileIcon, { path: 'module.cs', kind: 'directory' });
    const title = () => container.querySelector('title')?.textContent;
    expect(title()).toBe('Folder');
    await rerender({ path: 'module.cs', kind: 'symlink' });
    expect(title()).toBe('Symbolic link');
    await rerender({ path: 'module.cs', kind: 'submodule' });
    expect(title()).toBe('Git submodule');
    await rerender({ path: 'module.cs', kind: 'file' });
    expect(title()).toBe('C#');
    const csharpPath = container.querySelector('path')?.getAttribute('d');
    await rerender({ path: 'README.md', kind: 'file' });
    expect(title()).toBe('Markdown');
    expect(container.querySelector('path')?.getAttribute('d')).not.toBe(csharpPath);
  });
});
