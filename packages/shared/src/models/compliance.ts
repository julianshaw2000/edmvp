import type { ComplianceCheckStatus, ComplianceFramework, RiskLevel } from '../enums.js';

export interface ComplianceCheck {
  id: string;
  batchId: string;
  tenantId: string;
  framework: ComplianceFramework;
  status: ComplianceCheckStatus;
  riskLevel: RiskLevel;
  findings: string[];
  checkedAt: Date;
  checkedById?: string;
  notes?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface ComplianceCheckResponse {
  id: string;
  batchId: string;
  tenantId: string;
  framework: ComplianceFramework;
  status: ComplianceCheckStatus;
  riskLevel: RiskLevel;
  findings: string[];
  checkedAt: string;
  checkedById?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}
