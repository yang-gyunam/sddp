/**
 * TipTap Custom Extensions for Spec Editor
 * Export all custom blocks and marks
 */

// Custom Blocks (Node Extensions)
export { DecisionBlock, type DecisionBlockAttributes } from './DecisionBlock';
export {
  AcceptanceCriteriaBlock,
  type AcceptanceCriteriaBlockAttributes,
  type CriteriaItem,
} from './AcceptanceCriteriaBlock';
export { RiskBlock, type RiskBlockAttributes, type RiskLevel } from './RiskBlock';

// Custom Marks
export { GlossaryLink, type GlossaryLinkAttributes } from './GlossaryLink';
export { SpecReference, type SpecReferenceAttributes } from './SpecReference';
export {
  ChangeHighlight,
  type ChangeHighlightAttributes,
  type ChangeType,
} from './ChangeHighlight';

// Suggestion Plugins
export {
  GlossaryLinkSuggestion,
  type GlossaryLinkSuggestionOptions,
} from './GlossaryLinkSuggestion';
