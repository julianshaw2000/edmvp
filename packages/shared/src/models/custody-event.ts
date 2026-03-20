import type { CustodyEventType } from '../enums.js';

export interface MineExtractionMetadata {
  mineName: string;
  mineLocation: string;
  extractionDate: string;
  extractionMethod?: string;
  geologicalFormation?: string;
}

export interface ConcentrationMetadata {
  facilityName: string;
  facilityLocation: string;
  concentrationMethod: string;
  inputWeightKg: number;
  outputWeightKg: number;
  recoveryRate?: number;
}

export interface TradingTransferMetadata {
  fromParty: string;
  toParty: string;
  transferDate: string;
  contractReference?: string;
  transportMode?: string;
}

export interface LaboratoryAssayMetadata {
  laboratoryName: string;
  laboratoryAccreditation?: string;
  assayDate: string;
  assayMethod: string;
  tungstenContent?: number;
  otherElements?: Record<string, number>;
}

export interface PrimaryProcessingMetadata {
  facilityName: string;
  facilityLocation: string;
  processingMethod: string;
  inputWeightKg: number;
  outputWeightKg: number;
  processingDate: string;
}

export interface ExportShipmentMetadata {
  exporterName: string;
  destinationCountry: string;
  shipmentDate: string;
  billOfLadingNumber?: string;
  exportPermitNumber?: string;
  consigneeName?: string;
}

export type CustodyEventMetadata =
  | MineExtractionMetadata
  | ConcentrationMetadata
  | TradingTransferMetadata
  | LaboratoryAssayMetadata
  | PrimaryProcessingMetadata
  | ExportShipmentMetadata;

export interface CustodyEvent {
  id: string;
  batchId: string;
  tenantId: string;
  eventType: CustodyEventType;
  eventDate: Date;
  location: string;
  actorName: string;
  metadata: CustodyEventMetadata;
  idempotencyKey: string;
  schemaVersion: number;
  isCorrection: boolean;
  correctsEventId?: string;
  sha256Hash: string;
  previousEventHash?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateCustodyEventRequest {
  batchId: string;
  eventType: CustodyEventType;
  eventDate: string;
  location: string;
  actorName: string;
  metadata: Record<string, unknown>;
  idempotencyKey?: string;
  isCorrection?: boolean;
  correctsEventId?: string;
}
