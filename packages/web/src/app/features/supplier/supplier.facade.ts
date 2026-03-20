import { Injectable, inject } from '@angular/core';
import { SupplierStore } from './supplier.store';
import { CreateEventRequest } from './data/supplier.models';

@Injectable({ providedIn: 'root' })
export class SupplierFacade {
  private store = inject(SupplierStore);

  // Read-only signals
  readonly batches = this.store.batches;
  readonly batchesLoading = this.store.batchesLoading;
  readonly batchesError = this.store.batchesError;
  readonly totalBatches = this.store.totalBatches;
  readonly hasBatches = this.store.hasBatches;
  readonly selectedBatch = this.store.selectedBatch;
  readonly events = this.store.events;
  readonly documents = this.store.documents;
  readonly detailLoading = this.store.detailLoading;
  readonly submitting = this.store.submitting;
  readonly submitError = this.store.submitError;

  loadBatches(page?: number) { this.store.loadBatches(page); }
  loadBatchDetail(batchId: string) { this.store.loadBatchDetail(batchId); }

  createBatch(req: { batchNumber: string; mineralType: string; originCountry: string; originMine: string; weightKg: number }) {
    this.store.createBatch(req);
  }

  submitEvent(batchId: string, req: CreateEventRequest) {
    this.store.createEvent(batchId, req);
  }

  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    this.store.uploadDocument(eventId, batchId, file, documentType);
  }
}
