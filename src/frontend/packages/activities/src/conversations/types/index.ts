// Conversation Types (API DTOs)
// Note: We use explicit re-exports instead of `export *` to avoid ambiguity
// canonical source file: conversation.types.ts
export type {
  ChannelStatus,
  MemberType,
  MessageType,
  ConversationMember,
  Message,
  ChannelSummary,
  ChannelDetail,
  MessagesPage,
  CreateChannelRequest,
  CreateMessageRequest,
  CloseChannelRequest,
  ConversationUnread,
  UnreadCounts,
  UserConversationSettings,
  DirectMessageInfo,
  UserJoinedEvent,
  UserLeftEvent,
  UserTypingEvent,
  ConversationClosedEvent,
  DMConcludedEvent,
  MemberJoinedEvent,
  MemberRemovedEvent,
  ProjectChannelCreatedEvent,
  ConversationInvitationEvent,
  InvitationResponseEvent,
  UserOnlineEvent,
  UserOfflineEvent,
  PresenceStateEvent,
  ConversationStoreState,
  MessageGroup,
  MessageTypeStyle,
  BubbleWidthTier,
  ConversationSearchResult,
  LinkedRequirement,
} from './conversation.types';

export {
  MESSAGE_TYPE_STYLES,
  MESSAGE_TYPE_WIDTH,
  HUMAN_MESSAGE_TYPES,
  AI_MESSAGE_TYPES,
} from './conversation.types';

// Conversation Entry Types (UI prototype - Discord-style)
export type {
  ConversationMemberRole,
  TopicStatus,
  ChannelEntryStatus,
  ConversationEntry,
  Topic,
  DirectMessage,
} from './conversation-entry.types';
// Canonical taxonomy
export type {
  ConversationType,
  ConversationVisibility,
  ConversationScope,
} from './conversation-taxonomy.types';

// Activity Conversation Types (includes ConversationSummary, ProjectConversationGroup, etc.)
export * from './activity-conversation.types';
