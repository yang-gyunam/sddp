import { render, screen, fireEvent } from '@testing-library/svelte';
import { describe, it, expect, vi } from 'vitest';
import IconButton from '../IconButton.svelte';

describe('IconButton', () => {
  it('should render with required icon prop', () => {
    render(IconButton, { props: { icon: 'check' } });
    const button = screen.getByRole('button');
    expect(button).not.toBeNull();
  });

  it('should set aria-label from title', () => {
    render(IconButton, { props: { icon: 'check', title: 'Save' } });
    const button = screen.getByRole('button');
    expect(button.getAttribute('aria-label')).toBe('Save');
  });

  it('should fallback aria-label to icon name when no title', () => {
    render(IconButton, { props: { icon: 'edit' } });
    const button = screen.getByRole('button');
    expect(button.getAttribute('aria-label')).toBe('edit');
  });

  it('should render as disabled when disabled is true', () => {
    render(IconButton, { props: { icon: 'x', disabled: true } });
    const button = screen.getByRole('button');
    expect(button.hasAttribute('disabled')).toBe(true);
  });

  it('should call onclick handler when clicked', async () => {
    const handleClick = vi.fn();
    render(IconButton, { props: { icon: 'check', onclick: handleClick } });
    const button = screen.getByRole('button');
    await fireEvent.click(button);
    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('should not call onclick when disabled', async () => {
    const handleClick = vi.fn();
    render(IconButton, { props: { icon: 'check', onclick: handleClick, disabled: true } });
    const button = screen.getByRole('button');
    await fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('should show spinner when loading', () => {
    render(IconButton, { props: { icon: 'check', loading: true } });
    const svg = document.querySelector('svg.animate-spin');
    expect(svg).not.toBeNull();
  });

  it('should apply variant classes for danger', () => {
    render(IconButton, { props: { icon: 'trash', variant: 'danger' } });
    const button = screen.getByRole('button');
    expect(button.className).toContain('error');
  });

  it('should apply size classes for lg', () => {
    render(IconButton, { props: { icon: 'check', size: 'lg' } });
    const button = screen.getByRole('button');
    expect(button.className).toContain('w-8');
  });
});
