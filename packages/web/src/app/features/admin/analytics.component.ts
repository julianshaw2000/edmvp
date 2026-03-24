import { Component, inject, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { API_URL } from '../../core/http/api-url.token';

export interface AnalyticsResponse {
  totalBatches: number;
  completedBatches: number;
  flaggedBatches: number;
  pendingBatches: number;
  totalEvents: number;
  totalUsers: number;
  compliance: {
    compliant: number;
    flagged: number;
    pending: number;
  };
  mineralDistribution: { mineral: string; count: number }[];
  originCountries: { country: string; count: number }[];
  monthlyBatches: { month: string; count: number }[];
}

@Component({
  selector: 'app-analytics',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <app-page-header
      title="Analytics"
      subtitle="Compliance trends and supply chain overview"
    />

    @if (loading()) {
      <div class="flex justify-center py-16">
        <app-loading-spinner />
      </div>
    } @else if (error()) {
      <div class="bg-red-50 border border-red-200 rounded-xl p-6 text-center text-sm text-red-700">
        Failed to load analytics data. Please try again.
      </div>
    } @else if (data(); as d) {
      <!-- Metric Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
          <div class="w-9 h-9 rounded-lg bg-indigo-50 flex items-center justify-center mb-3">
            <svg class="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
            </svg>
          </div>
          <p class="text-3xl font-bold text-slate-900">{{ d.totalBatches }}</p>
          <p class="text-xs text-slate-500 mt-1 font-medium uppercase tracking-wide">Total Batches</p>
        </div>

        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
          <div class="w-9 h-9 rounded-lg bg-emerald-50 flex items-center justify-center mb-3">
            <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
          </div>
          <p class="text-3xl font-bold text-emerald-700">{{ d.completedBatches }}</p>
          <p class="text-xs text-slate-500 mt-1 font-medium uppercase tracking-wide">Completed</p>
        </div>

        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
          <div class="w-9 h-9 rounded-lg bg-red-50 flex items-center justify-center mb-3">
            <svg class="w-4 h-4 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"/>
            </svg>
          </div>
          <p class="text-3xl font-bold text-red-600">{{ d.flaggedBatches }}</p>
          <p class="text-xs text-slate-500 mt-1 font-medium uppercase tracking-wide">Flagged</p>
        </div>

        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
          <div class="w-9 h-9 rounded-lg bg-violet-50 flex items-center justify-center mb-3">
            <svg class="w-4 h-4 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2M9 11a4 4 0 100-8 4 4 0 000 8zm13 0a4 4 0 10-8 0 4 4 0 008 0z"/>
            </svg>
          </div>
          <p class="text-3xl font-bold text-slate-900">{{ d.totalUsers }}</p>
          <p class="text-xs text-slate-500 mt-1 font-medium uppercase tracking-wide">Active Users</p>
        </div>
      </div>

      <!-- Secondary metrics row -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5 flex items-center gap-4">
          <div class="w-10 h-10 rounded-lg bg-sky-50 flex items-center justify-center shrink-0">
            <svg class="w-5 h-5 text-sky-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m-8 6H4m0 0l4 4m-4-4l4-4"/>
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ d.totalEvents }}</p>
            <p class="text-xs text-slate-500 font-medium uppercase tracking-wide">Total Custody Events</p>
          </div>
        </div>
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5 flex items-center gap-4">
          <div class="w-10 h-10 rounded-lg bg-amber-50 flex items-center justify-center shrink-0">
            <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
          </div>
          <div>
            <p class="text-2xl font-bold text-slate-900">{{ d.pendingBatches }}</p>
            <p class="text-xs text-slate-500 font-medium uppercase tracking-wide">Pending Compliance</p>
          </div>
        </div>
      </div>

      <!-- Charts row -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">

        <!-- Compliance Breakdown -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 class="text-sm font-semibold text-slate-700 mb-5 uppercase tracking-wide">Compliance Breakdown</h3>
          @if (complianceTotal() === 0) {
            <p class="text-sm text-slate-400 text-center py-8">No batch data yet</p>
          } @else {
            <div class="space-y-4">
              <div>
                <div class="flex justify-between items-center mb-1.5">
                  <span class="text-sm font-medium text-slate-700">Compliant</span>
                  <span class="text-sm font-semibold text-emerald-600">{{ d.compliance.compliant }} <span class="text-slate-400 font-normal text-xs">({{ compliancePct(d.compliance.compliant) }}%)</span></span>
                </div>
                <div class="h-3 bg-slate-100 rounded-full overflow-hidden">
                  <div class="h-full rounded-full bg-emerald-500 transition-all duration-500" [style.width.%]="compliancePct(d.compliance.compliant)"></div>
                </div>
              </div>
              <div>
                <div class="flex justify-between items-center mb-1.5">
                  <span class="text-sm font-medium text-slate-700">Flagged</span>
                  <span class="text-sm font-semibold text-red-600">{{ d.compliance.flagged }} <span class="text-slate-400 font-normal text-xs">({{ compliancePct(d.compliance.flagged) }}%)</span></span>
                </div>
                <div class="h-3 bg-slate-100 rounded-full overflow-hidden">
                  <div class="h-full rounded-full bg-red-500 transition-all duration-500" [style.width.%]="compliancePct(d.compliance.flagged)"></div>
                </div>
              </div>
              <div>
                <div class="flex justify-between items-center mb-1.5">
                  <span class="text-sm font-medium text-slate-700">Pending</span>
                  <span class="text-sm font-semibold text-amber-600">{{ d.compliance.pending }} <span class="text-slate-400 font-normal text-xs">({{ compliancePct(d.compliance.pending) }}%)</span></span>
                </div>
                <div class="h-3 bg-slate-100 rounded-full overflow-hidden">
                  <div class="h-full rounded-full bg-amber-400 transition-all duration-500" [style.width.%]="compliancePct(d.compliance.pending)"></div>
                </div>
              </div>
            </div>

            <!-- Donut-style summary pills -->
            <div class="flex gap-3 mt-6 flex-wrap">
              <div class="flex items-center gap-1.5 bg-emerald-50 border border-emerald-200 rounded-full px-3 py-1">
                <div class="w-2 h-2 rounded-full bg-emerald-500"></div>
                <span class="text-xs font-medium text-emerald-700">{{ compliancePct(d.compliance.compliant) }}% Compliant</span>
              </div>
              <div class="flex items-center gap-1.5 bg-red-50 border border-red-200 rounded-full px-3 py-1">
                <div class="w-2 h-2 rounded-full bg-red-500"></div>
                <span class="text-xs font-medium text-red-700">{{ compliancePct(d.compliance.flagged) }}% Flagged</span>
              </div>
              <div class="flex items-center gap-1.5 bg-amber-50 border border-amber-200 rounded-full px-3 py-1">
                <div class="w-2 h-2 rounded-full bg-amber-400"></div>
                <span class="text-xs font-medium text-amber-700">{{ compliancePct(d.compliance.pending) }}% Pending</span>
              </div>
            </div>
          }
        </div>

        <!-- Monthly Activity -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 class="text-sm font-semibold text-slate-700 mb-5 uppercase tracking-wide">Monthly Batch Activity (Last 6 Months)</h3>
          @if (d.monthlyBatches.length === 0) {
            <p class="text-sm text-slate-400 text-center py-8">No activity in the last 6 months</p>
          } @else {
            <div class="flex items-end gap-2 h-40">
              @for (bar of monthlyBars(); track bar.month) {
                <div class="flex-1 flex flex-col items-center gap-1 min-w-0">
                  <span class="text-xs font-semibold text-slate-600">{{ bar.count }}</span>
                  <div
                    class="w-full rounded-t-md bg-indigo-500 transition-all duration-500 min-h-[4px]"
                    [style.height.%]="bar.heightPct"
                  ></div>
                  <span class="text-xs text-slate-400 truncate w-full text-center">{{ bar.label }}</span>
                </div>
              }
            </div>
          }
        </div>
      </div>

      <!-- Bottom row -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">

        <!-- Mineral Distribution -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 class="text-sm font-semibold text-slate-700 mb-5 uppercase tracking-wide">Mineral Distribution</h3>
          @if (d.mineralDistribution.length === 0) {
            <p class="text-sm text-slate-400 text-center py-8">No data available</p>
          } @else {
            <div class="space-y-3">
              @for (item of d.mineralDistribution; track item.mineral) {
                <div>
                  <div class="flex justify-between items-center mb-1">
                    <span class="text-sm font-medium text-slate-700">{{ item.mineral }}</span>
                    <span class="text-sm font-semibold text-slate-900">{{ item.count }}</span>
                  </div>
                  <div class="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                    <div
                      class="h-full rounded-full bg-sky-500 transition-all duration-500"
                      [style.width.%]="mineralPct(item.count)"
                    ></div>
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <!-- Top Origin Countries -->
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 class="text-sm font-semibold text-slate-700 mb-5 uppercase tracking-wide">Top Origin Countries</h3>
          @if (d.originCountries.length === 0) {
            <p class="text-sm text-slate-400 text-center py-8">No data available</p>
          } @else {
            <div class="space-y-2">
              @for (item of d.originCountries; track item.country; let i = $index) {
                <div class="flex items-center gap-3 py-2 border-b border-slate-100 last:border-0">
                  <span class="w-6 h-6 rounded-full bg-slate-100 flex items-center justify-center text-xs font-bold text-slate-500 shrink-0">{{ i + 1 }}</span>
                  <span class="flex-1 text-sm font-medium text-slate-800">{{ item.country }}</span>
                  <div class="flex items-center gap-2">
                    <div class="w-20 h-2 bg-slate-100 rounded-full overflow-hidden">
                      <div
                        class="h-full rounded-full bg-violet-500 transition-all duration-500"
                        [style.width.%]="countryPct(item.count)"
                      ></div>
                    </div>
                    <span class="text-sm font-semibold text-slate-700 w-8 text-right">{{ item.count }}</span>
                  </div>
                </div>
              }
            </div>
          }
        </div>
      </div>
    }
  `,
})
export class AnalyticsComponent {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  private analytics$ = this.http
    .get<AnalyticsResponse>(`${this.apiUrl}/api/analytics`)
    .pipe(catchError(() => of(null)));

  protected rawSignal = toSignal(this.analytics$, { initialValue: undefined });

  protected loading = computed(() => this.rawSignal() === undefined);
  protected error = computed(() => this.rawSignal() === null);
  protected data = computed(() => {
    const v = this.rawSignal();
    return v === null || v === undefined ? null : v;
  });

  protected complianceTotal = computed(() => {
    const d = this.data();
    if (!d) return 0;
    return d.compliance.compliant + d.compliance.flagged + d.compliance.pending;
  });

  protected compliancePct(value: number): number {
    const total = this.complianceTotal();
    if (total === 0) return 0;
    return Math.round((value / total) * 100);
  }

  protected mineralMax = computed(() => {
    const d = this.data();
    if (!d || d.mineralDistribution.length === 0) return 1;
    return Math.max(...d.mineralDistribution.map(m => m.count));
  });

  protected mineralPct(count: number): number {
    return Math.round((count / this.mineralMax()) * 100);
  }

  protected countryMax = computed(() => {
    const d = this.data();
    if (!d || d.originCountries.length === 0) return 1;
    return Math.max(...d.originCountries.map(c => c.count));
  });

  protected countryPct(count: number): number {
    return Math.round((count / this.countryMax()) * 100);
  }

  protected monthlyBars = computed(() => {
    const d = this.data();
    if (!d || d.monthlyBatches.length === 0) return [];
    const maxCount = Math.max(...d.monthlyBatches.map(m => m.count), 1);
    return d.monthlyBatches.map(m => ({
      month: m.month,
      count: m.count,
      label: m.month.slice(5), // e.g. "2025-03" -> "03"
      heightPct: Math.round((m.count / maxCount) * 100),
    }));
  });
}
