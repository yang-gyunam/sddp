import type { Meta, StoryObj } from '@storybook/svelte';
import Typography from './Typography.svelte';

const meta: Meta<typeof Typography> = {
  title: 'Controls/Typography',
  component: Typography,
  tags: ['autodocs'],
  argTypes: {
    as: {
      control: 'select',
      options: ['h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'span', 'div', 'label'],
      description: 'HTML element to render',
    },
    variant: {
      control: 'select',
      options: ['h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'body1', 'body2', 'caption', 'overline'],
      description: 'Typography style variant',
    },
    color: {
      control: 'select',
      options: ['primary', 'secondary', 'tertiary', 'inherit'],
      description: 'Text color',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Typography>;

export const Heading1: Story = {
  args: {
    as: 'h1',
    variant: 'h1',
    color: 'primary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Heading 1' },
  }),
};

export const Heading2: Story = {
  args: {
    as: 'h2',
    variant: 'h2',
    color: 'primary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Heading 2' },
  }),
};

export const Heading3: Story = {
  args: {
    as: 'h3',
    variant: 'h3',
    color: 'primary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Heading 3' },
  }),
};

export const Heading4: Story = {
  args: {
    as: 'h4',
    variant: 'h4',
    color: 'primary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Heading 4' },
  }),
};

export const Body1: Story = {
  args: {
    as: 'p',
    variant: 'body1',
    color: 'primary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Body 1 - This is the default body text style for paragraphs and general content.' },
  }),
};

export const Body2: Story = {
  args: {
    as: 'p',
    variant: 'body2',
    color: 'secondary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Body 2 - Smaller body text for secondary content and descriptions.' },
  }),
};

export const Caption: Story = {
  args: {
    as: 'span',
    variant: 'caption',
    color: 'tertiary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Caption text for labels and small annotations' },
  }),
};

export const Overline: Story = {
  args: {
    as: 'span',
    variant: 'overline',
    color: 'secondary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Overline Text' },
  }),
};

export const SecondaryColor: Story = {
  args: {
    as: 'p',
    variant: 'body1',
    color: 'secondary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Secondary color text for less prominent content.' },
  }),
};

export const TertiaryColor: Story = {
  args: {
    as: 'p',
    variant: 'body2',
    color: 'tertiary',
  },
  render: (args) => ({
    Component: Typography,
    props: args,
    slots: { default: 'Tertiary color text for muted content.' },
  }),
};
