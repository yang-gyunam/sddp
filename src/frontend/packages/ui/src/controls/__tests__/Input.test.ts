import { render, screen } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Input from '../Input.svelte';

describe('Input', () => {
  it('should render with default type text', () => {
    render(Input);
    const input = document.querySelector('input');
    expect(input).not.toBeNull();
    expect(input?.getAttribute('type')).toBe('text');
  });

  it('should render label when provided', () => {
    render(Input, { props: { label: 'Username' } });
    const label = screen.getByText('Username');
    expect(label).not.toBeNull();
  });

  it('should show required indicator', () => {
    render(Input, { props: { label: 'Email', required: true } });
    const asterisk = screen.getByText('*');
    expect(asterisk).not.toBeNull();
  });

  it('should render as disabled', () => {
    render(Input, { props: { disabled: true } });
    const input = document.querySelector('input');
    expect(input?.hasAttribute('disabled')).toBe(true);
  });

  it('should show error message', () => {
    render(Input, { props: { error: 'Field is required' } });
    const errorMsg = screen.getByText('Field is required');
    expect(errorMsg).not.toBeNull();
  });

  it('should set aria-invalid when error is present', () => {
    render(Input, { props: { error: 'Invalid' } });
    const input = document.querySelector('input');
    expect(input?.getAttribute('aria-invalid')).toBe('true');
  });

  it('should show hint when no error', () => {
    render(Input, { props: { hint: 'Enter your email' } });
    const hint = screen.getByText('Enter your email');
    expect(hint).not.toBeNull();
  });

  it('should set placeholder', () => {
    render(Input, { props: { placeholder: 'Type here' } });
    const input = document.querySelector('input');
    expect(input?.getAttribute('placeholder')).toBe('Type here');
  });

  it('should apply size classes for sm', () => {
    render(Input, { props: { size: 'sm' } });
    const input = document.querySelector('input');
    expect(input?.className).toContain('text-xs');
  });

  it('should render unstyled variant', () => {
    render(Input, { props: { unstyled: true, class: 'custom-class' } });
    const input = document.querySelector('input');
    expect(input?.className).toBe('custom-class');
  });
});
