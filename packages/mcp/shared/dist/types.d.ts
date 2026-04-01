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
export interface ComplianceSummary {
    batchId: string;
    overallStatus: string;
    checks: {
        framework: string;
        status: string;
        checkedAt: string;
    }[];
}
export interface PagedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}
export interface SmelterResponse {
    smelterId: string;
    smelterName: string;
    country: string;
    conformanceStatus: string;
    mineralType?: string;
}
export interface SupplierEngagement {
    totalSuppliers: number;
    activeSuppliers: number;
    staleSuppliers: number;
    flaggedSuppliers: number;
    suppliers: {
        id: string;
        displayName: string;
        lastEventDate: string | null;
        batchCount: number;
        flaggedBatchCount: number;
        status: string;
    }[];
}
export interface AnalyticsResponse {
    totalBatches: number;
    completedBatches: number;
    flaggedBatches: number;
    pendingBatches: number;
    totalEvents: number;
    totalUsers: number;
}
export interface AuditLogEntry {
    id: string;
    userDisplayName: string;
    action: string;
    entityType: string;
    entityId: string;
    result: string;
    timestamp: string;
}
export interface TenantResponse {
    id: string;
    name: string;
    status: string;
    createdAt: string;
    userCount: number;
    batchCount: number;
}
export interface UserResponse {
    id: string;
    email: string;
    displayName: string;
    role: string;
    isActive: boolean;
}
export interface ApiError {
    error: string;
    status: number;
}
