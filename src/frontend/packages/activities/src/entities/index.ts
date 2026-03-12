// @sddp/activities/entities - Entity Metadata
//
// Structure:
//   entities/
//   ├── types/           # Type definitions (FieldType, EntityMetadata, etc.)
//   └── services/        # API services (EntityMetadataService)

// Types (explicit exports)
export type {
  FieldType,
  EntityField,
  EntityMetadata,
  EntityFieldInput,
  CreateEntityMetadataRequest,
  UpdateEntityMetadataRequest,
} from './types';

// Services (explicit exports)
export {
  getEntityMetadata,
  createEntityMetadata,
  updateEntityMetadata,
  deleteEntityMetadata,
  EntityMetadataService,
} from './services/EntityMetadataService';

export { getEntityMetadataService } from './services';
