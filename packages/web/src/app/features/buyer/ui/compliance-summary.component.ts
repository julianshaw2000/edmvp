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
      <div class="space-y-3">
        <div class="flex items-center justify-between mb-2">
          <span class="text-sm font-medium text-slate-700">Overall Status</span>
          <app-status-badge [status]="c.overallStatus" />
        </div>

        <div class="space-y-2">
          @for (check of c.checks; track check.framework) {
            <div class="flex items-center justify-between py-2 px-3 bg-slate-50 rounded-lg">
              <div>
                <p class="text-sm font-medium text-slate-900">{{ check.framework }}</p>
                <p class="text-xs text-slate-500">Checked {{ check.checkedAt | date:'mediumDate' }}</p>
              </div>
              <app-status-badge [status]="check.status" />
            </div>
          } @empty {
            <p class="text-sm text-slate-400 text-center py-4">No compliance checks available</p>
          }
        </div>
      </div>
    } @else {
      <p class="text-sm text-slate-400 text-center py-4">Compliance data not available</p>
    }
  `,
})
export class ComplianceSummaryComponent {
  compliance = input.required<ComplianceSummary | null>();
}
