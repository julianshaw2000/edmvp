import { Injectable, inject } from '@angular/core';
import { BuyerStore } from './buyer.store';

@Injectable({ providedIn: 'root' })
export class BuyerFacade {
  private store = inject(BuyerStore);

  // Batch list
  readonly batches = this.store.batches;
  readonly batchesLoading = this.store.batchesLoading;
  readonly batchesError = this.store.batchesError;
  readonly totalCount = this.store.totalCount;
  readonly compliantCount = this.store.compliantCount;
  readonly flaggedCount = this.store.flaggedCount;
  readonly pendingCount = this.store.pendingCount;
  readonly insufficientDataCount = this.store.insufficientDataCount;

  // Detail
  readonly selectedBatch = this.store.selectedBatch;
  readonly events = this.store.events;
  readonly documents = this.store.documents;
  readonly compliance = this.store.compliance;
  readonly detailLoading = this.store.detailLoading;

  // Generation
  readonly generating = this.store.generating;
  readonly generatedDoc = this.store.generatedDoc;
  readonly generateError = this.store.generateError;

  loadBatches(page?: number) { this.store.loadBatches(page); }
  loadBatchDetail(batchId: string) { this.store.loadBatchDetail(batchId); }
  generatePassport(batchId: string) { this.store.generatePassport(batchId); }
  generateDossier(batchId: string) { this.store.generateDossier(batchId); }
  shareDocument(docId: string) { this.store.shareDocument(docId); }
}
