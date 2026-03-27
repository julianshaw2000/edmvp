export type {
  BatchResponse,
  CustodyEventResponse,
  DocumentResponse,
  PagedResponse,
  ComplianceSummary,
  BatchActivity,
} from '../../../shared/models/batch.models';

export interface GeneratedDocumentResponse {
  id: string;
  batchId: string;
  documentType: string;
  downloadUrl: string;
  shareToken: string | null;
  shareExpiresAt: string | null;
  generatedAt: string;
}

export interface ShareResponse {
  shareUrl: string;
  expiresAt: string;
}
