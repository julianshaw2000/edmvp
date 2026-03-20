import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { BatchCardComponent } from './ui/batch-card.component';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, BatchCardComponent],
  template: `
    <app-page-header
      title="Supplier Dashboard"
      subtitle="Manage your batches and custody events"
      actionLabel="New Batch"
      (actionClicked)="onNewBatch()"
    />

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else if (!facade.hasBatches()) {
      <app-empty-state message="No batches yet. Create your first batch to get started." />
    } @else {
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (batch of facade.batches(); track batch.id) {
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

  ngOnInit() {
    this.facade.loadBatches();
  }

  onNewBatch() {
    this.router.navigate(['/supplier/submit']);
  }

  onBatchSelected(batchId: string) {
    this.router.navigate(['/supplier/batch', batchId]);
  }
}
