import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../../core/http/api-url.token';

interface VerifyBatchResponse {
  batchId: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  complianceStatus: string;
  eventCount: number;
  hashChainIntact: boolean;
}

@Component({
  selector: 'app-verify-batch',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-6">

      <!-- Header -->
      <div class="flex items-center gap-3 mb-8">
        <div class="w-10 h-10 bg-indigo-600 rounded-xl flex items-center justify-center">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
          </svg>
        </div>
        <div>
          <h1 class="text-xl font-bold text-slate-900 leading-tight">AccuTrac</h1>
          <p class="text-xs text-slate-500 font-medium tracking-wide uppercase">Supply Chain Verification</p>
        </div>
      </div>

      <!-- Card -->
      <div class="bg-white rounded-2xl border border-slate-200 shadow-sm max-w-lg w-full overflow-hidden">

        @if (loading()) {
          <div class="text-center py-16">
            <div class="inline-block w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="text-slate-500 text-sm">Verifying batch...</p>
          </div>
        } @else if (error()) {
          <div class="text-center py-16 px-8">
            <div class="w-14 h-14 bg-rose-50 rounded-2xl flex items-center justify-center mx-auto mb-4">
              <svg class="w-7 h-7 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 class="text-lg font-bold text-slate-900 mb-2">Batch Not Found</h2>
            <p class="text-sm text-slate-500">{{ error() }}</p>
          </div>
        } @else if (batch()) {
          <!-- Status Banner -->
          <div [class]="statusBannerClass()">
            <div class="flex items-center justify-center gap-3">
              @if (batch()!.complianceStatus === 'Compliant') {
                <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                </svg>
              } @else if (batch()!.complianceStatus === 'Flagged') {
                <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5"
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
              } @else {
                <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              }
              <span class="text-xl font-bold text-white">{{ statusLabel() }}</span>
            </div>
          </div>

          <!-- Details -->
          <div class="p-6">
            <dl class="grid grid-cols-2 gap-4 mb-6">
              <div>
                <dt class="text-xs text-slate-500 font-medium uppercase tracking-wider mb-1">Batch Number</dt>
                <dd class="text-sm font-semibold text-slate-900">{{ batch()!.batchNumber }}</dd>
              </div>
              <div>
                <dt class="text-xs text-slate-500 font-medium uppercase tracking-wider mb-1">Mineral</dt>
                <dd class="text-sm font-semibold text-slate-900">{{ batch()!.mineralType }}</dd>
              </div>
              <div>
                <dt class="text-xs text-slate-500 font-medium uppercase tracking-wider mb-1">Origin</dt>
                <dd class="text-sm font-semibold text-slate-900">{{ batch()!.originCountry }}</dd>
              </div>
              <div>
                <dt class="text-xs text-slate-500 font-medium uppercase tracking-wider mb-1">Custody Events</dt>
                <dd class="text-sm font-semibold text-slate-900">{{ batch()!.eventCount }}</dd>
              </div>
            </dl>

            <!-- Compliance Frameworks -->
            <div class="mb-5">
              <h3 class="text-xs text-slate-500 font-medium uppercase tracking-wider mb-2">Compliance Frameworks</h3>
              <div class="flex flex-wrap gap-2">
                <span class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold bg-indigo-50 text-indigo-700">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4" />
                  </svg>
                  RMAP
                </span>
                <span class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold bg-indigo-50 text-indigo-700">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4" />
                  </svg>
                  OECD DDG
                </span>
                <span class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold bg-indigo-50 text-indigo-700">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4" />
                  </svg>
                  Mass Balance
                </span>
              </div>
            </div>

            <!-- Hash Chain Integrity -->
            <div class="flex items-center gap-2 px-4 py-3 rounded-xl"
              [class]="batch()!.hashChainIntact ? 'bg-emerald-50' : 'bg-rose-50'">
              @if (batch()!.hashChainIntact) {
                <svg class="w-5 h-5 text-emerald-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                </svg>
                <span class="text-sm font-semibold text-emerald-700">Hash chain integrity verified</span>
              } @else {
                <svg class="w-5 h-5 text-rose-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
                <span class="text-sm font-semibold text-rose-700">Hash chain integrity broken</span>
              }
            </div>
          </div>
        }

        <!-- Footer -->
        <div class="px-6 py-4 border-t border-slate-100 text-center">
          <div class="flex items-center justify-center gap-2">
            <div class="w-5 h-5 bg-indigo-600 rounded flex items-center justify-center">
              <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <p class="text-xs text-slate-400 font-medium">Verified by AccuTrac &middot; accutrac.org</p>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class VerifyBatchComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  loading = signal(true);
  error = signal<string | null>(null);
  batch = signal<VerifyBatchResponse | null>(null);

  ngOnInit() {
    const batchId = this.route.snapshot.paramMap.get('batchId');
    if (!batchId) {
      this.error.set('No batch ID provided.');
      this.loading.set(false);
      return;
    }

    this.http.get<VerifyBatchResponse>(`${this.apiUrl}/api/verify/${batchId}`)
      .subscribe({
        next: (res) => {
          this.batch.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('This batch could not be found or the verification link is invalid.');
          this.loading.set(false);
        },
      });
  }

  statusBannerClass(): string {
    const status = this.batch()?.complianceStatus;
    const base = 'py-5 px-6';
    if (status === 'Compliant') return `${base} bg-gradient-to-r from-emerald-500 to-emerald-600`;
    if (status === 'Flagged') return `${base} bg-gradient-to-r from-amber-500 to-amber-600`;
    return `${base} bg-gradient-to-r from-slate-400 to-slate-500`;
  }

  statusLabel(): string {
    const status = this.batch()?.complianceStatus;
    if (status === 'Compliant') return 'Verified Compliant';
    if (status === 'Flagged') return 'Flagged for Review';
    return 'Pending Verification';
  }
}
