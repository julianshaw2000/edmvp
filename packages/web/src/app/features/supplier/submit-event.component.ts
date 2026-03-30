import { Component, inject, signal, computed, effect, OnInit } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { API_URL } from '../../core/http/api-url.token';
import { BatchResponse } from '../../shared/models/batch.models';

const EVENT_TYPES = [
  { value: 'MINE_EXTRACTION', label: 'Mine Extraction', step: 1 },
  { value: 'CONCENTRATION', label: 'Concentration', step: 2 },
  { value: 'TRADING_TRANSFER', label: 'Trading/Transfer', step: 3 },
  { value: 'LABORATORY_ASSAY', label: 'Laboratory Assay', step: 4 },
  { value: 'PRIMARY_PROCESSING', label: 'Primary Processing (Smelting)', step: 5 },
  { value: 'EXPORT_SHIPMENT', label: 'Export/Shipment', step: 6 },
];

const METADATA_FIELDS: Record<string, { key: string; label: string; type: string }[]> = {
  MINE_EXTRACTION: [
    { key: 'gpsCoordinates', label: 'GPS Coordinates', type: 'text' },
    { key: 'mineOperatorIdentity', label: 'Mine Operator', type: 'text' },
    { key: 'mineralogicalCertificateRef', label: 'Certificate Ref', type: 'text' },
  ],
  CONCENTRATION: [
    { key: 'facilityName', label: 'Facility Name', type: 'text' },
    { key: 'processDescription', label: 'Process Description', type: 'text' },
    { key: 'inputWeightKg', label: 'Input Weight (kg)', type: 'number' },
    { key: 'outputWeightKg', label: 'Output Weight (kg)', type: 'number' },
    { key: 'concentrationRatio', label: 'Concentration Ratio', type: 'number' },
  ],
  TRADING_TRANSFER: [
    { key: 'sellerIdentity', label: 'Seller', type: 'text' },
    { key: 'buyerIdentity', label: 'Buyer', type: 'text' },
    { key: 'transferDate', label: 'Transfer Date', type: 'datetime-local' },
    { key: 'contractReference', label: 'Contract Ref', type: 'text' },
  ],
  LABORATORY_ASSAY: [
    { key: 'laboratoryName', label: 'Laboratory', type: 'text' },
    { key: 'assayMethod', label: 'Assay Method', type: 'text' },
    { key: 'tungstenContentPct', label: 'Tungsten Content (%)', type: 'number' },
    { key: 'assayCertificateRef', label: 'Certificate Ref', type: 'text' },
  ],
  PRIMARY_PROCESSING: [
    { key: 'smelterId', label: 'Smelter (RMAP)', type: 'smelter-search' },
    { key: 'processType', label: 'Process Type', type: 'text' },
    { key: 'inputWeightKg', label: 'Input Weight (kg)', type: 'number' },
    { key: 'outputWeightKg', label: 'Output Weight (kg)', type: 'number' },
  ],
  EXPORT_SHIPMENT: [
    { key: 'originCountry', label: 'Origin Country (ISO)', type: 'text' },
    { key: 'destinationCountry', label: 'Destination Country (ISO)', type: 'text' },
    { key: 'transportMode', label: 'Transport Mode', type: 'text' },
    { key: 'exportPermitRef', label: 'Export Permit Ref', type: 'text' },
  ],
};

interface SmelterResult {
  smelterId: string;
  smelterName: string;
  country: string;
  conformanceStatus: string;
  mineralType?: string;
  sourcingCountries?: string[];
}

