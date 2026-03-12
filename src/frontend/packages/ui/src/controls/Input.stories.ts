import type { Meta, StoryObj } from '@storybook/svelte';
import Input from './Input.svelte';

const meta: Meta<typeof Input> = {
  title: 'Controls/Input',
  component: Input,
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['text', 'password', 'email', 'number', 'tel', 'url', 'search', 'date'],
      description: 'Input type',
    },
    value: {
      control: 'text',
      description: 'Input value',
    },
    placeholder: {
      control: 'text',
      description: 'Placeholder text',
    },
    label: {
      control: 'text',
      description: 'Label text',
    },
    error: {
      control: 'text',
      description: 'Error message',
    },
    disabled: {
      control: 'boolean',
      description: 'Disable the input',
    },
    required: {
      control: 'boolean',
      description: 'Mark as required',
    },
  },
};

export default meta;
type Story = StoryObj<typeof Input>;

export const Default: Story = {
  args: {
    type: 'text',
    placeholder: 'Enter text...',
  },
};

export const WithLabel: Story = {
  args: {
    type: 'text',
    label: 'Username',
    placeholder: 'Enter your username',
  },
};

export const Required: Story = {
  args: {
    type: 'email',
    label: 'Email',
    placeholder: 'you@example.com',
    required: true,
  },
};

export const WithError: Story = {
  args: {
    type: 'email',
    label: 'Email',
    value: 'invalid-email',
    error: 'Please enter a valid email address',
  },
};

export const Disabled: Story = {
  args: {
    type: 'text',
    label: 'Disabled Input',
    value: 'Cannot edit this',
    disabled: true,
  },
};

export const Password: Story = {
  args: {
    type: 'password',
    label: 'Password',
    placeholder: 'Enter your password',
  },
};

export const Search: Story = {
  args: {
    type: 'search',
    placeholder: 'Search...',
  },
};

export const Number: Story = {
  args: {
    type: 'number',
    label: 'Quantity',
    placeholder: '0',
  },
};

export const Date: Story = {
  args: {
    type: 'date',
    label: 'Date',
  },
};
