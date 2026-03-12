export type { UserRef } from './userRef.types';
export { ROLE_DISPLAY, ALL_ROLES, PROJECT_ROLES, getRoleLabel, isSystemRole } from './role.constants';

/**
 * Field author information - who last modified each field
 */
export interface FieldAuthor {
  fieldName: string;
  userId: string;
  userName: string;
  timestamp: string;
}
