import { render, screen, fireEvent } from '@testing-library/svelte';
import { describe, it, expect, vi } from 'vitest';
import Button from '../Button.svelte';

describe('Button', () => {
  it('should render with default props', () => {
    render(Button);
    const button = screen.getByRole('button');
    expect(button).not.toBeNull();
    expect(button.getAttribute('type')).toBe('button');
    expect(button.getAttribute('disabled')).toBeNull();
  });

  it('should render as disabled when disabled prop is true', () => {
    render(Button, { props: { disabled: true } });
    const button = screen.getByRole('button');
    expect(button.hasAttribute('disabled')).toBe(true);
  });

  it('should render as disabled when loading prop is true', () => {
    render(Button, { props: { loading: true } });
    const button = screen.getByRole('button');
    expect(button.hasAttribute('disabled')).toBe(true);
  });

  it('should show spinner SVG when loading', () => {
    render(Button, { props: { loading: true } });
    const svg = document.querySelector('svg.animate-spin');
    expect(svg).not.toBeNull();
  });

  it('should call onclick handler when clicked', async () => {
    const handleClick = vi.fn();
    render(Button, { props: { onclick: handleClick } });
    const button = screen.getByRole('button');
    await fireEvent.click(button);
    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('should not call onclick when disabled', async () => {
    const handleClick = vi.fn();
    render(Button, { props: { onclick: handleClick, disabled: true } });
    const button = screen.getByRole('button');
    await fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('should not call onclick when loading', async () => {
    const handleClick = vi.fn();
    render(Button, { props: { onclick: handleClick, loading: true } });
    const button = screen.getByRole('button');
    await fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('should apply variant classes', () => {
    render(Button, { props: { variant: 'danger' } });
    const button = screen.getByRole('button');
    expect(button.className).toContain('error');
  });

  it('should apply size classes', () => {
    render(Button, { props: { size: 'lg' } });
    const button = screen.getByRole('button');
    expect(button.className).toContain('px-6');
  });

  it('should set title attribute', () => {
    render(Button, { props: { title: 'My Button' } });
    const button = screen.getByRole('button');
    expect(button.getAttribute('title')).toBe('My Button');
  });

  it('should apply fullWidth class', () => {
    render(Button, { props: { fullWidth: true } });
    const button = screen.getByRole('button');
    expect(button.className).toContain('w-full');
  });

  it('should set submit type', () => {
    render(Button, { props: { type: 'submit' } });
    const button = screen.getByRole('button');
    expect(button.getAttribute('type')).toBe('submit');
  });
});
