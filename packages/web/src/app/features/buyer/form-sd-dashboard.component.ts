import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormSdApiService, FilingCycle, FormSdPackageResult } from './data/form-sd-api.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

@Component({
  selector: 'app-form-sd-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/buyer" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <app-page-header
      title="Form SD Compliance"
      subtitle="Dodd-Frank §1502 — Filing cycle management and support package generation"
    />

    @if (loading()) {
      <app-loading-spinner />
    } @else {
      @if (currentCycle(); as cycle) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-semibold text-slate-900">Reporting Year {{ cycle.reportingYear }}</h2>
              <p class="text-sm text-slate-500">Due: {{ cycle.dueDate | date:'mediumDate' }}</p>
            </div>
            <app-status-badge [status]="cycle.status" />
          </div>

          <div class="flex gap-3">
            <button
              (click)="onGeneratePackage(cycle.reportingYear)"
              [disabled]="generating()"
              class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-all"
            >
              {{ generating() ? 'Generating...' : 'Generate Support Package' }}
            </button>

            @if (packageResult(); as pkg) {
              <a [href]="pkg.downloadUrl" target="_blank"
                class="inline-flex items-center gap-2 bg-emerald-50 text-emerald-700 py-2.5 px-6 rounded-xl text-sm font-semibold border border-emerald-200 hover:bg-emerald-100 transition-all">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Download Package
              </a>
            }
          </div>

          @if (generateError()) {
            <div class="mt-4 bg-rose-50 border border-rose-200 rounded-xl p-4">
              <p class="text-sm text-rose-700">{{ generateError() }}</p>
            </div>
          }
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-8 text-center mb-6">
          <p class="text-slate-500">No filing cycle found for the current year.</p>
        </div>
      }

      @if (cycles().length > 0) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div class="px-6 py-4 border-b border-slate-200">
            <h3 class="text-sm font-semibold text-slate-700">Filing Cycle History</h3>
          </div>
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-200 bg-slate-50">
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Year</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Due Date</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Status</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Filed</th>
              </tr>
            </thead>
            <tbody>
              @for (c of cycles(); track c.id) {
                <tr class="border-b border-slate-100">
                  <td class="px-6 py-3 font-medium">{{ c.reportingYear }}</td>
                  <td class="px-6 py-3 text-slate-500">{{ c.dueDate | date:'mediumDate' }}</td>
                  <td class="px-6 py-3"><app-status-badge [status]="c.status" /></td>
                  <td class="px-6 py-3 text-slate-500">{{ c.submittedAt ? (c.submittedAt | date:'mediumDate') : '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
})
export class FormSdDashboardComponent {
  private formSdApi = inject(FormSdApiService);

  protected loading = signal(true);
  protected cycles = signal<FilingCycle[]>([]);
  protected generating = signal(false);
  protected generateError = signal<string | null>(null);
  protected packageResult = signal<FormSdPackageResult | null>(null);

  protected currentCycle = computed(() => {
    const year = new Date().getFullYear();
    return this.cycles().find(c => c.reportingYear === year) ?? null;
  });

  constructor() {
    this.formSdApi.listFilingCycles().subscribe({
      next: (res) => { this.cycles.set(res.cycles); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onGeneratePackage(year: number) {
    this.generating.set(true);
    this.generateError.set(null);
    this.formSdApi.generatePackage(year).subscribe({
      next: (result) => { this.packageResult.set(result); this.generating.set(false); },
      error: (err) => { this.generateError.set(err?.error?.error ?? 'Failed to generate package'); this.generating.set(false); },
    });
  }
}
