import type { Meta, StoryObj } from '@storybook/svelte';
import Icon from './Icon.svelte';

const meta: Meta<typeof Icon> = {
  title: 'Controls/Icon',
  component: Icon,
  tags: ['autodocs'],
  argTypes: {
    name: {
      control: 'text',
      description: 'Icon name (lucide icon or codicon)',
    },
    source: {
      control: 'select',
      options: [undefined, 'lucide', 'codicon'],
      description: 'Icon source library. Auto-detected if not specified.',
    },
    size: {
      control: 'select',
      options: ['xs', 'sm', 'md', 'lg', 'xl'],
      description: 'Icon size',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Icon>;

export const Default: Story = {
  args: {
    name: 'home',
    size: 'md',
  },
};

export const ExtraSmall: Story = {
  args: {
    name: 'settings',
    size: 'xs',
  },
};

export const Small: Story = {
  args: {
    name: 'user',
    size: 'sm',
  },
};

export const Medium: Story = {
  args: {
    name: 'search',
    size: 'md',
  },
};

export const Large: Story = {
  args: {
    name: 'bell',
    size: 'lg',
  },
};

export const ExtraLarge: Story = {
  args: {
    name: 'folder',
    size: 'xl',
  },
};

// Common icons showcase
export const CommonIcons: Story = {
  args: {
    name: 'home',
    size: 'md',
  },
  render: () => ({
    Component: Icon,
    props: { name: 'home', size: 'md' },
  }),
};

// Codicons (VS Code icons) - auto-detected
export const Codicon: Story = {
  args: {
    name: 'dashboard',
    size: 'md',
  },
};

export const CodiconSearch: Story = {
  args: {
    name: 'search',
    size: 'md',
  },
};

export const CodiconSettings: Story = {
  args: {
    name: 'settings-gear',
    size: 'md',
  },
};

export const CodiconCommentDiscussion: Story = {
  args: {
    name: 'comment-discussion',
    size: 'md',
  },
};

export const CodiconTasklist: Story = {
  args: {
    name: 'tasklist',
    size: 'md',
  },
};

// Codicon with explicit source
export const CodiconExplicitSource: Story = {
  args: {
    name: 'dashboard',
    source: 'codicon',
    size: 'md',
  },
};

// Lucide with explicit source
export const LucideExplicitSource: Story = {
  args: {
    name: 'home',
    source: 'lucide',
    size: 'md',
  },
};
