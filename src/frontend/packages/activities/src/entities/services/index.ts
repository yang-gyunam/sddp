import { EntityMetadataService } from './EntityMetadataService';

let entityMetadataService: EntityMetadataService | null = null;

export function getEntityMetadataService(): EntityMetadataService {
  if (!entityMetadataService) {
    entityMetadataService = new EntityMetadataService();
  }
  return entityMetadataService;
}

export * from './EntityMetadataService';
