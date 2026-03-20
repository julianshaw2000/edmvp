import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { ComplianceSummary } from '../data/buyer.models';

@Component({
  selector: 'app-compliance-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, StatusBadgeComponent],
  template: `
    @if (compliance(); as c) {
      <div class="space-y-4">
        <div class="flex items-center justify-between p-4 bg-slate-50 rounded-xl">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center"
              [class]="c.overallStatus === 'COMPLIANT' ? 'bg-emerald-100' : c.overallStatus === 'FLAGGED' ? 'bg-amber-100' : 'bg-slate-200'">
              <svg class="w-5 h-5"
                [class]="c.overallStatus === 'COMPLIANT' ? 'text-emerald-600' : c.overallStatus === 'FLAGGED' ? 'text-amber-600' : 'text-slate-500'"
                fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <span class="text-sm font-semibold text-slate-700">Overall Status</span>
          </div>
          <app-status-badge [status]="c.overallStatus" />
        </div>

        <div class="space-y-2">
          @for (check of c.checks; track check.framework) {
            <div class="flex items-center justify-between py-3 px-4 rounded-xl border border-slate-100 hover:bg-slate-50 transition-colors">
              <div>
                <p class="text-sm font-semibold text-slate-900">{{ check.framework }}</p>
                <p class="text-xs text-slate-400 mt-0.5">Checked {{ check.checkedAt | date:'mediumDate' }}</p>
              </div>
              <app-status-badge [status]="check.status" />
            </div>
          } @empty {
            <div class="flex flex-col items-center py-8">
              <svg class="w-8 h-8 text-slate-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
              <p class="text-sm text-slate-400">No compliance checks available</p>
            </div>
          }
        </div>
      </div>
    } @else {
      <div class="flex flex-col items-center py-8">
        <svg class="w-8 h-8 text-slate-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        <p class="text-sm text-slate-400">Compliance data not available</p>
      </div>
    }
  `,
})
export class ComplianceSummaryComponent {
  compliance = input.required<ComplianceSummary | null>();
}
