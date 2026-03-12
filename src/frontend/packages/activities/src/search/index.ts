// @sddp/activities/search - Search Activity
//
// :
//   search/
// ├── types/ #
// ├── services/ # API (SearchService)
// ├── stores/ # status (searchStore)
// └── components/ #
// ├── idioms/ # (FilterSection, DateRangePicker, AdvancedSearchModal)
// ├── sections/ # (SearchInput, SearchResults)
// └── pages/ #

// Types
export * from './types';

// Services
export * from './services';

// Stores
export * from './stores';

// Components (sections + pages only; idioms are internal — avoids name collision)
export * from './components/sections';
export * from './components/pages';

// Idioms (explicit: only cross-domain components)
export {
  FilterSection,
  DateRangePicker,
  SavedSearchItem,
  AdvancedSearchModal,
} from './components/idioms';

// Constants
export const SEARCH_ACTIVITY_ID = 'search';
