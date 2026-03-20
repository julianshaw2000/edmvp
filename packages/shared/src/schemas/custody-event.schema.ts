import { z } from 'zod';
import { CustodyEventType } from '../enums.js';

export const mineExtractionMetadataSchema = z.object({
  mineName: z.string().min(1),
  mineLocation: z.string().min(1),
  extractionDate: z.string(),
  extractionMethod: z.string().optional(),
  geologicalFormation: z.string().optional(),
});

export const concentrationMetadataSchema = z.object({
  facilityName: z.string().min(1),
  facilityLocation: z.string().min(1),
  concentrationMethod: z.string().min(1),
  inputWeightKg: z.number().positive(),
  outputWeightKg: z.number().positive(),
  recoveryRate: z.number().min(0).max(100).optional(),
});

export const tradingTransferMetadataSchema = z.object({
  fromParty: z.string().min(1),
  toParty: z.string().min(1),
  transferDate: z.string(),
  contractReference: z.string().optional(),
  transportMode: z.string().optional(),
});

export const laboratoryAssayMetadataSchema = z.object({
  laboratoryName: z.string().min(1),
  laboratoryAccreditation: z.string().optional(),
  assayDate: z.string(),
  assayMethod: z.string().min(1),
  tungstenContent: z.number().min(0).max(100).optional(),
  otherElements: z.record(z.number()).optional(),
});

export const primaryProcessingMetadataSchema = z.object({
  facilityName: z.string().min(1),
  facilityLocation: z.string().min(1),
  processingMethod: z.string().min(1),
  inputWeightKg: z.number().positive(),
  outputWeightKg: z.number().positive(),
  processingDate: z.string(),
});

export const exportShipmentMetadataSchema = z.object({
  exporterName: z.string().min(1),
  destinationCountry: z.string().regex(/^[A-Z]{2}$/, 'Must be ISO alpha-2 country code'),
  shipmentDate: z.string(),
  billOfLadingNumber: z.string().optional(),
  exportPermitNumber: z.string().optional(),
  consigneeName: z.string().optional(),
});

export const metadataSchemaByEventType = {
  [CustodyEventType.MINE_EXTRACTION]: mineExtractionMetadataSchema,
  [CustodyEventType.CONCENTRATION]: concentrationMetadataSchema,
  [CustodyEventType.TRADING_TRANSFER]: tradingTransferMetadataSchema,
  [CustodyEventType.LABORATORY_ASSAY]: laboratoryAssayMetadataSchema,
  [CustodyEventType.PRIMARY_PROCESSING]: primaryProcessingMetadataSchema,
  [CustodyEventType.EXPORT_SHIPMENT]: exportShipmentMetadataSchema,
} as const;

export const createCustodyEventSchema = z.object({
  batchId: z.string().min(1),
  eventType: z.nativeEnum(CustodyEventType),
  eventDate: z.string(),
  location: z.string().min(1),
  actorName: z.string().min(1),
  metadata: z.record(z.unknown()),
  idempotencyKey: z.string().optional(),
  isCorrection: z.boolean().optional(),
  correctsEventId: z.string().optional(),
});

export type CreateCustodyEventInput = z.infer<typeof createCustodyEventSchema>;
