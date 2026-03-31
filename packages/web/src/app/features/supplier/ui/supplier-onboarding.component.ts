import { Component, inject, computed, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BatchFacade } from '../../../shared/state/batch.facade';

@Component({
  selector: 'app-supplier-onboarding',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (!dismissed() && !allComplete()) {
      <div class="mb-6 bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-5 sm:p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-lg bg-indigo-50 flex items-center justify-center">
                <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
                </svg>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-slate-900">Getting Started</h3>
                <p class="text-xs text-slate-500">{{ completedCount() }}/3 steps complete</p>
              </div>
            </div>
            <button (click)="dismiss()" class="text-slate-400 hover:text-slate-600 p-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>
          <div class="w-full h-1.5 bg-slate-100 rounded-full mb-5">
            <div class="h-1.5 bg-indigo-600 rounded-full transition-all duration-500"
              [style.width.%]="(completedCount() / 3) * 100"></div>
          </div>
          <div class="space-y-3">
            <a routerLink="/supplier/batches/new"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step1Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step1Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step1Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Create your first batch</p>
                <p class="text-xs text-slate-400">Register a mineral batch in the supply chain</p>
              </div>
              @if (!step1Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </a>
            <a routerLink="/supplier/submit"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step2Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step2Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step2Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Submit a custody event</p>
                <p class="text-xs text-slate-400">Record an event in the batch lifecycle</p>
              </div>
              @if (!step2Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </a>
            <div (click)="onViewCompliance()"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group cursor-pointer">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step3Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step3Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step3Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Review compliance status</p>
                <p class="text-xs text-slate-400">Check your batch compliance results</p>
              </div>
              @if (!step3Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class SupplierOnboardingComponent {
  private facade = inject(BatchFacade);
  private router = inject(Router);

  private readonly DISMISSED_KEY = 'auditraks_supplier_onboarding_dismissed';
  private readonly VIEWED_KEY = 'auditraks_supplier_viewed_compliance';

  dismissed = signal(localStorage.getItem(this.DISMISSED_KEY) === 'true');

  step1Complete = computed(() => this.facade.batches().length > 0);
  step2Complete = computed(() => this.facade.batches().some(b => b.eventCount > 0));
  step3Complete = signal(localStorage.getItem(this.VIEWED_KEY) === 'true');

  completedCount = computed(() =>
    [this.step1Complete(), this.step2Complete(), this.step3Complete()]
      .filter(Boolean).length
  );
  allComplete = computed(() => this.completedCount() === 3);

  dismiss() {
    localStorage.setItem(this.DISMISSED_KEY, 'true');
    this.dismissed.set(true);
  }

  onViewCompliance() {
    const batches = this.facade.batches();
    if (batches.length > 0) {
      localStorage.setItem(this.VIEWED_KEY, 'true');
      this.step3Complete.set(true);
      this.router.navigate(['/supplier/batch', batches[0].id]);
    }
  }
}
