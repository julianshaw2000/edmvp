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
