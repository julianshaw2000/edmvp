import { Component, inject, OnInit } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { AdminFacade } from './admin.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

@Component({
  selector: 'app-compliance-review',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, StatusBadgeComponent, SlicePipe],
  template: `
    <app-page-header
      title="Compliance Review"
      subtitle="Review batches with flagged compliance status"
    />

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else if (facade.batchesError()) {
      <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
        <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-sm text-rose-700">{{ facade.batchesError() }}</p>
      </div>
    } @else if (facade.flaggedBatches().length === 0) {
      <app-empty-state message="No flagged batches found. All compliance checks are passing." />
    } @else {
      <div class="space-y-4">
        @for (batch of facade.flaggedBatches(); track batch.id) {
          <div class="bg-white rounded-xl border border-amber-200 shadow-sm p-6 hover:shadow-md transition-shadow duration-200">
            <div class="flex items-start justify-between mb-4">
              <div>
                <h3 class="text-base font-semibold text-slate-900">Batch #{{ batch.batchNumber }}</h3>
                <p class="text-sm text-slate-500 mt-0.5">{{ batch.mineralType }} -- {{ batch.originCountry }}</p>
              </div>
              <app-status-badge [status]="batch.complianceStatus" />
            </div>
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div>
                <p class="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Origin Mine</p>
                <p class="text-slate-700 font-medium">{{ batch.originMine }}</p>
              </div>
              <div>
                <p class="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Weight</p>
                <p class="text-slate-700 font-medium">{{ batch.weightKg }} kg</p>
              </div>
              <div>
                <p class="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Batch Status</p>
                <app-status-badge [status]="batch.status" />
              </div>
              <div>
                <p class="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Created</p>
                <p class="text-slate-700 font-medium">{{ batch.createdAt | slice: 0 : 10 }}</p>
              </div>
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class ComplianceReviewComponent implements OnInit {
  protected facade = inject(AdminFacade);

  ngOnInit() {
    this.facade.loadBatches();
  }
}
