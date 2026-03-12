// @sddp/shell - VS Code Style App Shell
// NOTE: Stores and Services use explicit exports to prevent name collisions.
// Components, Utils, Core, Auth use export * (large export surface; conversion tracked separately).

// Core (absorbed from @sddp/core)
export * from './core';

// Auth (absorbed from @sddp/auth)
export * from './auth';

// Components
export * from './components';

// Stores (explicit exports — see stores/index.ts)
export * from './stores';

// Types
export * from './types';

// Services (already explicit)
export { TabFactory, NavigationService, RouterService, PathMatcher } from './services';

// Utils
export * from './utils';