@Component({
  selector: 'app-submit-event',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  template: `
    @if (backBatchId) {
      <a [routerLink]="'/supplier/batch/' + backBatchId" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
        <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
        Back to Batch
      </a>
    } @else {
      <a routerLink="/supplier" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
        <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
        Back to Dashboard
      </a>
    }
    <app-page-header title="Submit Custody Event" subtitle="Record a new event in the batch lifecycle" />

    <div class="max-w-2xl">
      <!-- Step Indicator -->
      @if (eventType) {
        <div class="mb-6 bg-white rounded-xl border border-slate-200 shadow-sm p-4">
          <div class="flex items-center justify-between">
            @for (step of eventTypes; track step.value) {
              <div class="flex items-center gap-2" [class.flex-1]="!$last">
                <div
                  class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 transition-colors"
                  [class]="step.value === eventType ? 'bg-indigo-600 text-white' : 'bg-slate-100 text-slate-400'"
                >
                  {{ step.step }}
                </div>
                @if (!$last) {
                  <div class="flex-1 h-0.5 bg-slate-100 mx-1"></div>
                }
              </div>
            }
          </div>
        </div>
      }

      <form (ngSubmit)="onSubmit()" class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-6 sm:p-8 space-y-6">
          <!-- Batch ID (typeahead) -->
          <div class="relative">
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Batch</label>
            <input
              type="text"
              [(ngModel)]="batchSearchQuery"
              (ngModelChange)="onBatchSearch($event)"
              (focus)="batchDropdownOpen.set(true)"
              name="batchId"
              required
              [class]="'w-full px-4 py-2.5 border rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow ' + (selectedBatch() ? 'border-emerald-300 bg-emerald-50/30' : 'border-slate-300')"
              placeholder="Search by batch number, mineral, or country..."
              autocomplete="off"
            />
            @if (selectedBatch(); as batch) {
              <div class="mt-1.5 px-3 py-2 bg-emerald-50 border border-emerald-200 rounded-lg text-xs text-emerald-700 flex items-center justify-between">
                <span>{{ batch.batchNumber }} — {{ batch.mineralType }} · {{ batch.originCountry }} · {{ batch.weightKg }}kg</span>
                <button type="button" (click)="clearBatchSelection()" class="text-emerald-500 hover:text-emerald-700 ml-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                  </svg>
                </button>
              </div>
            }
            @if (batchDropdownOpen() && filteredBatches().length > 0 && !selectedBatch()) {
              <div class="absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg max-h-60 overflow-y-auto">
                @for (b of filteredBatches(); track b.id) {
                  <button type="button" (click)="selectBatch(b)"
                    class="w-full text-left px-4 py-2.5 text-sm hover:bg-indigo-50 border-b border-slate-100 last:border-0">
                    <span class="font-medium text-slate-900">{{ b.batchNumber }}</span>
                    <span class="text-slate-400 ml-2">{{ b.mineralType }}</span>
                    <span class="text-slate-400 ml-1">· {{ b.originCountry }}</span>
                    <span class="text-slate-400 ml-1">· {{ b.weightKg }}kg</span>
                    <span class="ml-2 text-xs" [class]="b.complianceStatus === 'COMPLIANT' ? 'text-emerald-600' : b.complianceStatus === 'FLAGGED' ? 'text-amber-600' : 'text-slate-400'">
                      {{ b.complianceStatus }}
                    </span>
                  </button>
                }
              </div>
            }
            @if (batchDropdownOpen() && filteredBatches().length === 0 && batchSearchQuery.length > 0 && !selectedBatch()) {
              <div class="absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg px-4 py-3 text-sm text-slate-500">
                No matching batches found
              </div>
            }
          </div>

          <!-- Event Type -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Event Type</label>
            <select
              [(ngModel)]="eventType"
              name="eventType"
              (ngModelChange)="onEventTypeChange()"
              required
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
            >
              <option value="">Select event type...</option>
              @for (type of eventTypes; track type.value) {
                <option [value]="type.value">{{ type.label }}</option>
              }
            </select>
          </div>

          <!-- Common Fields -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Event Date</label>
              <input type="datetime-local" [(ngModel)]="eventDate" name="eventDate" required
                class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow" />
            </div>
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Location</label>
              <input type="text" [(ngModel)]="location" name="location" required
                class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Actor Name</label>
            <input type="text" [(ngModel)]="actorName" name="actorName" required
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow" />
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Description</label>
            <textarea [(ngModel)]="description" name="description" required rows="3"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow resize-none"></textarea>
          </div>

          <!-- Dynamic Metadata Fields -->
          @if (currentMetadataFields().length > 0) {
            <div class="border-t border-slate-200 pt-6">
              <div class="flex items-center gap-2 mb-4">
                <div class="w-6 h-6 rounded-md bg-indigo-50 flex items-center justify-center">
                  <svg class="w-3.5 h-3.5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                  </svg>
                </div>
                <h3 class="text-sm font-semibold text-slate-700">Event-Specific Fields</h3>
              </div>
              <div class="space-y-4">
                @for (field of currentMetadataFields(); track field.key) {
                  <div>
                    <label class="block text-sm font-medium text-slate-600 mb-1.5">{{ field.label }}</label>
                    @if (field.type === 'smelter-search') {
                      <div class="relative">
                        <input
                          type="text"
                          [(ngModel)]="smelterSearchQuery"
                          (ngModelChange)="onSmelterSearch($event)"
                          [name]="'meta_' + field.key"
                          required
                          placeholder="Search by name or ID..."
                          class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
                        />
                        @if (selectedSmelter()) {
                          <div class="mt-1 px-3 py-2 bg-emerald-50 border border-emerald-200 rounded-lg text-xs text-emerald-700">
                            {{ selectedSmelter()!.smelterName }} ({{ selectedSmelter()!.smelterId }}) — {{ selectedSmelter()!.conformanceStatus }}
                          </div>
                        }
                        @if (smelterResults().length > 0 && !selectedSmelter()) {
                          <div class="absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg max-h-48 overflow-y-auto">
                            @for (s of smelterResults(); track s.smelterId) {
                              <button type="button" (click)="selectSmelter(s)"
                                class="w-full text-left px-4 py-2.5 text-sm hover:bg-indigo-50 border-b border-slate-100 last:border-0">
                                <span class="font-medium">{{ s.smelterName }}</span>
                                <span class="text-slate-400 ml-2">{{ s.smelterId }}</span>
                                <span class="ml-2 text-xs" [class]="s.conformanceStatus === 'CONFORMANT' ? 'text-emerald-600' : s.conformanceStatus === 'ACTIVE_PARTICIPATING' ? 'text-amber-600' : 'text-rose-600'">
                                  {{ s.conformanceStatus }}
                                </span>
                              </button>
                            }
                          </div>
                        }
                      </div>
                    } @else {
                    <input
                      [type]="field.type"
                      [ngModel]="metadata()[field.key]"
                      (ngModelChange)="setMetadata(field.key, $event)"
                      [name]="'meta_' + field.key"
                      required
                      class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
                    />
                    }
                  </div>
                }
              </div>
            </div>
          }

          <!-- Error -->
          @if (facade.submitError()) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
              <svg class="w-5 h-5 text-rose-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700">{{ facade.submitError() }}</p>
            </div>
          }
        </div>

        <!-- Footer -->
        <div class="px-6 sm:px-8 py-4 bg-slate-50 border-t border-slate-200">
          <button
            type="submit"
            [disabled]="facade.submitting()"
            class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm shadow-indigo-600/20 transition-all duration-150"
          >
            {{ facade.submitting() ? 'Submitting...' : 'Submit Event' }}
          </button>
        </div>
      </form>
    </div>
  `,
})
export class SubmitEventComponent implements OnInit {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  eventTypes = EVENT_TYPES;

