/**
 * Role constants shared with the public backend role model.
 */

/** Display labels for public roles. */
export const ROLE_DISPLAY: Record<string, string> = {
  Admin: 'Admin',
  ProductOwner: 'Product Owner',
  DomainExpert: 'Domain Expert',
  Developer: 'Developer',
  Reviewer: 'Reviewer',
  QATester: 'QA Tester',
};

/** Public roles available in the OSS build. */
export const ALL_ROLES: { value: string; label: string }[] = [
  { value: 'Admin', label: 'Admin' },
  { value: 'ProductOwner', label: 'Product Owner' },
  { value: 'DomainExpert', label: 'Domain Expert' },
  { value: 'Developer', label: 'Developer' },
  { value: 'Reviewer', label: 'Reviewer' },
  { value: 'QATester', label: 'QA Tester' },
];

/** Project-assignable roles exclude the Admin system role. */
export const PROJECT_ROLES = ALL_ROLES.filter((r) => r.value !== 'Admin');

export function getRoleLabel(role: string): string {
  return ROLE_DISPLAY[role] ?? role;
}

export function isSystemRole(role: string): boolean {
  return role === 'Admin';
}
