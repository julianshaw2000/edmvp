import { Component, inject, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

const EVENT_TYPES = [
  { value: 'MINE_EXTRACTION', label: 'Mine Extraction' },
  { value: 'CONCENTRATION', label: 'Concentration' },
  { value: 'TRADING_TRANSFER', label: 'Trading/Transfer' },
  { value: 'LABORATORY_ASSAY', label: 'Laboratory Assay' },
  { value: 'PRIMARY_PROCESSING', label: 'Primary Processing (Smelting)' },
  { value: 'EXPORT_SHIPMENT', label: 'Export/Shipment' },
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
    <app-page-header title="Submit Custody Event" />

    <div class="max-w-2xl">
      <form (ngSubmit)="onSubmit()" class="space-y-6">
        <!-- Batch ID -->
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Batch ID</label>
          <input
            type="text"
            [(ngModel)]="batchId"
            name="batchId"
            required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Batch UUID"
          />
        </div>

        <!-- Event Type -->
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Event Type</label>
          <select
            [(ngModel)]="eventType"
            name="eventType"
            (ngModelChange)="onEventTypeChange()"
            required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
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
            <label class="block text-sm font-medium text-slate-700 mb-1">Event Date</label>
            <input type="datetime-local" [(ngModel)]="eventDate" name="eventDate" required
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Location</label>
            <input type="text" [(ngModel)]="location" name="location" required
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Actor Name</label>
          <input type="text" [(ngModel)]="actorName" name="actorName" required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Description</label>
          <textarea [(ngModel)]="description" name="description" required rows="3"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"></textarea>
        </div>

        <!-- Dynamic Metadata Fields -->
        @if (currentMetadataFields().length > 0) {
          <div class="border-t border-slate-200 pt-4">
            <h3 class="text-sm font-semibold text-slate-700 mb-3">Event-Specific Fields</h3>
            <div class="space-y-3">
              @for (field of currentMetadataFields(); track field.key) {
                <div>
                  <label class="block text-sm font-medium text-slate-600 mb-1">{{ field.label }}</label>
                  <input
                    [type]="field.type"
                    [ngModel]="metadata()[field.key]"
                    (ngModelChange)="setMetadata(field.key, $event)"
                    [name]="'meta_' + field.key"
                    required
                    class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              }
            </div>
          </div>
        }

        <!-- Error -->
        @if (facade.submitError()) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
            {{ facade.submitError() }}
          </div>
        }

        <!-- Submit -->
        <button
          type="submit"
          [disabled]="facade.submitting()"
          class="w-full bg-blue-600 text-white py-2.5 px-4 rounded-lg font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
        >
          {{ facade.submitting() ? 'Submitting...' : 'Submit Event' }}
        </button>
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
