import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';

export interface FormSdStatus {
  applicabilityStatus: string;
  ruleSetVersion: string | null;
  reasoning: string | null;
  assessedAt: string | null;
}

export interface FilingCycle {
  id: string;
  reportingYear: number;
  dueDate: string;
  status: string;
  submittedAt: string | null;
  notes: string | null;
}

export interface FormSdPackageResult {
  id: string;
  downloadUrl: string;
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class FormSdApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  getBatchStatus(batchId: string): Observable<FormSdStatus> {
    return this.http.get<FormSdStatus>(`${this.apiUrl}/api/form-sd/batches/${batchId}/status`);
  }

  listFilingCycles(): Observable<{ cycles: FilingCycle[] }> {
    return this.http.get<{ cycles: FilingCycle[] }>(`${this.apiUrl}/api/form-sd/filing-cycles`);
  }

  generatePackage(reportingYear: number): Observable<FormSdPackageResult> {
    return this.http.post<FormSdPackageResult>(`${this.apiUrl}/api/form-sd/generate/${reportingYear}`, {});
  }
}
