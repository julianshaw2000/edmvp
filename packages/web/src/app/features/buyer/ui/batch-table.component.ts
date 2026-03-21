import { Component, input, output, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BatchResponse } from '../data/buyer.models';

type ComplianceFilter = 'ALL' | 'COMPLIANT' | 'FLAGGED' | 'PENDING' | 'INSUFFICIENT_DATA';

@Component({
  selector: 'app-buyer-batch-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [StatusBadgeComponent, DatePipe, FormsModule],
  template: `
    <div>
      <!-- Filters row -->
      <div class="px-5 py-4 border-b border-slate-200">
        <div class="flex flex-wrap gap-3">
          <div class="relative flex-1 min-w-[200px]">
            <svg class="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              type="text"
              [(ngModel)]="filterText"
              placeholder="Search by batch number, origin, or mineral..."
              class="w-full pl-10 pr-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
            />
          </div>

          <select
            [(ngModel)]="complianceFilter"
            class="px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white transition-shadow"
          >
            <option value="ALL">All Compliance</option>
            <option value="COMPLIANT">Compliant</option>
            <option value="FLAGGED">Flagged</option>
            <option value="PENDING">Pending</option>
            <option value="INSUFFICIENT_DATA">Insufficient Data</option>
          </select>

          <input
            type="date"
            [(ngModel)]="dateFrom"
            title="Created from"
            class="px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
          />
          <input
            type="date"
            [(ngModel)]="dateTo"
            title="Created to"
            class="px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
          />

          @if (filterText() || complianceFilter() !== 'ALL' || dateFrom() || dateTo()) {
            <button
              (click)="clearFilters()"
              class="px-4 py-2.5 text-sm font-medium text-slate-500 hover:text-slate-700 border border-slate-300 rounded-xl hover:bg-slate-50 transition-all duration-150"
            >
              Clear
            </button>
          }
        </div>
      </div>

      <!-- Table -->
      <div class="overflow-x-auto">
        <table class="min-w-full table-zebra">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50/50">
              <th (click)="toggleSort('batchNumber')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Batch # {{ sortArrow('batchNumber') }}</th>
              <th (click)="toggleSort('originMine')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Origin {{ sortArrow('originMine') }}</th>
              <th (click)="toggleSort('weightKg')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Weight (kg) {{ sortArrow('weightKg') }}</th>
              <th (click)="toggleSort('status')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Status {{ sortArrow('status') }}</th>
              <th (click)="toggleSort('complianceStatus')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Compliance {{ sortArrow('complianceStatus') }}</th>
              <th (click)="toggleSort('eventCount')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Events {{ sortArrow('eventCount') }}</th>
              <th (click)="toggleSort('createdAt')" class="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider cursor-pointer hover:text-slate-700 select-none">Created {{ sortArrow('createdAt') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            @for (batch of sortedBatches(); track batch.id) {
              <tr
                class="hover:bg-indigo-50/50 cursor-pointer transition-colors"
                (click)="batchSelected.emit(batch.id)"
              >
                <td class="px-5 py-3.5 text-sm font-semibold text-indigo-600">{{ batch.batchNumber }}</td>
                <td class="px-5 py-3.5 text-sm text-slate-600">{{ batch.originMine }}, {{ batch.originCountry }}</td>
                <td class="px-5 py-3.5 text-sm text-slate-600 font-medium">{{ batch.weightKg }}</td>
                <td class="px-5 py-3.5"><app-status-badge [status]="batch.status" /></td>
                <td class="px-5 py-3.5"><app-status-badge [status]="batch.complianceStatus" /></td>
                <td class="px-5 py-3.5 text-sm text-slate-600">{{ batch.eventCount }}</td>
                <td class="px-5 py-3.5 text-sm text-slate-400">{{ batch.createdAt | date:'mediumDate' }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="px-5 py-12 text-center">
                  <div class="flex flex-col items-center">
                    <svg class="w-8 h-8 text-slate-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                    </svg>
                    <p class="text-sm text-slate-400">No batches found</p>
                  </div>
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
  complianceFilter = signal<ComplianceFilter>('ALL');
  dateFrom = signal('');
  dateTo = signal('');

  sortColumn = signal<string>('batchNumber');
  sortDirection = signal<'asc' | 'desc'>('asc');

  filteredBatches = computed(() => {
    const text = this.filterText().toLowerCase().trim();
    const compliance = this.complianceFilter();
    const from = this.dateFrom() ? new Date(this.dateFrom()).getTime() : null;
    const to = this.dateTo() ? new Date(this.dateTo() + 'T23:59:59').getTime() : null;

    return this.batches().filter(b => {
      if (text && !(
        b.batchNumber.toLowerCase().includes(text) ||
        b.originCountry.toLowerCase().includes(text) ||
        b.originMine.toLowerCase().includes(text) ||
        b.mineralType.toLowerCase().includes(text)
      )) return false;

      if (compliance !== 'ALL' && b.complianceStatus !== compliance) return false;

      const created = new Date(b.createdAt).getTime();
      if (from !== null && created < from) return false;
      if (to !== null && created > to) return false;

      return true;
    });
  });

  sortedBatches = computed(() => {
    const batches = [...this.filteredBatches()];
    const col = this.sortColumn();
    const dir = this.sortDirection();
    return batches.sort((a, b) => {
      const aVal = (a as any)[col] ?? '';
      const bVal = (b as any)[col] ?? '';
      const cmp = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  toggleSort(column: string) {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
  }

  sortArrow(column: string): string {
    if (this.sortColumn() !== column) return '';
    return this.sortDirection() === 'asc' ? '▲' : '▼';
  }

  clearFilters() {
    this.filterText.set('');
    this.complianceFilter.set('ALL');
    this.dateFrom.set('');
    this.dateTo.set('');
  }
}
