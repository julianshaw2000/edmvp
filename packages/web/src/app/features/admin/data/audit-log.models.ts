export interface AuditLogEntry {
  id: string;
  userId: string;
  userDisplayName: string;
  action: string;
  entityType: string;
  entityId: string | null;
  payload: Record<string, unknown> | null;
  result: 'Success' | 'Failure';
  failureReason: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface AuditLogFilters {
  page: number;
  pageSize: number;
  userId?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
  tenantId?: string;
}

export interface PagedAuditLogs {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type { BatchActivity } from '../../../shared/models/batch.models';

export const AUDIT_ACTION_LABELS: Record<string, string> = {
  CreateBatch: 'Created batch',
  UpdateBatchStatus: 'Updated batch status',
  SplitBatch: 'Split batch',
  CreateCustodyEvent: 'Logged custody event',
  CreateCorrection: 'Submitted correction',
  UploadDocument: 'Uploaded document',
  GeneratePassport: 'Generated Material Passport',
  GenerateDossier: 'Generated audit dossier',
  ShareDocument: 'Shared document',
  CreateUser: 'Created user',
  UpdateUser: 'Updated user',
  UploadRmapList: 'Uploaded RMAP smelter list',
};
