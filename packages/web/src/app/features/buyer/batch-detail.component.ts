import { Component, inject, OnInit, input } from '@angular/core';
import { BuyerFacade } from './buyer.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EventTimelineComponent } from '../../shared/ui/event-timeline.component';
import { DocumentListComponent } from '../../shared/ui/document-list.component';
import { ComplianceSummaryComponent } from './ui/compliance-summary.component';

@Component({
  selector: 'app-buyer-batch-detail',
  standalone: true,
  imports: [
    PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent,
    EventTimelineComponent, DocumentListComponent, ComplianceSummaryComponent,
  ],
  template: `
    @if (facade.detailLoading()) {
      <app-loading-spinner />
    } @else if (facade.selectedBatch(); as batch) {
      <app-page-header
        [title]="'Batch: ' + batch.batchNumber"
        [subtitle]="batch.originMine + ', ' + batch.originCountry"
      />

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
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

        <!-- Compliance Summary -->
        <div class="bg-white rounded-lg border border-slate-200 p-5 lg:col-span-2">
          <h3 class="font-semibold text-slate-900 mb-3">Compliance Details</h3>
          <app-compliance-summary [compliance]="facade.compliance()" />
        </div>
      </div>

      <!-- Event Timeline -->
      <div class="bg-white rounded-lg border border-slate-200 p-5 mb-6">
        <h3 class="font-semibold text-slate-900 mb-3">Custody Events</h3>
        <app-event-timeline [events]="facade.events()" />
      </div>

      <!-- Documents -->
      <div class="bg-white rounded-lg border border-slate-200 p-5 mb-6">
        <h3 class="font-semibold text-slate-900 mb-3">Documents</h3>
        <app-document-list [documents]="facade.documents()" />
      </div>

      <!-- Document Generation -->
      <div class="bg-white rounded-lg border border-slate-200 p-5">
        <h3 class="font-semibold text-slate-900 mb-4">Generate Documents</h3>

        <div class="flex flex-wrap gap-3">
          <button
            (click)="onGeneratePassport(batch.id)"
            [disabled]="facade.generating()"
            class="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            @if (facade.generating()) { Generating... } @else { Generate Material Passport }
          </button>
          <button
            (click)="onGenerateDossier(batch.id)"
            [disabled]="facade.generating()"
            class="px-4 py-2 bg-slate-700 text-white text-sm font-medium rounded-lg hover:bg-slate-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            @if (facade.generating()) { Generating... } @else { Generate Audit Dossier }
          </button>
        </div>

        @if (facade.generateError(); as err) {
          <p class="mt-3 text-sm text-red-600">{{ err }}</p>
        }

        @if (facade.generatedDoc(); as doc) {
          <div class="mt-4 p-4 bg-green-50 border border-green-200 rounded-lg">
            <p class="text-sm font-medium text-green-800 mb-2">
              {{ doc.documentType }} generated successfully
            </p>
            <div class="flex flex-wrap gap-3 items-center">
              <a
                [href]="doc.downloadUrl"
                target="_blank"
                class="text-sm text-blue-600 hover:text-blue-700 font-medium"
              >
                Download
              </a>
              <button
                (click)="onShare(doc.id)"
                class="text-sm text-slate-600 hover:text-slate-800 font-medium"
              >
                Share
              </button>
              @if (doc.shareExpiresAt) {
                <span class="text-xs text-slate-500">
                  Share expires: {{ doc.shareExpiresAt }}
                </span>
              }
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class BuyerBatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(BuyerFacade);

  ngOnInit() {
    this.facade.loadBatchDetail(this.id());
  }

  onGeneratePassport(batchId: string) {
    this.facade.generatePassport(batchId);
  }

  onGenerateDossier(batchId: string) {
    this.facade.generateDossier(batchId);
  }

  onShare(docId: string) {
    this.facade.shareDocument(docId);
  }
}
