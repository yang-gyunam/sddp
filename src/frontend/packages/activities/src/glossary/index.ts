// @sddp/activities/glossary - Glossary Activity
//
// :
//   glossary/
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
export { TermCategoryBadge, TermStatusBadge } from './components/idioms';

// Activity ID
export const GLOSSARY_ACTIVITY_ID = 'glossary';
