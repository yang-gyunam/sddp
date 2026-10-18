# @sddp/ui

SDDP Design System - a Svelte 5 UI component library

## Installation

```bash
cd src/frontend
npm ci
```

This package is linked by the repository npm workspaces. A separately published npm package is not required. Run package-local commands below from `src/frontend/packages/ui`.

## Components

### Form Controls
| Component | Description |
|---------|------|
| `Button` | Supports primary, secondary, ghost, danger, outline and unstyled variants |
| `Input` | Supports Text, Password, Email, Number, and other input types |
| `Textarea` | Multi-line text input |
| `Checkbox` | Checkbox with indeterminate-state support |
| `CheckboxList` | Checkbox list |
| `Radio` | Radio button |
| `RadioGroup` | Radio button group |
| `Select` | Dropdown select |
| `Combobox` | Searchable dropdown |
| `SearchField` | Search input field |
| `Switch` | Toggle switch |

### Date & Time Controls
| Component | Description |
|---------|------|
| `Calendar` | Calendar |
| `DatePicker` | Date picker |
| `DateRangePicker` | Date-range picker |
| `TimePicker` | Time picker |

### Display Controls
| Component | Description |
|---------|------|
| `Badge` | Status badge with default, primary, success, warning, error, and info variants |
| `Icon` | Lucide icons plus VS Code Codicons |
| `IconButton` | Icon button |
| `Tooltip` | Tooltip with top, bottom, left, and right placement |
| `Typography` | Text styling for h1-h6, body1, body2, caption, and overline |
| `Spinner` | Loading spinner |
| `LinearProgress` | Linear progress bar |
| `RadialProgress` | Radial progress indicator |

### Layout & Navigation Controls
| Component | Description |
|---------|------|
| `Divider` | Divider with horizontal and vertical variants |
| `ResizeHandle` | Resize handle |
| `TabBar` | Tab navigation bar |

## Usage

```svelte
<script>
  import { Button, Input, Badge, Icon } from '@sddp/ui';
</script>

<Button variant="primary" size="md" onclick={() => console.log('clicked')}>
  Click me
</Button>

<Input label="Email" type="email" placeholder="you@example.com" required />

<Badge variant="success">Active</Badge>

<Icon name="home" size="md" />
```

## Storybook

Component documentation and interactive demos:

```bash
# Run the dev server
npm run storybook

# Build the static Storybook site
npm run build-storybook
```

Open `http://localhost:6006`.

### Story List
- **Button** - variants, sizes, loading, and disabled states
- **Badge** - six color variants with closable examples
- **Input** - examples by type, error states, and labels
- **Typography** - heading, body, and caption styles
- **Switch** - size, label, and description variants
- **Checkbox** - indeterminate state
- **Select** - options, error states, and disabled states
- **Icon** - Lucide + Codicons
- **Tooltip** - placement examples

## Design Tokens

CSS-variable-based theme system:

```css
@import '@sddp/ui/styles';
```

Key tokens:
- `--color-primary-*` - primary colors
- `--color-text-*` - text colors (primary, secondary, tertiary)
- `--color-bg-*` - background colors
- `--color-border` - border color
- `--color-success-*`, `--color-warning-*`, `--color-error-*`, `--color-info-*`

## Development

```bash
# Run type checking
npm run type-check
```

## Tech Stack

- Svelte 5 (Runes syntax)
- TypeScript
- Tailwind CSS
- Storybook 10
