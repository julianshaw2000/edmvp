import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

const MINERAL_TYPES = [
  { value: 'TUNGSTEN', label: 'Tungsten' },
  { value: 'TIN', label: 'Tin' },
  { value: 'TANTALUM', label: 'Tantalum' },
  { value: 'GOLD', label: 'Gold' },
];

@Component({
  selector: 'app-create-batch',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  template: `
    <app-page-header
      title="Create New Batch"
      subtitle="Register a new mineral batch in the supply chain"
    />

    <div class="max-w-2xl">
      <form (ngSubmit)="onSubmit()" class="space-y-6 bg-white rounded-xl border border-slate-200 p-6">

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Batch Number</label>
          <input
            type="text"
            [(ngModel)]="batchNumber"
            name="batchNumber"
            required
            placeholder="e.g. BATCH-2026-001"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Mineral Type</label>
          <select
            [(ngModel)]="mineralType"
            name="mineralType"
            required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">Select mineral type...</option>
            @for (m of mineralTypes; track m.value) {
              <option [value]="m.value">{{ m.label }}</option>
            }
          </select>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Origin Country (ISO)</label>
            <input
              type="text"
              [(ngModel)]="originCountry"
              name="originCountry"
              required
              placeholder="e.g. CD"
              maxlength="3"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Mine Site</label>
            <input
              type="text"
              [(ngModel)]="mineSite"
              name="mineSite"
              required
              placeholder="Mine or site name"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Estimated Weight (kg)</label>
          <input
            type="number"
            [(ngModel)]="estimatedWeight"
            name="estimatedWeight"
            required
            min="0"
            step="0.01"
            placeholder="0.00"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        @if (facade.submitError()) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
            {{ facade.submitError() }}
          </div>
        }

        <div class="flex items-center gap-3 pt-2">
          <button
            type="submit"
            [disabled]="facade.submitting()"
            class="bg-blue-600 text-white py-2.5 px-6 rounded-lg font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {{ facade.submitting() ? 'Creating...' : 'Create Batch' }}
          </button>
          <button
            type="button"
            (click)="onCancel()"
            class="text-sm text-slate-500 hover:text-slate-700 transition-colors"
          >
            Cancel
          </button>
        </div>

      </form>
    </div>
  `,
})
export class CreateBatchComponent {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  mineralTypes = MINERAL_TYPES;

  batchNumber = '';
  mineralType = '';
  originCountry = '';
  mineSite = '';
  estimatedWeight: number | null = null;

  onSubmit() {
    if (!this.batchNumber || !this.mineralType || !this.originCountry || !this.mineSite || this.estimatedWeight == null) {
      return;
    }
    this.facade.createBatch({
      batchNumber: this.batchNumber,
      mineralType: this.mineralType,
      originCountry: this.originCountry,
      originMine: this.mineSite,
      weightKg: this.estimatedWeight,
    });
    this.router.navigate(['/supplier']);
  }

  onCancel() {
    this.router.navigate(['/supplier']);
  }
}
