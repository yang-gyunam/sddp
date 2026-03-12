import type { Meta, StoryObj } from '@storybook/svelte';
import Select from './Select.svelte';

const meta: Meta<typeof Select> = {
  title: 'Controls/Select',
  component: Select,
  tags: ['autodocs'],
  argTypes: {
    value: {
      control: 'text',
      description: 'Selected value',
    },
    disabled: {
      control: 'boolean',
      description: 'Disable the select',
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Select size',
    },
    label: {
      control: 'text',
      description: 'Label text',
    },
    placeholder: {
      control: 'text',
      description: 'Placeholder text',
    },
    error: {
      control: 'text',
      description: 'Error message',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Select>;

const defaultOptions = [
  { value: 'option1', label: 'Option 1' },
  { value: 'option2', label: 'Option 2' },
  { value: 'option3', label: 'Option 3' },
  { value: 'option4', label: 'Option 4', disabled: true },
];

export const Default: Story = {
  args: {
    options: defaultOptions,
    placeholder: 'Select an option',
    size: 'md',
  },
};

export const WithLabel: Story = {
  args: {
    options: defaultOptions,
    label: 'Choose an option',
    placeholder: 'Select...',
    size: 'md',
  },
};

export const WithValue: Story = {
  args: {
    options: defaultOptions,
    label: 'Selected option',
    value: 'option2',
    size: 'md',
  },
};

export const WithError: Story = {
  args: {
    options: defaultOptions,
    label: 'Required field',
    placeholder: 'Select...',
    error: 'Please select an option',
    size: 'md',
  },
};

export const Small: Story = {
  args: {
    options: defaultOptions,
    label: 'Small select',
    placeholder: 'Select...',
    size: 'sm',
  },
};

export const Large: Story = {
  args: {
    options: defaultOptions,
    label: 'Large select',
    placeholder: 'Select...',
    size: 'lg',
  },
};

export const Disabled: Story = {
  args: {
    options: defaultOptions,
    label: 'Disabled select',
    value: 'option1',
    disabled: true,
    size: 'md',
  },
};

export const Required: Story = {
  args: {
    options: defaultOptions,
    label: 'Required field',
    placeholder: 'Select...',
    required: true,
    size: 'md',
  },
};
