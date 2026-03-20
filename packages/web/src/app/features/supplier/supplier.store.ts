import { Injectable, inject, signal, computed } from '@angular/core';
import { SupplierApiService } from './data/supplier-api.service';
import { BatchResponse, CustodyEventResponse, DocumentResponse } from './data/supplier.models';
import { extractErrorMessage } from '../../shared/utils/error.utils';

@Injectable({ providedIn: 'root' })
export class SupplierStore {
  private api = inject(SupplierApiService);

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
  private _detailLoading = signal(false);

  readonly selectedBatch = this._selectedBatch.asReadonly();
  readonly events = this._events.asReadonly();
  readonly documents = this._documents.asReadonly();
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
        this._batchesLoading.set(false);
      },
      error: (err) => {
        this._batchesError.set(extractErrorMessage(err));
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
  }

  createBatch(req: { batchNumber: string; mineralType: string; originCountry: string; originMine: string; weightKg: number }) {
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

  createEvent(batchId: string, req: any) {
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
