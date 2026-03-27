export interface BatchResponse {
  id: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: string;
  complianceStatus: string;
  createdAt: string;
  eventCount: number;
}

export interface CreateBatchRequest {
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
}

export interface CustodyEventResponse {
  id: string;
  batchId: string;
  eventType: string;
  eventDate: string;
  location: string;
  actorName: string;
  isCorrection: boolean;
  sha256Hash: string;
  createdAt: string;
}

export interface CreateEventRequest {
  eventType: string;
  eventDate: string;
  location: string;
  gpsCoordinates?: string;
  actorName: string;
  smelterId?: string;
  description: string;
  metadata: Record<string, unknown>;
}

export interface DocumentResponse {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  documentType: string;
  downloadUrl: string;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ComplianceSummary {
  batchId: string;
  overallStatus: string;
  checks: { framework: string; status: string; checkedAt: string }[];
}

export interface BatchActivity {
  id: string;
  userDisplayName: string;
  action: string;
  entityType: string;
  result: string;
  failureReason: string | null;
  timestamp: string;
}
