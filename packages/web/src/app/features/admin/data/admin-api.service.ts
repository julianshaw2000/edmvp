import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';
import { UserResponse, CreateUserRequest, RmapSmelterResponse, ComplianceFlagResponse } from './admin.models';
import { BatchResponse, PagedResponse, ComplianceSummary } from '../../supplier/data/supplier.models';

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
}
