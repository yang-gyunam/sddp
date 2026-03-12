// Core Services
export {
  ServiceContainer,
  container,
  type ServiceFactory,
  type ServiceDescriptor,
} from './ServiceContainer';

export { EventBus, eventBus, type EventHandler, type UnsubscribeFn } from './EventBus';

export { ConfigurationManager, config, type AppConfig } from './ConfigurationManager';

export {
  GlobalStateManager,
  globalState,
  createStore,
  type Store,
  type StateListener,
  type Unsubscriber,
} from './GlobalStateManager';

export {
  navigationService,
  navigateTo,
  navigateToSpec,
  navigateToConversation,
  navigateToChannel,
  navigateToRequirement,
  navigateToGlossary,
  navigateToArtifact,
  navigateToDM,
  navigateToPath,
  navigateBack,
  navigateForward,
  type NavigationTarget,
  type NavigationOptions,
  type NavigationState,
} from './NavigationService';
