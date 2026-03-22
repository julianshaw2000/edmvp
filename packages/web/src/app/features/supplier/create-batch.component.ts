import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
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
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  template: `
    <a routerLink="/supplier" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header
      title="Create New Batch"
      subtitle="Register a new mineral batch in the supply chain"
    />

    <div class="max-w-2xl">
      <form (ngSubmit)="onSubmit()" class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-6 sm:p-8 space-y-6">
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Batch Number</label>
            <input
              type="text"
              [(ngModel)]="batchNumber"
              name="batchNumber"
              required
              placeholder="e.g. BATCH-2026-001"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
            />
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Mineral Type</label>
            <select
              [(ngModel)]="mineralType"
              name="mineralType"
              required
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
            >
              <option value="">Select mineral type...</option>
              @for (m of mineralTypes; track m.value) {
                <option [value]="m.value">{{ m.label }}</option>
              }
            </select>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Origin Country (ISO)</label>
              <input
                type="text"
                [(ngModel)]="originCountry"
                name="originCountry"
                required
                placeholder="e.g. CD"
                maxlength="3"
                class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
              />
            </div>
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Mine Site</label>
              <input
                type="text"
                [(ngModel)]="mineSite"
                name="mineSite"
                required
                placeholder="Mine or site name"
                class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Estimated Weight (kg)</label>
            <input
              type="number"
              [(ngModel)]="estimatedWeight"
              name="estimatedWeight"
              required
              min="0"
              step="0.01"
              placeholder="0.00"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
            />
          </div>

          @if (facade.submitError()) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
              <svg class="w-5 h-5 text-rose-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700">{{ facade.submitError() }}</p>
            </div>
          }
        </div>

        <div class="px-6 sm:px-8 py-4 bg-slate-50 border-t border-slate-200 flex items-center gap-3">
          <button
            type="submit"
            [disabled]="facade.submitting()"
            class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm shadow-indigo-600/20 transition-all duration-150"
          >
            {{ facade.submitting() ? 'Creating...' : 'Create Batch' }}
          </button>
          <button
            type="button"
            (click)="onCancel()"
            class="text-sm font-medium text-slate-500 hover:text-slate-700 px-4 py-2.5 rounded-xl hover:bg-slate-100 transition-all duration-150"
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
