import { Component, input, output, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BatchResponse } from '../data/buyer.models';

@Component({
  selector: 'app-buyer-batch-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StatusBadgeComponent, DatePipe, FormsModule],
  template: `
    <div class="space-y-4">
      <input
        type="text"
        [(ngModel)]="filterText"
        placeholder="Filter by batch number, origin, or mineral..."
        class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />

      <div class="overflow-x-auto rounded-lg border border-slate-200">
        <table class="min-w-full divide-y divide-slate-200">
          <thead class="bg-slate-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Batch #</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Origin</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Weight (kg)</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Status</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Compliance</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Events</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Created</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-slate-100">
            @for (batch of filteredBatches(); track batch.id) {
              <tr
                class="hover:bg-slate-50 cursor-pointer transition-colors"
                (click)="batchSelected.emit(batch.id)"
              >
                <td class="px-4 py-3 text-sm font-medium text-blue-600">{{ batch.batchNumber }}</td>
                <td class="px-4 py-3 text-sm text-slate-700">{{ batch.originMine }}, {{ batch.originCountry }}</td>
                <td class="px-4 py-3 text-sm text-slate-700">{{ batch.weightKg }}</td>
                <td class="px-4 py-3"><app-status-badge [status]="batch.status" /></td>
                <td class="px-4 py-3"><app-status-badge [status]="batch.complianceStatus" /></td>
                <td class="px-4 py-3 text-sm text-slate-700">{{ batch.eventCount }}</td>
                <td class="px-4 py-3 text-sm text-slate-500">{{ batch.createdAt | date:'mediumDate' }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="px-4 py-8 text-center text-sm text-slate-400">
                  No batches found
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class BatchTableComponent {
  batches = input.required<BatchResponse[]>();
  batchSelected = output<string>();

  filterText = signal('');

  filteredBatches = computed(() => {
    const filter = this.filterText().toLowerCase().trim();
    if (!filter) return this.batches();
    return this.batches().filter(b =>
      b.batchNumber.toLowerCase().includes(filter) ||
      b.originCountry.toLowerCase().includes(filter) ||
      b.originMine.toLowerCase().includes(filter) ||
      b.mineralType.toLowerCase().includes(filter)
    );
  });
}
