import { Injectable, inject, signal, computed } from '@angular/core';
import { BatchApiService } from '../services/batch-api.service';
import { BatchResponse, CustodyEventResponse, DocumentResponse, ComplianceSummary, CreateBatchRequest, CreateEventRequest } from '../models/batch.models';
import { extractErrorMessage } from '../utils/error.utils';
import { OfflineDbService } from '../../core/offline/offline-db.service';
import { ConnectivityService } from '../../core/offline/connectivity.service';

@Injectable({ providedIn: 'root' })
export class BatchStore {
  private api = inject(BatchApiService);
  private offlineDb = inject(OfflineDbService);
  private connectivity = inject(ConnectivityService);

  // Batch list
  private _batches = signal<BatchResponse[]>([]);
  private _batchesLoading = signal(false);
  private _batchesError = signal<string | null>(null);
  private _totalBatches = signal(0);

  readonly batches = this._batches.asReadonly();
  readonly batchesLoading = this._batchesLoading.asReadonly();
  readonly batchesError = this._batchesError.asReadonly();
  readonly totalBatches = this._totalBatches.asReadonly();
  readonly hasBatches = computed(() => this._batches().length > 0);

  // Selected batch detail
  private _selectedBatch = signal<BatchResponse | null>(null);
  private _events = signal<CustodyEventResponse[]>([]);
  private _documents = signal<DocumentResponse[]>([]);
  private _compliance = signal<ComplianceSummary | null>(null);
  private _detailLoading = signal(false);

  readonly selectedBatch = this._selectedBatch.asReadonly();
  readonly events = this._events.asReadonly();
  readonly documents = this._documents.asReadonly();
  readonly compliance = this._compliance.asReadonly();
  readonly detailLoading = this._detailLoading.asReadonly();

  // Submission state
  private _submitting = signal(false);
  private _submitError = signal<string | null>(null);

  readonly submitting = this._submitting.asReadonly();
  readonly submitError = this._submitError.asReadonly();

  loadBatches(page = 1) {
    this._batchesLoading.set(true);
    this._batchesError.set(null);
    this.api.listBatches(page).subscribe({
      next: (res) => {
        this._batches.set(res.items);
        this._totalBatches.set(res.totalCount);
        // Cache batches for offline access
        this.offlineDb.cacheBatches(res.items.map(b => ({
          id: b.id,
          batchNumber: b.batchNumber,
          mineralType: b.mineralType,
          originCountry: b.originCountry,
          originMine: b.originMine,
          weightKg: b.weightKg,
          status: b.status,
          complianceStatus: b.complianceStatus,
          eventCount: b.eventCount,
          cachedAt: new Date().toISOString(),
        })));
        this._batchesLoading.set(false);
      },
      error: (err) => {
        this._batchesError.set(extractErrorMessage(err));
        // Fallback to cached batches when offline
        if (!this.connectivity.isOnline()) {
          this.offlineDb.getCachedBatches().then(cached => {
            if (cached.length > 0) {
              this._batches.set(cached.map(c => ({
                id: c.id,
                batchNumber: c.batchNumber,
                mineralType: c.mineralType,
                originCountry: c.originCountry,
                originMine: c.originMine,
                weightKg: c.weightKg,
                status: c.status,
                complianceStatus: c.complianceStatus,
                createdAt: c.cachedAt,
                eventCount: c.eventCount,
              })));
            }
          });
        }
        this._batchesLoading.set(false);
      },
    });
  }

  loadBatchDetail(batchId: string) {
    this._detailLoading.set(true);
    this.api.getBatch(batchId).subscribe({
      next: (batch) => {
        this._selectedBatch.set(batch);
        this._detailLoading.set(false);
      },
      error: () => this._detailLoading.set(false),
    });

    this.api.listEvents(batchId).subscribe({
      next: (res) => this._events.set(res.items),
    });

    this.api.listDocuments(batchId).subscribe({
      next: (res) => this._documents.set(res.documents),
    });

    this.api.getBatchCompliance(batchId).subscribe({
      next: (res) => this._compliance.set(res),
      error: () => this._compliance.set(null),
    });
  }

  createBatch(req: CreateBatchRequest) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.createBatch(req).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatches();
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  createEvent(batchId: string, req: CreateEventRequest) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.createEvent(batchId, req).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatchDetail(batchId);
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    this._submitting.set(true);
    return this.api.uploadDocument(eventId, file, documentType).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatchDetail(batchId);
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }
}
