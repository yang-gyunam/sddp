/**
 * Validation Utilities - Declarative schema-based validation
 */

export interface ValidationRule<T = unknown> {
  validate: (value: T) => boolean | string;
  message?: string;
}

export interface ValidationResult {
  valid: boolean;
  errors: Record<string, string[]>;
}

export type ValidationSchema<T> = {
  [K in keyof T]?: ValidationRule<T[K]>[];
};

// --- Rule Builders ---

export function required(msg?: string): ValidationRule {
  return {
    validate: (value) => {
      if (value == null) return false;
      if (typeof value === 'string' && value.trim().length === 0) return false;
      return true;
    },
    message: msg ?? 'This field is required',
  };
}

export function minLength(n: number, msg?: string): ValidationRule<string> {
  return {
    validate: (value) => typeof value === 'string' && value.length >= n,
    message: msg ?? `Must be at least ${n} characters`,
  };
}

export function maxLength(n: number, msg?: string): ValidationRule<string> {
  return {
    validate: (value) => typeof value === 'string' && value.length <= n,
    message: msg ?? `Must be at most ${n} characters`,
  };
}

export function min(n: number, msg?: string): ValidationRule<number> {
  return {
    validate: (value) => typeof value === 'number' && value >= n,
    message: msg ?? `Must be greater than or equal to ${n}`,
  };
}

export function max(n: number, msg?: string): ValidationRule<number> {
  return {
    validate: (value) => typeof value === 'number' && value <= n,
    message: msg ?? `Must be less than or equal to ${n}`,
  };
}

export function pattern(regex: RegExp, msg?: string): ValidationRule<string> {
  return {
    validate: (value) => typeof value === 'string' && regex.test(value),
    message: msg ?? 'Invalid format',
  };
}

export function email(msg?: string): ValidationRule<string> {
  return pattern(/^[^\s@]+@[^\s@]+\.[^\s@]+$/, msg ?? 'Invalid email address');
}

export function url(msg?: string): ValidationRule<string> {
  return {
    validate: (value) => {
      if (typeof value !== 'string') return false;
      try {
        new URL(value);
        return true;
      } catch {
        return false;
      }
    },
    message: msg ?? 'Invalid URL',
  };
}

export function oneOf<T>(values: readonly T[], msg?: string): ValidationRule<T> {
  return {
    validate: (value) => values.includes(value),
    message: msg ?? `Must be one of: ${values.join(', ')}`,
  };
}

export function custom<T>(fn: (value: T) => boolean | string, msg?: string): ValidationRule<T> {
  return {
    validate: fn,
    message: msg,
  };
}

// --- Validation Functions ---

/** Validate a single field value against an array of rules */
export function validateField<T>(value: T, rules: ValidationRule<T>[]): string[] {
  const errors: string[] = [];
  for (const rule of rules) {
    const result = rule.validate(value);
    if (result === false) {
      errors.push(rule.message ?? 'Invalid value');
    } else if (typeof result === 'string') {
      errors.push(result);
    }
  }
  return errors;
}

/** Validate an entire object against a schema */
export function validate<T extends Record<string, unknown>>(
  data: T,
  schema: ValidationSchema<T>,
): ValidationResult {
  const errors: Record<string, string[]> = {};
  let valid = true;

  for (const key of Object.keys(schema) as (keyof T)[]) {
    const rules = schema[key];
    if (!rules) continue;

    const fieldErrors = validateField(data[key], rules as ValidationRule<unknown>[]);
    if (fieldErrors.length > 0) {
      errors[key as string] = fieldErrors;
      valid = false;
    }
  }

  return { valid, errors };
}

/** Check if validation result is valid */
export function isValid(result: ValidationResult): boolean {
  return result.valid;
}

/** Get errors for a specific field */
export function getFieldErrors(result: ValidationResult, field: string): string[] {
  return result.errors[field] ?? [];
}
