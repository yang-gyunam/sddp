# Keyboard Shortcuts

The public web app registers these global shortcuts in `web/src/lib/shortcuts.ts`.
Use Command in place of Control on macOS. Browser-reserved shortcuts can take precedence.

| Shortcut | Action |
|---|---|
| Ctrl+B | Toggle the left sidebar |
| Ctrl+J | Toggle the bottom panel |
| Ctrl+Alt+B | Toggle the right panel |
| Ctrl+W | Request closure of the active tab, including unsaved-change handling |
| Ctrl+K | Toggle the command palette |
| Ctrl+S | Submit the focused form or emit the application save event |

Global shortcuts are ignored in input, textarea and editable content, except Save.
Component-local tab and focus navigation depends on the focused control.

The shipped L0 browser tests are in `web/tests/e2e/L0/`; they are separate from the limited
unit suite run by `make test`. This page does not claim that every shortcut has public unit coverage.
