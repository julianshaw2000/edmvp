export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  role: string;
}

export interface RmapSmelterResponse {
  smelterId: string;
  smelterName: string;
  country: string;
  conformanceStatus: string;
  lastAuditDate: string | null;
}

export interface ComplianceFlagResponse {
  id: string;
  framework: string;
  status: string;
  checkedAt: string;
  batchNumber: string;
  eventType: string;
}

export interface JobResponse {
  id: string;
  jobType: string;
  status: string;
  referenceId: string;
  errorDetail: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface ApiKeyResponse {
  id: string;
  name: string;
  keyPrefix: string;
  isActive: boolean;
  createdAt: string;
  lastUsedAt: string | null;
}

export interface CreateApiKeyResponse {
  id: string;
  name: string;
  keyPrefix: string;
  key: string; // Full key — shown once only
}
