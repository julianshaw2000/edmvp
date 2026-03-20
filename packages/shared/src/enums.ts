export const UserRole = {
  SUPPLIER: 'SUPPLIER',
  BUYER: 'BUYER',
  PLATFORM_ADMIN: 'PLATFORM_ADMIN',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const BatchStatus = {
  CREATED: 'CREATED',
  IN_TRANSIT: 'IN_TRANSIT',
  AT_PROCESSOR: 'AT_PROCESSOR',
  PROCESSING: 'PROCESSING',
  REFINED: 'REFINED',
  COMPLETED: 'COMPLETED',
} as const;
export type BatchStatus = (typeof BatchStatus)[keyof typeof BatchStatus];

export const ComplianceStatus = {
  PENDING: 'PENDING',
  COMPLIANT: 'COMPLIANT',
  FLAGGED: 'FLAGGED',
  INSUFFICIENT_DATA: 'INSUFFICIENT_DATA',
} as const;
export type ComplianceStatus = (typeof ComplianceStatus)[keyof typeof ComplianceStatus];

export const CustodyEventType = {
  MINE_EXTRACTION: 'MINE_EXTRACTION',
  CONCENTRATION: 'CONCENTRATION',
  TRADING_TRANSFER: 'TRADING_TRANSFER',
  LABORATORY_ASSAY: 'LABORATORY_ASSAY',
  PRIMARY_PROCESSING: 'PRIMARY_PROCESSING',
  EXPORT_SHIPMENT: 'EXPORT_SHIPMENT',
} as const;
export type CustodyEventType = (typeof CustodyEventType)[keyof typeof CustodyEventType];

export const ComplianceCheckStatus = {
  PASS: 'PASS',
  FAIL: 'FAIL',
  FLAG: 'FLAG',
  INSUFFICIENT_DATA: 'INSUFFICIENT_DATA',
  PENDING: 'PENDING',
} as const;
export type ComplianceCheckStatus = (typeof ComplianceCheckStatus)[keyof typeof ComplianceCheckStatus];

export const ComplianceFramework = {
  RMAP: 'RMAP',
  OECD_DDG: 'OECD_DDG',
} as const;
export type ComplianceFramework = (typeof ComplianceFramework)[keyof typeof ComplianceFramework];

export const DocumentType = {
  CERTIFICATE_OF_ORIGIN: 'CERTIFICATE_OF_ORIGIN',
  ASSAY_REPORT: 'ASSAY_REPORT',
  TRANSPORT_DOCUMENT: 'TRANSPORT_DOCUMENT',
  SMELTER_CERTIFICATE: 'SMELTER_CERTIFICATE',
  MINERALOGICAL_CERTIFICATE: 'MINERALOGICAL_CERTIFICATE',
  EXPORT_PERMIT: 'EXPORT_PERMIT',
  OTHER: 'OTHER',
} as const;
export type DocumentType = (typeof DocumentType)[keyof typeof DocumentType];

export const SmelterConformanceStatus = {
  CONFORMANT: 'CONFORMANT',
  ACTIVE_PARTICIPATING: 'ACTIVE_PARTICIPATING',
  NON_CONFORMANT: 'NON_CONFORMANT',
} as const;
export type SmelterConformanceStatus = (typeof SmelterConformanceStatus)[keyof typeof SmelterConformanceStatus];

export const RiskLevel = {
  HIGH: 'HIGH',
  MEDIUM: 'MEDIUM',
  LOW: 'LOW',
} as const;
export type RiskLevel = (typeof RiskLevel)[keyof typeof RiskLevel];

export const NotificationType = {
  COMPLIANCE_FLAG: 'COMPLIANCE_FLAG',
  DOCUMENT_AVAILABLE: 'DOCUMENT_AVAILABLE',
  PASSPORT_GENERATED: 'PASSPORT_GENERATED',
  USER_INVITED: 'USER_INVITED',
  COMPLIANCE_ESCALATION: 'COMPLIANCE_ESCALATION',
} as const;
export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export const GeneratedDocumentType = {
  MATERIAL_PASSPORT: 'MATERIAL_PASSPORT',
  AUDIT_DOSSIER: 'AUDIT_DOSSIER',
} as const;
export type GeneratedDocumentType = (typeof GeneratedDocumentType)[keyof typeof GeneratedDocumentType];

export const TenantStatus = {
  ACTIVE: 'ACTIVE',
  SUSPENDED: 'SUSPENDED',
} as const;
export type TenantStatus = (typeof TenantStatus)[keyof typeof TenantStatus];

export const JobType = {
  COMPLIANCE_CHECK: 'COMPLIANCE_CHECK',
  PASSPORT_GENERATION: 'PASSPORT_GENERATION',
  DOSSIER_GENERATION: 'DOSSIER_GENERATION',
  EMAIL_SEND: 'EMAIL_SEND',
} as const;
export type JobType = (typeof JobType)[keyof typeof JobType];

export const JobStatus = {
  QUEUED: 'QUEUED',
  RUNNING: 'RUNNING',
  COMPLETED: 'COMPLETED',
  FAILED: 'FAILED',
} as const;
export type JobStatus = (typeof JobStatus)[keyof typeof JobStatus];
