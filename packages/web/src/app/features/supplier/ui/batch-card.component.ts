import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';

@Component({
  selector: 'app-batch-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StatusBadgeComponent],
  template: `
    <div
      (click)="selected.emit()"
      class="bg-white rounded-xl border border-slate-200 p-5 hover:border-indigo-300 hover:shadow-md cursor-pointer transition-all duration-200 group"
    >
      <div class="flex items-start justify-between mb-3">
        <div>
          <h3 class="font-semibold text-slate-900 group-hover:text-indigo-600 transition-colors">{{ batch().batchNumber }}</h3>
          <p class="text-sm text-slate-500 mt-0.5">{{ batch().originMine }}, {{ batch().originCountry }}</p>
        </div>
        <app-status-badge [status]="batch().complianceStatus" />
      </div>
      <div class="flex items-center gap-4 text-sm text-slate-400 pt-3 border-t border-slate-100">
        <span class="flex items-center gap-1">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
          </svg>
          {{ batch().weightKg }} kg
        </span>
        <span class="flex items-center gap-1">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
          {{ batch().eventCount }} events
        </span>
        <span class="ml-auto">
          <app-status-badge [status]="batch().status" />
        </span>
      </div>
    </div>
  `,
})
export class BatchCardComponent {
  batch = input.required<{
    batchNumber: string; originMine: string; originCountry: string;
    complianceStatus: string; weightKg: number; eventCount: number; status: string;
  }>();
  selected = output();
}
