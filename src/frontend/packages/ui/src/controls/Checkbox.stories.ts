import type { Meta, StoryObj } from '@storybook/svelte';
import Checkbox from './Checkbox.svelte';

const meta: Meta<typeof Checkbox> = {
  title: 'Controls/Checkbox',
  component: Checkbox,
  tags: ['autodocs'],
  argTypes: {
    checked: {
      control: 'boolean',
      description: 'Checkbox state',
    },
    disabled: {
      control: 'boolean',
      description: 'Disable the checkbox',
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Checkbox size',
    },
    label: {
      control: 'text',
      description: 'Label text',
    },
    indeterminate: {
      control: 'boolean',
      description: 'Indeterminate state',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Checkbox>;

export const Default: Story = {
  args: {
    checked: false,
    size: 'md',
  },
};

export const Checked: Story = {
  args: {
    checked: true,
    size: 'md',
  },
};

export const WithLabel: Story = {
  args: {
    checked: false,
    size: 'md',
    label: 'Accept terms and conditions',
  },
};

export const Small: Story = {
  args: {
    checked: true,
    size: 'sm',
    label: 'Small checkbox',
  },
};

export const Large: Story = {
  args: {
    checked: true,
    size: 'lg',
    label: 'Large checkbox',
  },
};

export const Disabled: Story = {
  args: {
    checked: false,
    size: 'md',
    label: 'Disabled checkbox',
    disabled: true,
  },
};

export const DisabledChecked: Story = {
  args: {
    checked: true,
    size: 'md',
    label: 'Disabled checked',
    disabled: true,
  },
};

export const Indeterminate: Story = {
  args: {
    checked: false,
    indeterminate: true,
    size: 'md',
    label: 'Select all',
  },
};
