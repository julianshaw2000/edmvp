import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';
import { UserResponse, CreateUserRequest, RmapSmelterResponse, ComplianceFlagResponse, JobResponse, ApiKeyResponse, CreateApiKeyResponse } from './admin.models';
import { BatchResponse, PagedResponse, ComplianceSummary } from '../../supplier/data/supplier.models';
import { AuditLogFilters, PagedAuditLogs } from './audit-log.models';
import { TenantDto, CreateTenantRequest, PagedTenants } from './tenant.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  // User management
  listUsers(tenantId?: string): Observable<{ users: UserResponse[]; totalCount: number }> {
    let params = new HttpParams();
    if (tenantId) params = params.set('tenantId', tenantId);
    return this.http.get<{ users: UserResponse[]; totalCount: number }>(`${this.apiUrl}/api/users`, { params });
  }

  createUser(req: CreateUserRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.apiUrl}/api/users`, req);
  }

  updateUser(id: string, data: Partial<CreateUserRequest>): Observable<UserResponse> {
    return this.http.patch<UserResponse>(`${this.apiUrl}/api/users/${id}`, data);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/users/${id}`);
  }

  // RMAP smelter list
  uploadRmapList(file: File): Observable<{ count: number }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ count: number }>(`${this.apiUrl}/api/admin/rmap/upload`, formData);
  }

  listSmelters(): Observable<{ smelters: RmapSmelterResponse[]; totalCount: number }> {
    return this.http.get<{ smelters: RmapSmelterResponse[]; totalCount: number }>(`${this.apiUrl}/api/admin/rmap`);
  }

  // Compliance
  getBatchCompliance(batchId: string): Observable<ComplianceSummary> {
    return this.http.get<ComplianceSummary>(`${this.apiUrl}/api/batches/${batchId}/compliance`);
  }

  // Batches (admin sees all)
  listBatches(page = 1, pageSize = 50): Observable<PagedResponse<BatchResponse>> {
    return this.http.get<PagedResponse<BatchResponse>>(
      `${this.apiUrl}/api/batches?page=${page}&pageSize=${pageSize}`
    );
  }

  // Jobs
  listJobs(): Observable<{ jobs: JobResponse[]; totalCount: number }> {
    return this.http.get<{ jobs: JobResponse[]; totalCount: number }>(`${this.apiUrl}/api/admin/jobs`);
  }

  // Tenant management (PLATFORM_ADMIN only)
  listTenants(page = 1, pageSize = 20): Observable<PagedTenants> {
    return this.http.get<PagedTenants>(`${this.apiUrl}/api/platform/tenants?page=${page}&pageSize=${pageSize}`);
  }

  createTenant(request: CreateTenantRequest): Observable<TenantDto> {
    return this.http.post<TenantDto>(`${this.apiUrl}/api/platform/tenants`, request);
  }

  updateTenantStatus(id: string, status: 'ACTIVE' | 'SUSPENDED'): Observable<TenantDto> {
    return this.http.patch<TenantDto>(`${this.apiUrl}/api/platform/tenants/${id}/status`, { status });
  }

  deleteTenant(id: string): Observable<{ tenantName: string; usersRemoved: number }> {
    return this.http.delete<{ tenantName: string; usersRemoved: number }>(`${this.apiUrl}/api/platform/tenants/${id}`);
  }

  // Billing
  createBillingPortalSession() {
    return this.http.post<{ portalUrl: string }>(`${this.apiUrl}/api/billing/portal`, {});
  }

  // API Keys
  listApiKeys(): Observable<ApiKeyResponse[]> {
    return this.http.get<ApiKeyResponse[]>(`${this.apiUrl}/api/api-keys`);
  }

  createApiKey(name: string): Observable<CreateApiKeyResponse> {
    return this.http.post<CreateApiKeyResponse>(`${this.apiUrl}/api/api-keys`, { name });
  }

  revokeApiKey(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/api-keys/${id}`);
  }

  // AI features
  generateComplianceReport(period?: string): Observable<{ report: string }> {
    return this.http.post<{ report: string }>(`${this.apiUrl}/api/ai/compliance-report`, { period });
  }

  chatWithAssistant(message: string, history?: { role: string; content: string }[]): Observable<{ reply: string }> {
    return this.http.post<{ reply: string }>(`${this.apiUrl}/api/ai/chat`, { message, history });
  }

  getDataCompleteness(): Observable<{ batches: any[]; averageScore: number }> {
    return this.http.get<{ batches: any[]; averageScore: number }>(`${this.apiUrl}/api/ai/data-completeness`);
  }

  getChurnPrediction(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/ai/churn-prediction`);
  }

  getUsageCoaching(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/ai/usage-coaching`);
  }

  getRevenueSummary(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/ai/revenue-summary`);
  }

  getTenantHealth(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/ai/tenant-health`);
  }

  generateIncidentReport(batchId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/ai/incident-report`, { batchId });
  }

  queryNaturalLanguage(question: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/ai/query`, { question });
  }

  getRegulatoryUpdates(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/ai/regulatory-updates`);
  }

  // Audit logs
  getAuditLogs(filters: AuditLogFilters): Observable<PagedAuditLogs> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);
    if (filters.userId) params = params.set('userId', filters.userId);
    if (filters.action) params = params.set('action', filters.action);
    if (filters.entityType) params = params.set('entityType', filters.entityType);
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);
    if (filters.tenantId) params = params.set('tenantId', filters.tenantId);
    return this.http.get<PagedAuditLogs>(`${this.apiUrl}/api/admin/audit-logs`, { params });
  }
}
