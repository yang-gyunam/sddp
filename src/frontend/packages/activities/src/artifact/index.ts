// @sddp/activities/artifact - Artifact Activity
//
// Structure:
//   artifact/
//   ├── types/           # Type definitions
//   ├── stores/          # State management
//   └── components/      # Reusable components
//       ├── idioms/      # Composite components
//       ├── sections/    # Page sections
//       └── pages/       # Page components
//
// Pages (UI Definition):
// - GlobalArtifactsPage (ACT-ART-001)
//
// Capabilities:
// - Track generated code artifacts
// - Detect modified files
// - Trace from Spec to Artifact
// - Support regeneration

// Types
export * from './types';

// Stores
export * from './stores';

// Services (explicit to avoid naming collisions with wildcard re-exports)
export { resetArtifactService, getArtifactService, ArtifactService } from './services';

// Components (sections + pages only; idioms are internal — avoids name collision with types)
export * from './components/sections';
export * from './components/pages';

// Activity Root
export { default as ArtifactActivity } from './components/ArtifactActivity.svelte';

// Activity ID
export const ARTIFACT_ACTIVITY_ID = 'artifact';
