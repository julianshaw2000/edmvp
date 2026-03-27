import { Injectable, inject } from '@angular/core';
import { BatchStore } from './batch.store';
import { CreateBatchRequest, CreateEventRequest } from '../models/batch.models';

@Injectable({ providedIn: 'root' })
export class BatchFacade {
  private store = inject(BatchStore);

  readonly batches = this.store.batches;
  readonly batchesLoading = this.store.batchesLoading;
  readonly batchesError = this.store.batchesError;
  readonly totalBatches = this.store.totalBatches;
  readonly hasBatches = this.store.hasBatches;
  readonly selectedBatch = this.store.selectedBatch;
  readonly events = this.store.events;
  readonly documents = this.store.documents;
  readonly compliance = this.store.compliance;
  readonly detailLoading = this.store.detailLoading;
  readonly submitting = this.store.submitting;
  readonly submitError = this.store.submitError;

  loadBatches(page?: number) { this.store.loadBatches(page); }
  loadBatchDetail(batchId: string) { this.store.loadBatchDetail(batchId); }
  createBatch(req: CreateBatchRequest) { this.store.createBatch(req); }
  submitEvent(batchId: string, req: CreateEventRequest) { this.store.createEvent(batchId, req); }
  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    this.store.uploadDocument(eventId, batchId, file, documentType);
  }
}
