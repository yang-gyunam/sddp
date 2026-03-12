// @sddp/activities/specs - Specs Activity
//
// :
//   specs/
// ├── types/ #
// ├── services/ # API
// ├── stores/ # status
// └── components/ #
// ├── idioms/ #
// ├── sections/ #
// └── pages/ #

// Types
export * from './types';

// Services
export * from './services';

// Stores
export * from './stores';

// Components (sections + pages only; idioms are internal — avoids name collision with types)
export * from './components/sections';

// Idioms (explicit: only non-colliding Badge components used cross-domain)
export { SpecStatusBadge } from './components/idioms';

// Activity ID
export const SPECS_ACTIVITY_ID = 'specs';
