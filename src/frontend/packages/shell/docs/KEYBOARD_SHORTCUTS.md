# Keyboard Shortcuts

This document lists all keyboard shortcuts available in the SDDP application.

## Layout Shortcuts

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+B` | Toggle Sidebar | Show/hide the left sidebar | `layout.store.ts` |
| `Ctrl+J` | Toggle Panel | Show/hide the bottom panel | `panel.store.ts` |
| `Ctrl+Alt+B` | Toggle Right Panel | Show/hide the right side panel | `shortcuts.ts` |
| `F6` | Focus Next Zone | Cycle focus through zones (Activity Bar → Sidebar → Editor → Panel) | `layout.store.ts` |
| `Shift+F6` | Focus Previous Zone | Cycle focus backwards through zones | `layout.store.ts` |

## Tab Management

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+W` | Close Tab | Close the currently active tab | `tabs.store.ts` |
| `Ctrl+Tab` | Next Tab | Switch to the next tab | `tabs.store.ts` |
| `Ctrl+Shift+Tab` | Previous Tab | Switch to the previous tab | `tabs.store.ts` |
| `Delete` | Close Tab | Close the currently focused tab (when tab bar is focused) | `tabs.store.ts` |
| `ArrowLeft` | Previous Tab | Navigate to previous tab (when tab bar is focused) | `tabs.store.ts` |
| `ArrowRight` | Next Tab | Navigate to next tab (when tab bar is focused) | `tabs.store.ts` |
| `Home` | First Tab | Jump to the first tab (when tab bar is focused) | `tabs.store.ts` |
| `End` | Last Tab | Jump to the last tab (when tab bar is focused) | `tabs.store.ts` |

## Command Palette

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+Shift+P` | Open Command Palette | Show the command palette | `App.svelte` |
| `Ctrl+K` | Open Command Palette | Toggle the command palette | `shortcuts.ts` |

## File

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+S` | Save | Submit the active form or emit a save event | `shortcuts.ts` |

## Quick Switcher

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+Tab` | Open Quick Switcher | Show the quick tab switcher overlay | `App.svelte` |

## Terminal

| Shortcut | Action | Description | Implementation |
|----------|--------|-------------|----------------|
| `Ctrl+Shift+\`` | Focus Terminal | Open panel and focus terminal tab | `Panel.svelte` |

## Platform-Specific Notes

### macOS
- `Ctrl` is replaced with `⌘ (Command)` key
- `Alt` is replaced with `⌥ (Option)` key
- Shortcuts automatically adapt to platform conventions

### Windows/Linux
- Uses standard `Ctrl`, `Alt`, `Shift` modifiers
- All shortcuts work as documented

## Focus Zones

The application is divided into four focus zones for keyboard navigation:

1. **Activity Bar** - Left-most vertical bar with activity icons
2. **Sidebar** - Collapsible left panel with tree views and lists
3. **Editor** - Main content area with tabs
4. **Panel** - Bottom panel with terminal, problems, output

Use `F6` to cycle forward through zones, `Shift+F6` to cycle backward.

## Customization

Keyboard shortcuts can be customized programmatically using the shortcuts API:

```typescript
import { registerShortcut, unregisterShortcut } from '@sddp/web/lib/shortcuts';

// Register a new shortcut
registerShortcut({
  key: 'n',
  ctrl: true,
  action: () => console.log('New file'),
  description: 'Create new file',
  category: 'File',
});

// Unregister a shortcut
unregisterShortcut('n', { ctrl: true });
```

## Implementation Details

### Shortcut Registration
- Global shortcuts are registered in `web/src/lib/shortcuts.ts`
- Component-specific shortcuts are handled in their respective components
- Tab shortcuts are managed in `shell/src/stores/tabs.store.ts`
- Layout shortcuts are managed in `shell/src/stores/layout.store.ts`

### Event Handling
- Shortcuts are disabled when focus is in input fields, textareas, or contenteditable elements
- `Ctrl+S` is allowed in inputs to submit the active form
- Event propagation is stopped for matched shortcuts to prevent conflicts
- Platform detection automatically adjusts modifier key display

### Conflict Resolution
- Duplicate shortcuts are detected and logged as warnings
- Later registrations override earlier ones
- Component-specific shortcuts take precedence over global shortcuts

## Testing

Keyboard shortcuts are tested in:
- `web/tests/e2e/sidebar-integration.spec.ts` - Sidebar toggle (Ctrl+B)
- `web/tests/e2e/app-shell.spec.ts` - Tab close (Ctrl+W)

## Future Enhancements

Planned improvements:
- [ ] User-configurable shortcuts via settings UI
- [ ] Shortcut conflict detection and resolution UI
- [ ] Shortcut cheat sheet overlay (Ctrl+K Ctrl+S)
- [ ] Context-aware shortcuts (different shortcuts per activity)
- [ ] Shortcut recording/macro system
