import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/http/api-url.token';
import {
  BatchResponse, CreateBatchRequest, CustodyEventResponse, CreateEventRequest,
  DocumentResponse, PagedResponse, ComplianceSummary, BatchActivity,
} from '../models/batch.models';

@Injectable({ providedIn: 'root' })
export class BatchApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  listBatches(page = 1, pageSize = 20): Observable<PagedResponse<BatchResponse>> {
    return this.http.get<PagedResponse<BatchResponse>>(
      `${this.apiUrl}/api/batches?page=${page}&pageSize=${pageSize}`);
  }

  getBatch(id: string): Observable<BatchResponse> {
    return this.http.get<BatchResponse>(`${this.apiUrl}/api/batches/${id}`);
  }

  createBatch(req: CreateBatchRequest): Observable<BatchResponse> {
    return this.http.post<BatchResponse>(`${this.apiUrl}/api/batches`, req);
  }

  listEvents(batchId: string, page = 1, pageSize = 50): Observable<PagedResponse<CustodyEventResponse>> {
    return this.http.get<PagedResponse<CustodyEventResponse>>(
      `${this.apiUrl}/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`);
  }

  createEvent(batchId: string, req: CreateEventRequest): Observable<CustodyEventResponse> {
    return this.http.post<CustodyEventResponse>(
      `${this.apiUrl}/api/batches/${batchId}/events`, req);
  }

  listDocuments(batchId: string): Observable<{ documents: DocumentResponse[]; totalCount: number }> {
    return this.http.get<{ documents: DocumentResponse[]; totalCount: number }>(
      `${this.apiUrl}/api/batches/${batchId}/documents`);
  }

  uploadDocument(eventId: string, file: File, documentType: string): Observable<DocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    return this.http.post<DocumentResponse>(
      `${this.apiUrl}/api/events/${eventId}/documents`, formData);
  }

  getBatchCompliance(batchId: string): Observable<ComplianceSummary> {
    return this.http.get<ComplianceSummary>(`${this.apiUrl}/api/batches/${batchId}/compliance`);
  }

  getBatchActivity(batchId: string): Observable<BatchActivity[]> {
    return this.http.get<BatchActivity[]>(`${this.apiUrl}/api/batches/${batchId}/activity`);
  }
}
