import { Component, inject, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

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
    { key: 'smelterId', label: 'Smelter ID (RMAP)', type: 'text' },
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

@Component({
  selector: 'app-submit-event',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  template: `
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
          <!-- Batch ID -->
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Batch ID</label>
            <input
              type="text"
              [(ngModel)]="batchId"
              name="batchId"
              required
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
              placeholder="Batch UUID"
            />
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
                    <input
                      [type]="field.type"
                      [ngModel]="metadata()[field.key]"
                      (ngModelChange)="setMetadata(field.key, $event)"
                      [name]="'meta_' + field.key"
                      required
                      class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
                    />
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
export class SubmitEventComponent {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  eventTypes = EVENT_TYPES;

  batchId = '';
  eventType = '';
  eventDate = '';
  location = '';
  actorName = '';
  description = '';
  metadata = signal<Record<string, unknown>>({});

  currentMetadataFields = signal<{ key: string; label: string; type: string }[]>([]);

  constructor() {
    const qp = this.route.snapshot.queryParams;
    if (qp['batchId']) this.batchId = qp['batchId'];
  }

  onEventTypeChange() {
    this.currentMetadataFields.set(METADATA_FIELDS[this.eventType] ?? []);
    this.metadata.set({});
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
