import { Component, inject, OnInit, input } from '@angular/core';
import { Router } from '@angular/router';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EventTimelineComponent } from './ui/event-timeline.component';
import { DocumentListComponent } from './ui/document-list.component';

@Component({
  selector: 'app-batch-detail',
  standalone: true,
  imports: [
    PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent,
    EventTimelineComponent, DocumentListComponent,
  ],
  template: `
    @if (facade.detailLoading()) {
      <app-loading-spinner />
    } @else if (facade.selectedBatch(); as batch) {
      <app-page-header
        [title]="'Batch: ' + batch.batchNumber"
        [subtitle]="batch.originMine + ', ' + batch.originCountry"
        actionLabel="Submit Event"
        (actionClicked)="onSubmitEvent()"
      />

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Batch Info -->
        <div class="bg-white rounded-lg border border-slate-200 p-5">
          <h3 class="font-semibold text-slate-900 mb-3">Batch Info</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-slate-500">Mineral</dt>
              <dd class="text-slate-900">{{ batch.mineralType }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Weight</dt>
              <dd class="text-slate-900">{{ batch.weightKg }} kg</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Status</dt>
              <dd><app-status-badge [status]="batch.status" /></dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Compliance</dt>
              <dd><app-status-badge [status]="batch.complianceStatus" /></dd>
            </div>
          </dl>
        </div>

        <!-- Event Timeline -->
        <div class="bg-white rounded-lg border border-slate-200 p-5 lg:col-span-2">
          <h3 class="font-semibold text-slate-900 mb-3">Custody Events</h3>
          <app-event-timeline [events]="facade.events()" />
        </div>
      </div>

      <!-- Documents -->
      <div class="mt-6 bg-white rounded-lg border border-slate-200 p-5">
        <h3 class="font-semibold text-slate-900 mb-3">Documents</h3>
        <app-document-list [documents]="facade.documents()" />
      </div>
    }
  `,
})
export class BatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  ngOnInit() {
    this.facade.loadBatchDetail(this.id());
  }

  onSubmitEvent() {
    this.router.navigate(['/supplier/submit'], { queryParams: { batchId: this.id() } });
  }
}
