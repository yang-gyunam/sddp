import type { Meta, StoryObj } from '@storybook/svelte';
import Badge from './Badge.svelte';

const meta: Meta<typeof Badge> = {
  title: 'Controls/Badge',
  component: Badge,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['default', 'primary', 'success', 'warning', 'error', 'info'],
      description: 'Badge style variant',
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
      description: 'Badge size',
    },
    closable: {
      control: 'boolean',
      description: 'Show close button',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Badge>;

export const Default: Story = {
  args: {
    variant: 'default',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Default' },
  }),
};

export const Primary: Story = {
  args: {
    variant: 'primary',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Primary' },
  }),
};

export const Success: Story = {
  args: {
    variant: 'success',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Success' },
  }),
};

export const Warning: Story = {
  args: {
    variant: 'warning',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Warning' },
  }),
};

export const Error: Story = {
  args: {
    variant: 'error',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Error' },
  }),
};

export const Info: Story = {
  args: {
    variant: 'info',
    size: 'md',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Info' },
  }),
};

export const Small: Story = {
  args: {
    variant: 'primary',
    size: 'sm',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Small' },
  }),
};

export const Large: Story = {
  args: {
    variant: 'primary',
    size: 'lg',
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Large' },
  }),
};

export const Closable: Story = {
  args: {
    variant: 'primary',
    size: 'md',
    closable: true,
  },
  render: (args) => ({
    Component: Badge,
    props: args,
    slots: { default: 'Closable Badge' },
  }),
};
