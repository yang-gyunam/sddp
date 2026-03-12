export type FieldType =
  | 'String'
  | 'Int'
  | 'Long'
  | 'Decimal'
  | 'Boolean'
  | 'DateTime'
  | 'Guid'
  | 'Json';

export interface EntityField {
  id: string;
  fieldName: string;
  columnName: string;
  fieldType: FieldType;
  isRequired: boolean;
  isUnique: boolean;
  maxLength: number | null;
  minLength: number | null;
  validationType: string | null;
  pattern: string | null;
  defaultValue: string | null;
  description: string;
  displayOrder: number;
}

export interface EntityMetadata {
  id: string;
  specId: string;
  entityName: string;
  tableName: string;
  namespace: string;
  description: string;
  baseClass: string;
  isGenerated: boolean;
  createdAt: string;
  updatedAt: string;
  fields: EntityField[];
}

export interface EntityFieldInput {
  id?: string;
  fieldName: string;
  columnName: string;
  fieldType: FieldType;
  isRequired?: boolean;
  isUnique?: boolean;
  maxLength?: number | null;
  minLength?: number | null;
  validationType?: string | null;
  pattern?: string | null;
  defaultValue?: string | null;
  description?: string;
  displayOrder?: number;
}

export interface CreateEntityMetadataRequest {
  entityName: string;
  tableName: string;
  namespace: string;
  description?: string;
  baseClass?: string;
  isGenerated?: boolean;
  fields: EntityFieldInput[];
}

export type UpdateEntityMetadataRequest = CreateEntityMetadataRequest;
