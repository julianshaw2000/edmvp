import type { BatchStatus, ComplianceStatus } from '../enums.js';

export interface Batch {
  id: string;
  tenantId: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: BatchStatus;
  complianceStatus: ComplianceStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateBatchRequest {
  batchNumber: string;
  mineralType?: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
}

export interface BatchResponse {
  id: string;
  tenantId: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: BatchStatus;
  complianceStatus: ComplianceStatus;
  eventCount: number;
  createdAt: string;
  updatedAt: string;
}
