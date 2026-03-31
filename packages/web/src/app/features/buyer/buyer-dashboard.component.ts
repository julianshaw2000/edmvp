import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BuyerFacade } from './buyer.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { BatchTableComponent } from './ui/batch-table.component';
import { SupplierEngagementPanelComponent } from './ui/supplier-engagement-panel.component';

@Component({
  selector: 'app-buyer-dashboard',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, LoadingSpinnerComponent, BatchTableComponent, SupplierEngagementPanelComponent],
  template: `
    <app-page-header
      title="Buyer Dashboard"
      subtitle="All batches across the supply chain"
    />

    <!-- Quick Actions -->
    <div class="mb-6">
      <a routerLink="/buyer/form-sd"
        class="inline-flex items-center gap-3 bg-white border border-slate-200 rounded-xl px-5 py-3 shadow-sm hover:border-indigo-300 hover:shadow-md transition-all group">
        <div class="w-9 h-9 rounded-lg bg-indigo-50 flex items-center justify-center group-hover:bg-indigo-100 transition-colors">
          <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
        </div>
        <div>
          <p class="text-sm font-semibold text-slate-900">Form SD Compliance</p>
          <p class="text-xs text-slate-500">Dodd-Frank §1502 filing & support packages</p>
        </div>
        <svg class="w-4 h-4 text-slate-400 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
        </svg>
      </a>
    </div>

    <!-- Compliance Overview -->
    <div class="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
      <!-- Donut Chart Card -->
      <div class="col-span-2 lg:col-span-1 bg-white rounded-xl border border-slate-200 shadow-sm p-5 flex flex-col items-center justify-center">
        <div class="relative w-24 h-24 mb-3">
          <svg class="w-24 h-24 -rotate-90" viewBox="0 0 36 36">
            <circle cx="18" cy="18" r="14" fill="none" stroke="#F1F5F9" stroke-width="3.5" />
            <circle cx="18" cy="18" r="14" fill="none" stroke="#10B981" stroke-width="3.5"
              [attr.stroke-dasharray]="compliantPct() + ' ' + (100 - compliantPct())"
              stroke-dashoffset="0" stroke-linecap="round" />
            <circle cx="18" cy="18" r="14" fill="none" stroke="#F59E0B" stroke-width="3.5"
              [attr.stroke-dasharray]="flaggedPct() + ' ' + (100 - flaggedPct())"
              [attr.stroke-dashoffset]="-(compliantPct())" stroke-linecap="round" />
            <circle cx="18" cy="18" r="14" fill="none" stroke="#F43F5E" stroke-width="3.5"
              [attr.stroke-dasharray]="insufficientPct() + ' ' + (100 - insufficientPct())"
              [attr.stroke-dashoffset]="-(compliantPct() + flaggedPct())" stroke-linecap="round" />
          </svg>
          <div class="absolute inset-0 flex flex-col items-center justify-center">
            <span class="text-lg font-bold text-slate-900">{{ facade.batches().length }}</span>
            <span class="text-[10px] text-slate-400 font-medium">TOTAL</span>
          </div>
        </div>
      </div>

      <!-- Stat Cards -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-lg bg-emerald-50 flex items-center justify-center">
            <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Compliant</span>
        </div>
        <p class="text-3xl font-bold text-slate-900">{{ facade.compliantCount() }}</p>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-lg bg-amber-50 flex items-center justify-center">
            <svg class="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Flagged</span>
        </div>
        <p class="text-3xl font-bold text-slate-900">{{ facade.flaggedCount() }}</p>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center">
            <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Pending</span>
        </div>
        <p class="text-3xl font-bold text-slate-900">{{ facade.pendingCount() }}</p>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-lg bg-rose-50 flex items-center justify-center">
            <svg class="w-4 h-4 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Insufficient</span>
        </div>
        <p class="text-3xl font-bold text-slate-900">{{ facade.insufficientDataCount() }}</p>
      </div>
    </div>

    <!-- Supplier Engagement -->
    <app-supplier-engagement-panel
      [engagement]="facade.engagement()"
      [nudgingSupplier]="facade.nudgingSupplier()"
      (nudgeClicked)="facade.nudgeSupplier($event)"
    />

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else {
      @if (facade.batchesError(); as err) {
        <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3 mb-6">
          <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm text-rose-700">{{ err }}</p>
        </div>
      }
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <app-buyer-batch-table
          [batches]="facade.batches()"
          (batchSelected)="onBatchSelected($event)"
        />
      </div>
    }
  `,
})
export class BuyerDashboardComponent implements OnInit {
  protected facade = inject(BuyerFacade);
  private router = inject(Router);

  ngOnInit() {
    this.facade.loadBatches();
    this.facade.loadEngagement();
  }

  compliantPct(): number {
    const total = this.facade.batches().length;
    return total ? (this.facade.compliantCount() / total) * 100 : 0;
  }

  flaggedPct(): number {
    const total = this.facade.batches().length;
    return total ? (this.facade.flaggedCount() / total) * 100 : 0;
  }

  insufficientPct(): number {
    const total = this.facade.batches().length;
    return total ? (this.facade.insufficientDataCount() / total) * 100 : 0;
  }

  onBatchSelected(batchId: string) {
    this.router.navigate(['/buyer/batch', batchId]);
  }
}