  batchId = '';
  backBatchId = '';
  eventType = '';
  eventDate = '';
  location = '';
  actorName = '';
  description = '';
  metadata = signal<Record<string, unknown>>({});

  currentMetadataFields = signal<{ key: string; label: string; type: string }[]>([]);

  // Batch typeahead
  batchSearchQuery = '';
  private allBatches = signal<BatchResponse[]>([]);
  selectedBatch = signal<BatchResponse | null>(null);
  batchDropdownOpen = signal(false);
  filteredBatches = computed(() => {
    const q = this.batchSearchQuery.toLowerCase().trim();
    if (!q) return this.allBatches();
    return this.allBatches().filter(b =>
      b.batchNumber.toLowerCase().includes(q) ||
      b.mineralType.toLowerCase().includes(q) ||
      b.originCountry.toLowerCase().includes(q) ||
      b.id.toLowerCase().includes(q)
    );
  });

  // Smelter search
  smelterSearchQuery = '';
  smelterResults = signal<SmelterResult[]>([]);
  selectedSmelter = signal<SmelterResult | null>(null);
  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    const qp = this.route.snapshot.queryParams;
    if (qp['batchId']) {
      this.batchId = qp['batchId'];
      this.backBatchId = qp['batchId'];
    }
  }

  private batchSyncEffect = effect(() => {
    const batches = this.facade.batches();
    this.allBatches.set(batches as unknown as BatchResponse[]);
    if (this.batchId && !this.selectedBatch()) {
      const match = batches.find(b => b.id === this.batchId);
      if (match) {
        this.selectedBatch.set(match as unknown as BatchResponse);
        this.batchSearchQuery = match.batchNumber;
      }
    }
  });

  ngOnInit() {
    this.facade.loadBatches();
  }

  onBatchSearch(_query: string) {
    this.selectedBatch.set(null);
    this.batchDropdownOpen.set(true);
  }

  selectBatch(batch: BatchResponse) {
    this.selectedBatch.set(batch);
    this.batchId = batch.id;
    this.batchSearchQuery = batch.batchNumber;
    this.batchDropdownOpen.set(false);
  }

  clearBatchSelection() {
    this.selectedBatch.set(null);
    this.batchId = '';
    this.batchSearchQuery = '';
    this.batchDropdownOpen.set(true);
  }

  onEventTypeChange() {
    this.currentMetadataFields.set(METADATA_FIELDS[this.eventType] ?? []);
    this.metadata.set({});
    this.smelterSearchQuery = '';
    this.smelterResults.set([]);
    this.selectedSmelter.set(null);
  }

  onSmelterSearch(query: string) {
    this.selectedSmelter.set(null);
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    if (query.length < 2) {
      this.smelterResults.set([]);
      return;
    }
    this.searchTimeout = setTimeout(() => {
      this.http.get<{ items: SmelterResult[] }>(
        `${this.apiUrl}/api/smelters?q=${encodeURIComponent(query)}&pageSize=10`
      ).subscribe({
        next: (res) => this.smelterResults.set(res.items),
        error: () => this.smelterResults.set([]),
      });
    }, 300);
  }

  selectSmelter(s: SmelterResult) {
    this.selectedSmelter.set(s);
    this.smelterSearchQuery = `${s.smelterName} (${s.smelterId})`;
    this.smelterResults.set([]);
    this.setMetadata('smelterId', s.smelterId);
  }

  setMetadata(key: string, value: unknown) {
    this.metadata.update(m => ({ ...m, [key]: value }));
  }

  onSubmit() {
    const smelterId = this.eventType === 'PRIMARY_PROCESSING'
      ? this.metadata()['smelterId'] as string
      : undefined;

    this.facade.submitEvent(this.batchId, {
      eventType: this.eventType,
      eventDate: new Date(this.eventDate).toISOString(),
      location: this.location,
      actorName: this.actorName,
      smelterId,
      description: this.description,
      metadata: this.metadata(),
    });
  }
}
