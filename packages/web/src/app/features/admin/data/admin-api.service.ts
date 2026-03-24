import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';
import { UserResponse, CreateUserRequest, RmapSmelterResponse, ComplianceFlagResponse, JobResponse } from './admin.models';
import { BatchResponse, PagedResponse, ComplianceSummary } from '../../supplier/data/supplier.models';
import { AuditLogFilters, PagedAuditLogs } from './audit-log.models';
import { TenantDto, CreateTenantRequest, PagedTenants } from './tenant.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  // User management
  listUsers(): Observable<{ users: UserResponse[]; totalCount: number }> {
    return this.http.get<{ users: UserResponse[]; totalCount: number }>(`${this.apiUrl}/api/users`);
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

  // Billing
  createBillingPortalSession() {
    return this.http.post<{ portalUrl: string }>(`${this.apiUrl}/api/billing/portal`, {});
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
    return this.http.get<PagedAuditLogs>(`${this.apiUrl}/api/admin/audit-logs`, { params });
  }
}
