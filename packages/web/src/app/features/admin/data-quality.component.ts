import { Component, inject, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { AdminApiService } from './data/admin-api.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

interface BatchCompleteness {
  id: string;
  batchNumber: string;
  mineralType: string;
  score: number;
  missingFields: string[];
}

interface DataCompletenessResponse {
  batches: BatchCompleteness[];
  averageScore: number;
}

@Component({
  selector: 'app-data-quality',
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
      title="Data Quality"
      subtitle="Completeness scoring for all batches in your account"
    />

    @if (loading()) {
      <div class="flex justify-center py-16">
        <app-loading-spinner />
      </div>
    } @else if (error()) {
      <div class="bg-red-50 border border-red-200 rounded-xl p-6 text-center text-sm text-red-700">
        Failed to load data quality scores. Please try again.
      </div>
    } @else if (data(); as d) {

      <!-- Average Score Card -->
      <div class="mb-6">
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 flex items-center gap-5 max-w-sm">
          <div
            class="w-16 h-16 rounded-full flex items-center justify-center text-xl font-bold shrink-0"
            [class]="scoreRingClass(d.averageScore)"
          >
            {{ d.averageScore }}
          </div>
          <div>
            <p class="text-sm font-semibold text-slate-700">Average Completeness</p>
            <p class="text-xs text-slate-500 mt-0.5">Across {{ d.batches.length }} batch{{ d.batches.length !== 1 ? 'es' : '' }}</p>
            <div class="mt-2 h-2 w-36 bg-slate-100 rounded-full overflow-hidden">
              <div
                class="h-full rounded-full transition-all duration-500"
                [class]="scoreBarClass(d.averageScore)"
                [style.width.%]="d.averageScore"
              ></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Batch Table -->
      @if (d.batches.length === 0) {
        <div class="bg-white rounded-xl border border-slate-200 p-12 text-center">
          <p class="text-slate-400 text-sm">No batches found in your account.</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-100 bg-slate-50">
                <th class="text-left px-5 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wider">Batch</th>
                <th class="text-left px-5 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wider">Mineral</th>
                <th class="text-left px-5 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wider">Score</th>
                <th class="text-left px-5 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wider hidden md:table-cell">Missing Items</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              @for (batch of d.batches; track batch.id) {
                <tr class="hover:bg-slate-50 transition-colors">
                  <td class="px-5 py-3.5 font-mono text-xs font-semibold text-slate-700">{{ batch.batchNumber }}</td>
                  <td class="px-5 py-3.5 text-slate-600">{{ batch.mineralType }}</td>
                  <td class="px-5 py-3.5">
                    <div class="flex items-center gap-2">
                      <div class="w-16 h-2 bg-slate-100 rounded-full overflow-hidden">
                        <div
                          class="h-full rounded-full transition-all duration-500"
                          [class]="scoreBarClass(batch.score)"
                          [style.width.%]="batch.score"
                        ></div>
                      </div>
                      <span
                        class="text-xs font-bold w-8"
                        [class]="scoreLabelClass(batch.score)"
                      >{{ batch.score }}</span>
                    </div>
                  </td>
                  <td class="px-5 py-3.5 hidden md:table-cell">
                    @if (batch.missingFields.length === 0) {
                      <span class="inline-flex items-center gap-1 text-xs text-emerald-600 font-medium">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                        </svg>
                        Complete
                      </span>
                    } @else {
                      <ul class="space-y-0.5">
                        @for (item of batch.missingFields; track item) {
                          <li class="text-xs text-slate-500 flex items-start gap-1">
                            <span class="text-amber-400 mt-0.5 shrink-0">&#x25CF;</span>
                            {{ item }}
                          </li>
                        }
                      </ul>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
})
export class DataQualityComponent {
  private adminApi = inject(AdminApiService);

  private raw = toSignal(
    this.adminApi.getDataCompleteness().pipe(catchError(() => of(null))),
    { initialValue: undefined }
  );

  protected loading = computed(() => this.raw() === undefined);
  protected error = computed(() => this.raw() === null);
  protected data = computed(() => {
    const v = this.raw();
    return v === null || v === undefined ? null : (v as DataCompletenessResponse);
  });

  protected scoreRingClass(score: number): string {
    if (score > 80) return 'bg-emerald-50 text-emerald-700 ring-2 ring-emerald-400';
    if (score >= 50) return 'bg-amber-50 text-amber-700 ring-2 ring-amber-400';
    return 'bg-red-50 text-red-700 ring-2 ring-red-400';
  }

  protected scoreBarClass(score: number): string {
    if (score > 80) return 'bg-emerald-500';
    if (score >= 50) return 'bg-amber-400';
    return 'bg-red-500';
  }

  protected scoreLabelClass(score: number): string {
    if (score > 80) return 'text-emerald-600';
    if (score >= 50) return 'text-amber-600';
    return 'text-red-600';
  }
}
