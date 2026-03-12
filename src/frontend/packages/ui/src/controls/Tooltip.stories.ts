import type { Meta, StoryObj } from '@storybook/svelte';
import Tooltip from './Tooltip.svelte';

const meta: Meta<typeof Tooltip> = {
  title: 'Controls/Tooltip',
  component: Tooltip,
  tags: ['autodocs'],
  argTypes: {
    content: {
      control: 'text',
      description: 'Tooltip content',
    },
    placement: {
      control: 'select',
      options: ['top', 'bottom', 'left', 'right'],
      description: 'Tooltip placement',
    },
    delay: {
      control: 'number',
      description: 'Delay before showing (ms)',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Tooltip>;

export const Top: Story = {
  args: {
    content: 'Tooltip on top',
    placement: 'top',
  },
  render: (args) => ({
    Component: Tooltip,
    props: args,
    slots: { default: '<button class="px-4 py-2 bg-blue-500 text-white rounded">Hover me</button>' },
  }),
};

export const Bottom: Story = {
  args: {
    content: 'Tooltip on bottom',
    placement: 'bottom',
  },
  render: (args) => ({
    Component: Tooltip,
    props: args,
    slots: { default: '<button class="px-4 py-2 bg-blue-500 text-white rounded">Hover me</button>' },
  }),
};

export const Left: Story = {
  args: {
    content: 'Tooltip on left',
    placement: 'left',
  },
  render: (args) => ({
    Component: Tooltip,
    props: args,
    slots: { default: '<button class="px-4 py-2 bg-blue-500 text-white rounded">Hover me</button>' },
  }),
};

export const Right: Story = {
  args: {
    content: 'Tooltip on right',
    placement: 'right',
  },
  render: (args) => ({
    Component: Tooltip,
    props: args,
    slots: { default: '<button class="px-4 py-2 bg-blue-500 text-white rounded">Hover me</button>' },
  }),
};

export const WithDelay: Story = {
  args: {
    content: 'Appears after 500ms',
    placement: 'top',
    delay: 500,
  },
  render: (args) => ({
    Component: Tooltip,
    props: args,
    slots: { default: '<button class="px-4 py-2 bg-blue-500 text-white rounded">Hover me (delayed)</button>' },
  }),
};
