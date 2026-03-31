import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';
import {
  BatchResponse, CustodyEventResponse, DocumentResponse,
  PagedResponse, ComplianceSummary, GeneratedDocumentResponse, ShareResponse
} from './buyer.models';
import { BatchActivity } from '../../admin/data/audit-log.models';

@Injectable({ providedIn: 'root' })
export class BuyerApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  listBatches(page = 1, pageSize = 20): Observable<PagedResponse<BatchResponse>> {
    return this.http.get<PagedResponse<BatchResponse>>(
      `${this.apiUrl}/api/batches?page=${page}&pageSize=${pageSize}`);
  }

  getBatch(id: string): Observable<BatchResponse> {
    return this.http.get<BatchResponse>(`${this.apiUrl}/api/batches/${id}`);
  }

  listEvents(batchId: string): Observable<PagedResponse<CustodyEventResponse>> {
    return this.http.get<PagedResponse<CustodyEventResponse>>(
      `${this.apiUrl}/api/batches/${batchId}/events`);
  }

  listDocuments(batchId: string): Observable<{ documents: DocumentResponse[]; totalCount: number }> {
    return this.http.get<{ documents: DocumentResponse[]; totalCount: number }>(
      `${this.apiUrl}/api/batches/${batchId}/documents`);
  }

  getBatchCompliance(batchId: string): Observable<ComplianceSummary> {
    return this.http.get<ComplianceSummary>(`${this.apiUrl}/api/batches/${batchId}/compliance`);
  }

  generatePassport(batchId: string): Observable<GeneratedDocumentResponse> {
    return this.http.post<GeneratedDocumentResponse>(
      `${this.apiUrl}/api/batches/${batchId}/passport`, {});
  }

  generateDossier(batchId: string): Observable<GeneratedDocumentResponse> {
    return this.http.post<GeneratedDocumentResponse>(
      `${this.apiUrl}/api/batches/${batchId}/dossier`, {});
  }

  generateDpp(batchId: string): Observable<GeneratedDocumentResponse> {
    return this.http.post<GeneratedDocumentResponse>(
      `${this.apiUrl}/api/batches/${batchId}/dpp`, {});
  }

  getGeneratedDocument(id: string): Observable<GeneratedDocumentResponse> {
    return this.http.get<GeneratedDocumentResponse>(
      `${this.apiUrl}/api/generated-documents/${id}`);
  }

  getBatchActivity(batchId: string): Observable<BatchActivity[]> {
    return this.http.get<BatchActivity[]>(`${this.apiUrl}/api/batches/${batchId}/activity`);
  }

  shareDocument(id: string): Observable<ShareResponse> {
    return this.http.post<ShareResponse>(
      `${this.apiUrl}/api/generated-documents/${id}/share`, {});
  }

  getSupplierEngagement(): Observable<SupplierEngagement> {
    return this.http.get<SupplierEngagement>(`${this.apiUrl}/api/buyer/supplier-engagement`);
  }
}

export interface SupplierEngagement {
  totalSuppliers: number;
  activeSuppliers: number;
  staleSuppliers: number;
  flaggedSuppliers: number;
  suppliers: SupplierEngagementItem[];
}

export interface SupplierEngagementItem {
  id: string;
  displayName: string;
  lastEventDate: string | null;
  batchCount: number;
  flaggedBatchCount: number;
  status: 'active' | 'stale' | 'flagged' | 'new';
}
