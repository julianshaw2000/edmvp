import { Injectable, inject, signal, computed } from '@angular/core';
import { BuyerApiService } from './data/buyer-api.service';
import {
  BatchResponse, CustodyEventResponse, DocumentResponse,
  ComplianceSummary, GeneratedDocumentResponse
} from './data/buyer.models';
import { extractErrorMessage } from '../../shared/utils/error.utils';

@Injectable({ providedIn: 'root' })
export class BuyerStore {
  private api = inject(BuyerApiService);

  // Batch list state
  private _batches = signal<BatchResponse[]>([]);
  private _batchesLoading = signal(false);
  private _batchesError = signal<string | null>(null);
  private _totalCount = signal(0);

  readonly batches = this._batches.asReadonly();
  readonly batchesLoading = this._batchesLoading.asReadonly();
  readonly batchesError = this._batchesError.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();

  readonly compliantCount = computed(() =>
    this._batches().filter(b => b.complianceStatus === 'COMPLIANT').length);
  readonly flaggedCount = computed(() =>
    this._batches().filter(b => b.complianceStatus === 'FLAGGED').length);
  readonly pendingCount = computed(() =>
    this._batches().filter(b => b.complianceStatus === 'PENDING').length);
  readonly insufficientDataCount = computed(() =>
    this._batches().filter(b => b.complianceStatus === 'INSUFFICIENT_DATA').length);

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

  // Generation state
  private _generating = signal(false);
  private _generatedDoc = signal<GeneratedDocumentResponse | null>(null);
  private _generateError = signal<string | null>(null);
  private _shareUrl = signal<string | null>(null);

  readonly generating = this._generating.asReadonly();
  readonly generatedDoc = this._generatedDoc.asReadonly();
  readonly generateError = this._generateError.asReadonly();
  readonly shareUrl = this._shareUrl.asReadonly();

  loadBatches(page = 1) {
    this._batchesLoading.set(true);
    this._batchesError.set(null);
    this.api.listBatches(page).subscribe({
      next: (res) => {
        this._batches.set(res.items);
        this._totalCount.set(res.totalCount);
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
    this._generatedDoc.set(null);
    this._generateError.set(null);
    this._shareUrl.set(null);

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
      next: (compliance) => this._compliance.set(compliance),
    });
  }

  generatePassport(batchId: string) {
    this._generating.set(true);
    this._generateError.set(null);
    this.api.generatePassport(batchId).subscribe({
      next: (doc) => {
        this._generatedDoc.set(doc);
        this._generating.set(false);
      },
      error: (err) => {
        this._generateError.set(extractErrorMessage(err));
        this._generating.set(false);
      },
    });
  }

  generateDossier(batchId: string) {
    this._generating.set(true);
    this._generateError.set(null);
    this.api.generateDossier(batchId).subscribe({
      next: (doc) => {
        this._generatedDoc.set(doc);
        this._generating.set(false);
      },
      error: (err) => {
        this._generateError.set(extractErrorMessage(err));
        this._generating.set(false);
      },
    });
  }

  shareDocument(docId: string) {
    this.api.shareDocument(docId).subscribe({
      next: (res) => {
        this._shareUrl.set(res.shareUrl);
        // Update the generated doc with share info if available
        const current = this._generatedDoc();
        if (current) {
          this._generatedDoc.set({
            ...current,
            shareExpiresAt: res.expiresAt,
          });
        }
      },
    });
  }
}
