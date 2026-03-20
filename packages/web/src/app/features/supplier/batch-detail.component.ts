import { Component, inject, OnInit, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
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
    FormsModule,
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
        <div class="flex items-center justify-between mb-3">
          <h3 class="font-semibold text-slate-900">Documents</h3>
        </div>
        <app-document-list [documents]="facade.documents()" />

        <!-- Upload Section -->
        <div class="mt-4 border-t border-slate-100 pt-4">
          <h4 class="text-sm font-medium text-slate-700 mb-3">Upload Document</h4>
          <div class="flex flex-col gap-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs text-slate-500 mb-1">Event ID</label>
                <input
                  type="text"
                  [(ngModel)]="uploadEventId"
                  name="uploadEventId"
                  placeholder="Event UUID"
                  class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label class="block text-xs text-slate-500 mb-1">Document Type</label>
                <select
                  [(ngModel)]="uploadDocumentType"
                  name="uploadDocumentType"
                  class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
                >
                  <option value="CERTIFICATE">Certificate</option>
                  <option value="PERMIT">Permit</option>
                  <option value="INVOICE">Invoice</option>
                  <option value="ASSAY_REPORT">Assay Report</option>
                  <option value="OTHER">Other</option>
                </select>
              </div>
            </div>
            <div class="flex items-center gap-3">
              <input
                #fileInput
                type="file"
                class="hidden"
                (change)="onFileSelected($event)"
              />
              <button
                type="button"
                (click)="fileInput.click()"
                class="px-4 py-2 border border-slate-300 rounded-lg text-sm text-slate-700 hover:bg-slate-50 transition-colors"
              >
                @if (selectedFile()) {
                  {{ selectedFile()!.name }}
                } @else {
                  Choose File
                }
              </button>
              <button
                type="button"
                (click)="onUpload()"
                [disabled]="!selectedFile() || !uploadEventId || facade.submitting()"
                class="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {{ facade.submitting() ? 'Uploading...' : 'Upload' }}
              </button>
            </div>
            @if (uploadError()) {
              <p class="text-sm text-red-600">{{ uploadError() }}</p>
            }
          </div>
        </div>
      </div>
    }
  `,
})
export class BatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  uploadEventId = '';
  uploadDocumentType = 'CERTIFICATE';
  selectedFile = signal<File | null>(null);
  uploadError = signal<string | null>(null);

  ngOnInit() {
    this.facade.loadBatchDetail(this.id());
  }

  onSubmitEvent() {
    this.router.navigate(['/supplier/submit'], { queryParams: { batchId: this.id() } });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    this.uploadError.set(null);
  }

  onUpload() {
    const file = this.selectedFile();
    if (!file || !this.uploadEventId) return;
    this.uploadError.set(null);
    this.facade.uploadDocument(this.uploadEventId, this.id(), file, this.uploadDocumentType);
    this.selectedFile.set(null);
    this.uploadEventId = '';
  }
}
