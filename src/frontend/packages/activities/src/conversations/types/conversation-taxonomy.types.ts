/**
 * Canonical conversation taxonomy
 * Shared naming model for BE/FE contracts.
 */

export type ConversationType = 'Channel' | 'Forum' | 'DirectMessage';

export type ConversationVisibility = 'Public' | 'Private';

export type ConversationScope = 'TenantWide' | 'ProjectScoped';
