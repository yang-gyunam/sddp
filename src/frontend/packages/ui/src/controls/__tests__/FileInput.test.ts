import { fireEvent, render, screen } from '@testing-library/svelte';
import { describe, it, expect, vi } from 'vitest';
import FileInput from '../FileInput.svelte';

describe('FileInput', () => {
  it('labels the native picker and forwards file restrictions without reading file bytes', async () => {
    const onselect = vi.fn();
    render(FileInput, {
      label: 'YAML file',
      id: 'yaml',
      name: 'diagram',
      accept: '.yaml,.yml',
      multiple: true,
      required: true,
      hint: 'Select a diagram',
      onselect,
    });
    const input = screen.getByLabelText('YAML file', {
      exact: false,
    }) as HTMLInputElement;
    expect(input.type).toBe('file');
    expect(input.name).toBe('diagram');
    expect(input.accept).toBe('.yaml,.yml');
    expect(input.multiple).toBe(true);
    expect(input.required).toBe(true);
    expect(input.getAttribute('aria-describedby')).toBe('yaml-hint');
    const files = [
      new File([new Uint8Array([255, 254, 65, 0])], 'first.yaml'),
      new File(['B'], 'second.yml'),
    ];
    await fireEvent.change(input, { target: { files } });
    expect(onselect).toHaveBeenCalledExactlyOnceWith(files);
    expect(onselect.mock.calls[0]![0][0]).toBe(files[0]);
    expect(onselect.mock.calls[0]![0][1]).toBe(files[1]);
  });

  it('resets the browser value and emits the same selected file again', async () => {
    const onselect = vi.fn();
    render(FileInput, { label: 'YAML file', onselect });
    const input = screen.getByLabelText('YAML file') as HTMLInputElement;
    const reset = vi.spyOn(input, 'value', 'set');
    const file = new File(['diagram'], 'same.yaml');
    await fireEvent.change(input, { target: { files: [file] } });
    await fireEvent.change(input, { target: { files: [file] } });
    expect(onselect).toHaveBeenNthCalledWith(1, [file]);
    expect(onselect).toHaveBeenNthCalledWith(2, [file]);
    expect(reset.mock.calls).toEqual([[''], ['']]);
    reset.mockRestore();
  });

  it('does not emit a cancelled selection or a selection while disabled', async () => {
    const onselect = vi.fn();
    const view = render(FileInput, { label: 'YAML file', onselect });
    const input = screen.getByLabelText('YAML file') as HTMLInputElement;
    await fireEvent.change(input, { target: { files: [] } });
    expect(onselect).not.toHaveBeenCalled();
    await view.rerender({ disabled: true });
    expect(input.disabled).toBe(true);
    await fireEvent.change(input, {
      target: { files: [new File(['A'], 'a.yaml')] },
    });
    expect(onselect).not.toHaveBeenCalled();
  });

  it('retains its generated label target and describes the current error or hint only', async () => {
    const view = render(FileInput, { label: 'YAML file', hint: 'Choose YAML' });
    const input = screen.getByLabelText('YAML file') as HTMLInputElement;
    const id = input.id;
    expect(id).not.toBe('');
    expect(input.getAttribute('aria-describedby')).toBe(`${id}-hint`);
    await view.rerender({ error: 'File rejected' });
    expect(screen.getByLabelText('YAML file')).toBe(input);
    expect(input.id).toBe(id);
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toBe(`${id}-error`);
    expect(screen.getByRole('alert').textContent?.trim()).toBe('File rejected');
    expect(screen.queryByText('Choose YAML')).toBeNull();
    await view.rerender({ error: undefined, hint: undefined });
    expect(input.getAttribute('aria-describedby')).toBeNull();
    expect(input.getAttribute('aria-invalid')).toBeNull();
    expect(screen.queryByRole('alert')).toBeNull();
  });
});
