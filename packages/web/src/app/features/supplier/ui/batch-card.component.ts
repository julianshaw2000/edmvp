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
      class="bg-white rounded-lg border border-slate-200 p-5 hover:border-blue-300 hover:shadow-sm cursor-pointer transition-all"
    >
      <div class="flex items-start justify-between">
        <div>
          <h3 class="font-semibold text-slate-900">{{ batch().batchNumber }}</h3>
          <p class="text-sm text-slate-500 mt-1">{{ batch().originMine }}, {{ batch().originCountry }}</p>
        </div>
        <app-status-badge [status]="batch().complianceStatus" />
      </div>
      <div class="mt-3 flex items-center gap-4 text-sm text-slate-500">
        <span>{{ batch().weightKg }} kg</span>
        <span>{{ batch().eventCount }} events</span>
        <span>{{ batch().status }}</span>
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
