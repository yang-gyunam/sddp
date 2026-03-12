import type { Meta, StoryObj } from '@storybook/svelte';
import Switch from './Switch.svelte';

const meta: Meta<typeof Switch> = {
  title: 'Controls/Switch',
  component: Switch,
  tags: ['autodocs'],
  argTypes: {
    checked: {
      control: 'boolean',
      description: 'Switch state',
    },
    disabled: {
      control: 'boolean',
      description: 'Disable the switch',
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Switch size',
    },
    label: {
      control: 'text',
      description: 'Label text',
    },
    description: {
      control: 'text',
      description: 'Description text',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Switch>;

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
    label: 'Enable notifications',
  },
};

export const WithDescription: Story = {
  args: {
    checked: true,
    size: 'md',
    label: 'Dark mode',
    description: 'Use dark theme for the application',
  },
};

export const Small: Story = {
  args: {
    checked: true,
    size: 'sm',
    label: 'Small switch',
  },
};

export const Large: Story = {
  args: {
    checked: true,
    size: 'lg',
    label: 'Large switch',
  },
};

export const Disabled: Story = {
  args: {
    checked: false,
    size: 'md',
    label: 'Disabled switch',
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
