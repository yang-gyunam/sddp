import type { Meta, StoryObj } from '@storybook/svelte';
import Button from './Button.svelte';

const meta: Meta<typeof Button> = {
  title: 'Controls/Button',
  component: Button,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['primary', 'secondary', 'ghost', 'danger', 'outline'],
      description: 'Button style variant',
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Button size',
    },
    disabled: {
      control: 'boolean',
      description: 'Disable the button',
    },
    loading: {
      control: 'boolean',
      description: 'Show loading spinner',
    },
    fullWidth: {
      control: 'boolean',
      description: 'Make button full width',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Button>;

export const Primary: Story = {
  args: {
    variant: 'primary',
    size: 'md',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Primary Button' },
  }),
};

export const Secondary: Story = {
  args: {
    variant: 'secondary',
    size: 'md',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Secondary Button' },
  }),
};

export const Ghost: Story = {
  args: {
    variant: 'ghost',
    size: 'md',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Ghost Button' },
  }),
};

export const Danger: Story = {
  args: {
    variant: 'danger',
    size: 'md',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Danger Button' },
  }),
};

export const Outline: Story = {
  args: {
    variant: 'outline',
    size: 'md',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Outline Button' },
  }),
};

export const Small: Story = {
  args: {
    variant: 'primary',
    size: 'sm',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Small Button' },
  }),
};

export const Large: Story = {
  args: {
    variant: 'primary',
    size: 'lg',
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Large Button' },
  }),
};

export const Loading: Story = {
  args: {
    variant: 'primary',
    size: 'md',
    loading: true,
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Loading...' },
  }),
};

export const Disabled: Story = {
  args: {
    variant: 'primary',
    size: 'md',
    disabled: true,
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Disabled Button' },
  }),
};

export const FullWidth: Story = {
  args: {
    variant: 'primary',
    size: 'md',
    fullWidth: true,
  },
  render: (args) => ({
    Component: Button,
    props: args,
    slots: { default: 'Full Width Button' },
  }),
};
