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

      <!-- Tabs -->
      <div class="mb-6 border-b border-slate-200">
        <nav class="flex gap-6">
          @for (tab of tabs; track tab.id) {
            <button
              (click)="activeTab.set(tab.id)"
              class="pb-3 text-sm font-medium border-b-2 transition-colors"
              [class]="activeTab() === tab.id
                ? 'border-indigo-600 text-indigo-600'
                : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'"
            >
              {{ tab.label }}
            </button>
          }
        </nav>
      </div>

      <!-- Tab: Overview -->
      @if (activeTab() === 'overview') {
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
            <h3 class="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">Batch Information</h3>
            <dl class="space-y-3">
              <div class="flex justify-between items-center">
                <dt class="text-sm text-slate-500">Mineral</dt>
                <dd class="text-sm font-medium text-slate-900">{{ batch.mineralType }}</dd>
              </div>
              <div class="flex justify-between items-center">
                <dt class="text-sm text-slate-500">Weight</dt>
                <dd class="text-sm font-medium text-slate-900">{{ batch.weightKg }} kg</dd>
              </div>
              <div class="flex justify-between items-center">
                <dt class="text-sm text-slate-500">Status</dt>
                <dd><app-status-badge [status]="batch.status" /></dd>
              </div>
              <div class="flex justify-between items-center">
                <dt class="text-sm text-slate-500">Compliance</dt>
                <dd><app-status-badge [status]="batch.complianceStatus" /></dd>
              </div>
            </dl>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 lg:col-span-2">
            <h3 class="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">Custody Events</h3>
            <app-event-timeline [events]="facade.events()" />
          </div>
        </div>
      }

      <!-- Tab: Events -->
      @if (activeTab() === 'events') {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <app-event-timeline [events]="facade.events()" />
        </div>
      }

      <!-- Tab: Documents -->
      @if (activeTab() === 'documents') {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <app-document-list [documents]="facade.documents()" />

          <!-- Upload Section -->
          <div class="mt-6 pt-6 border-t border-slate-200">
            <h4 class="text-sm font-semibold text-slate-700 mb-4 flex items-center gap-2">
              <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
              Upload Document
            </h4>
            <div class="flex flex-col gap-4">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-xs font-medium text-slate-500 mb-1.5">Event ID</label>
                  <input
                    type="text"
                    [(ngModel)]="uploadEventId"
                    name="uploadEventId"
                    placeholder="Event UUID"
                    class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-slate-500 mb-1.5">Document Type</label>
                  <select
                    [(ngModel)]="uploadDocumentType"
                    name="uploadDocumentType"
                    class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
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
                  class="px-4 py-2.5 border border-dashed border-slate-300 rounded-xl text-sm text-slate-600 hover:bg-slate-50 hover:border-slate-400 transition-all duration-150"
                >
                  @if (selectedFile()) {
                    <span class="flex items-center gap-2">
                      <svg class="w-4 h-4 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                      </svg>
                      {{ selectedFile()!.name }}
                    </span>
                  } @else {
                    <span class="flex items-center gap-2">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                      </svg>
                      Choose File
                    </span>
                  }
                </button>
                <button
                  type="button"
                  (click)="onUpload()"
                  [disabled]="!selectedFile() || !uploadEventId || facade.submitting()"
                  class="px-5 py-2.5 bg-indigo-600 text-white rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm shadow-indigo-600/20 transition-all duration-150"
                >
                  {{ facade.submitting() ? 'Uploading...' : 'Upload' }}
                </button>
              </div>
              @if (uploadError()) {
                <p class="text-sm text-rose-600">{{ uploadError() }}</p>
              }
            </div>
          </div>
        </div>
      }

      <!-- Tab: Compliance -->
      @if (activeTab() === 'compliance') {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center"
              [class]="batch.complianceStatus === 'COMPLIANT' ? 'bg-emerald-50' : batch.complianceStatus === 'FLAGGED' ? 'bg-amber-50' : 'bg-slate-100'">
              <svg class="w-5 h-5"
                [class]="batch.complianceStatus === 'COMPLIANT' ? 'text-emerald-600' : batch.complianceStatus === 'FLAGGED' ? 'text-amber-600' : 'text-slate-500'"
                fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <div>
              <p class="text-sm font-medium text-slate-900">Overall Compliance Status</p>
              <app-status-badge [status]="batch.complianceStatus" />
            </div>
          </div>
          <p class="text-sm text-slate-500">Detailed compliance checks are performed automatically by the background worker and will appear here once available.</p>
        </div>
      }
    }
  `,
})
export class BatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'events', label: 'Events' },
    { id: 'documents', label: 'Documents' },
    { id: 'compliance', label: 'Compliance' },
  ];
  activeTab = signal('overview');

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
