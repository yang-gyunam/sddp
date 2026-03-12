/**
 * Lightweight user reference returned from backend as nested object.
 * Replaces flat fields like createdById/createdByName/createdByAvatarUrl.
 */
export interface UserRef {
  id: string;
  name: string | null;
  avatarUrl: string | null;
}
