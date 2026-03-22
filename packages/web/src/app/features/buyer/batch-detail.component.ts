import { Component, inject, OnInit, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
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
    RouterLink,
    PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent,
    EventTimelineComponent, DocumentListComponent, ComplianceSummaryComponent,
  ],
  template: `
    @if (facade.detailLoading()) {
      <app-loading-spinner />
    } @else if (facade.selectedBatch(); as batch) {
      <a routerLink="/buyer" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
        <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
        Back to Dashboard
      </a>
      <app-page-header
        [title]="'Batch: ' + batch.batchNumber"
        [subtitle]="batch.originMine + ', ' + batch.originCountry"
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
            <h3 class="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-4">Compliance Details</h3>
            <app-compliance-summary [compliance]="facade.compliance()" />
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
        </div>
      }

      <!-- Tab: Generate & Share -->
      @if (activeTab() === 'generate') {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 class="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-5">Generate Documents</h3>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
            <button
              (click)="onGeneratePassport(batch.id)"
              [disabled]="facade.generating()"
              class="flex items-center gap-4 p-5 rounded-xl border border-slate-200 hover:border-indigo-300 hover:shadow-md transition-all duration-200 text-left group disabled:opacity-50"
            >
              <div class="w-12 h-12 rounded-xl bg-indigo-50 flex items-center justify-center shrink-0 group-hover:bg-indigo-100 transition-colors">
                <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <div>
                <p class="font-semibold text-slate-900 text-sm">Material Passport</p>
                <p class="text-xs text-slate-500 mt-0.5">Full custody chain document</p>
              </div>
            </button>

            <button
              (click)="onGenerateDossier(batch.id)"
              [disabled]="facade.generating()"
              class="flex items-center gap-4 p-5 rounded-xl border border-slate-200 hover:border-indigo-300 hover:shadow-md transition-all duration-200 text-left group disabled:opacity-50"
            >
              <div class="w-12 h-12 rounded-xl bg-slate-100 flex items-center justify-center shrink-0 group-hover:bg-slate-200 transition-colors">
                <svg class="w-6 h-6 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
              </div>
              <div>
                <p class="font-semibold text-slate-900 text-sm">Audit Dossier</p>
                <p class="text-xs text-slate-500 mt-0.5">Compliance audit package</p>
              </div>
            </button>
          </div>

          @if (facade.generating()) {
            <div class="flex items-center gap-3 p-4 bg-indigo-50 rounded-xl mb-4">
              <div class="w-5 h-5 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
              <p class="text-sm text-indigo-700 font-medium">Generating document...</p>
            </div>
          }

          @if (facade.generateError(); as err) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3 mb-4">
              <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700">{{ err }}</p>
            </div>
          }

          @if (facade.generatedDoc(); as doc) {
            <div class="p-5 bg-emerald-50 border border-emerald-200 rounded-xl">
              <div class="flex items-start gap-3">
                <div class="w-8 h-8 rounded-lg bg-emerald-100 flex items-center justify-center shrink-0">
                  <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <div class="flex-1">
                  <p class="text-sm font-semibold text-emerald-800">
                    {{ doc.documentType }} generated successfully
                  </p>
                  <div class="flex flex-wrap gap-3 items-center mt-3">
                    <a
                      [href]="doc.downloadUrl"
                      target="_blank"
                      class="inline-flex items-center gap-1.5 text-sm text-indigo-600 hover:text-indigo-700 font-semibold"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                      </svg>
                      Download
                    </a>
                    <button
                      (click)="onShare(doc.id)"
                      class="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-800 font-semibold"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z" />
                      </svg>
                      Share
                    </button>
                    @if (doc.shareExpiresAt) {
                      <span class="text-xs text-slate-500">
                        Share expires: {{ doc.shareExpiresAt }}
                      </span>
                    }
                  </div>

                  @if (facade.shareUrl(); as url) {
                    <div class="mt-4 p-4 bg-emerald-100/60 border border-emerald-200 rounded-xl">
                      <p class="text-xs font-semibold text-emerald-800 mb-2">Share link ready -- copy and send to recipients:</p>
                      <div class="flex gap-2 items-center">
                        <input
                          type="text"
                          readonly
                          [value]="url"
                          class="flex-1 px-3 py-2 text-xs border border-emerald-300 rounded-lg bg-white text-slate-800 focus:outline-none"
                        />
                        <button
                          (click)="copyShareUrl(url)"
                          class="px-4 py-2 text-xs bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition-colors font-semibold"
                        >
                          Copy
                        </button>
                      </div>
                    </div>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    }
  `,
})
export class BuyerBatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(BuyerFacade);

  tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'events', label: 'Events' },
    { id: 'documents', label: 'Documents' },
    { id: 'generate', label: 'Generate & Share' },
  ];
  activeTab = signal('overview');

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

  copyShareUrl(url: string) {
    navigator.clipboard.writeText(url).catch(() => {});
  }
}
