import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BuyerFacade } from './buyer.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { BatchTableComponent } from './ui/batch-table.component';

@Component({
  selector: 'app-buyer-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, BatchTableComponent],
  template: `
    <app-page-header
      title="Buyer Dashboard"
      subtitle="All batches across the supply chain"
    />

    <!-- Compliance overview cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="bg-green-50 border border-green-200 rounded-lg p-4 text-center">
        <p class="text-2xl font-bold text-green-700">{{ facade.compliantCount() }}</p>
        <p class="text-sm text-green-600 mt-1">Compliant</p>
      </div>
      <div class="bg-amber-50 border border-amber-200 rounded-lg p-4 text-center">
        <p class="text-2xl font-bold text-amber-700">{{ facade.flaggedCount() }}</p>
        <p class="text-sm text-amber-600 mt-1">Flagged</p>
      </div>
      <div class="bg-slate-50 border border-slate-200 rounded-lg p-4 text-center">
        <p class="text-2xl font-bold text-slate-700">{{ facade.pendingCount() }}</p>
        <p class="text-sm text-slate-600 mt-1">Pending</p>
      </div>
      <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4 text-center">
        <p class="text-2xl font-bold text-yellow-700">{{ facade.insufficientDataCount() }}</p>
        <p class="text-sm text-yellow-600 mt-1">Insufficient Data</p>
      </div>
    </div>

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else {
      @if (facade.batchesError(); as err) {
        <div class="bg-red-50 border border-red-200 rounded-lg p-4 text-sm text-red-700">
          {{ err }}
        </div>
      }
      <app-buyer-batch-table
        [batches]="facade.batches()"
        (batchSelected)="onBatchSelected($event)"
      />
    }
  `,
})
export class BuyerDashboardComponent implements OnInit {
  protected facade = inject(BuyerFacade);
  private router = inject(Router);

  ngOnInit() {
    this.facade.loadBatches();
  }

  onBatchSelected(batchId: string) {
    this.router.navigate(['/buyer/batch', batchId]);
  }
}
