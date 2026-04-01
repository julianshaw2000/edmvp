import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { BatchCardComponent } from './ui/batch-card.component';
import { SupplierOnboardingComponent } from './ui/supplier-onboarding.component';
import { OfflineBannerComponent } from '../../shared/ui/offline-banner.component';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, BatchCardComponent, SupplierOnboardingComponent, OfflineBannerComponent],
  template: `
    <app-offline-banner />
    <app-page-header
      title="Supplier Dashboard"
      subtitle="Manage your batches and custody events"
      actionLabel="New Batch"
      (actionClicked)="onNewBatch()"
    />

    <app-supplier-onboarding />

    <!-- Stat Cards -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      <div class="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-indigo-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ facade.batches().length }}</p>
            <p class="text-xs text-slate-500 font-medium">Total Batches</p>
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ compliantCount() }}</p>
            <p class="text-xs text-slate-500 font-medium">Compliant</p>
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-amber-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ flaggedCount() }}</p>
            <p class="text-xs text-slate-500 font-medium">Flagged</p>
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center">
            <svg class="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ pendingCount() }}</p>
            <p class="text-xs text-slate-500 font-medium">Pending</p>
          </div>
        </div>
      </div>
    </div>

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else if (!facade.hasBatches()) {
      <app-empty-state
        message="No batches yet. Create your first batch to get started."
        ctaLabel="Create Batch"
        (ctaClicked)="onNewBatch()"
      />
    } @else {
      <div class="flex items-center gap-3 mb-6">
        <div class="relative flex-1">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="text"
            placeholder="Search batches..."
            [value]="searchQuery()"
            (input)="searchQuery.set($any($event.target).value)"
            class="w-full pl-10 pr-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
        </div>
        <select
          [value]="statusFilter()"
          (change)="statusFilter.set($any($event.target).value)"
          class="px-4 py-2.5 border border-slate-300 rounded-xl text-sm"
        >
          <option value="ALL">All Statuses</option>
          <option value="COMPLIANT">Compliant</option>
          <option value="FLAGGED">Flagged</option>
          <option value="PENDING">Pending</option>
        </select>
      </div>

      <h2 class="text-lg font-semibold text-slate-900 mb-4">Recent Batches</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (batch of filteredBatches(); track batch.id) {
          <app-batch-card
            [batch]="batch"
            (selected)="onBatchSelected(batch.id)"
          />
        }
      </div>
    }
  `,
})
export class SupplierDashboardComponent implements OnInit {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  searchQuery = signal('');
  statusFilter = signal<string>('ALL');

  filteredBatches = computed(() => {
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();
    return this.facade.batches().filter(b => {
      const matchesSearch = !query ||
        b.batchNumber.toLowerCase().includes(query) ||
        b.originCountry.toLowerCase().includes(query);
      const matchesStatus = status === 'ALL' ||
        b.complianceStatus === status ||
        (status === 'FLAGGED' && b.complianceStatus === 'FLAG') ||
        (status === 'PENDING' && b.complianceStatus === 'INSUFFICIENT_DATA');
      return matchesSearch && matchesStatus;
    });
  });

  ngOnInit() {
    this.facade.loadBatches();
  }

  compliantCount() {
    return this.facade.batches().filter(b => b.complianceStatus === 'COMPLIANT').length;
  }

  flaggedCount() {
    return this.facade.batches().filter(b => b.complianceStatus === 'FLAGGED' || b.complianceStatus === 'FLAG').length;
  }

  pendingCount() {
    return this.facade.batches().filter(b => b.complianceStatus === 'PENDING' || b.complianceStatus === 'INSUFFICIENT_DATA').length;
  }

  onNewBatch() {
    this.router.navigate(['/supplier/new-batch']);
  }

  onBatchSelected(batchId: string) {
    this.router.navigate(['/supplier/batch', batchId]);
  }
}
