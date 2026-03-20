import type { DocumentType, GeneratedDocumentType } from '../enums.js';

export interface Document {
  id: string;
  batchId: string;
  tenantId: string;
  documentType: DocumentType;
  fileName: string;
  mimeType: string;
  fileSizeBytes: number;
  storageKey: string;
  sha256Hash: string;
  uploadedById: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface DocumentResponse {
  id: string;
  batchId: string;
  tenantId: string;
  documentType: DocumentType;
  fileName: string;
  mimeType: string;
  fileSizeBytes: number;
  sha256Hash: string;
  uploadedById: string;
  downloadUrl: string;
  createdAt: string;
  updatedAt: string;
}

export interface GeneratedDocument {
  id: string;
  batchId: string;
  tenantId: string;
  documentType: GeneratedDocumentType;
  fileName: string;
  mimeType: string;
  fileSizeBytes: number;
  storageKey: string;
  shareToken: string;
  shareExpiresAt: Date;
  generatedById: string;
  createdAt: Date;
  updatedAt: Date;
}
