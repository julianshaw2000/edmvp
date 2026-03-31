import { Component, input, signal, output, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SupplierEngagement } from '../data/buyer-api.service';

@Component({
  selector: 'app-supplier-engagement-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    @if (engagement(); as data) {
      <div class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-base font-semibold text-slate-900">Supplier Engagement</h2>
          <button (click)="expanded.set(!expanded())"
            class="text-xs font-medium text-indigo-600 hover:text-indigo-700">
            {{ expanded() ? 'Collapse' : 'View suppliers' }}
          </button>
        </div>

        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center">
                <svg class="w-4 h-4 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
                  <circle cx="9" cy="7" r="4"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Total</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.totalSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-emerald-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Active</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.activeSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-amber-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Stale</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.staleSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-rose-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Flagged</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.flaggedSuppliers }}</p>
          </div>
        </div>

        @if (expanded()) {
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-200">
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Supplier</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Last Activity</th>
                  <th class="text-center px-4 py-3 font-semibold text-slate-600">Batches</th>
                  <th class="text-center px-4 py-3 font-semibold text-slate-600">Flagged</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Status</th>
                  <th class="text-center px-4 py-3 font-semibold text-slate-600">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (s of data.suppliers; track s.id) {
                  <tr class="border-b border-slate-100 last:border-0"
                    [class]="s.status === 'flagged' ? 'border-l-2 border-l-rose-400' : s.status === 'stale' ? 'border-l-2 border-l-amber-400' : ''">
                    <td class="px-4 py-3 font-medium text-slate-900">{{ s.displayName }}</td>
                    <td class="px-4 py-3 text-slate-500">
                      {{ s.lastEventDate ? (s.lastEventDate | date:'mediumDate') : 'No activity' }}
                    </td>
                    <td class="px-4 py-3 text-center text-slate-700">{{ s.batchCount }}</td>
                    <td class="px-4 py-3 text-center">
                      @if (s.flaggedBatchCount > 0) {
                        <span class="text-rose-600 font-medium">{{ s.flaggedBatchCount }}</span>
                      } @else {
                        <span class="text-slate-400">0</span>
                      }
                    </td>
                    <td class="px-4 py-3">
                      <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                        [class]="s.status === 'active' ? 'bg-emerald-50 text-emerald-700' :
                                  s.status === 'stale' ? 'bg-amber-50 text-amber-700' :
                                  s.status === 'flagged' ? 'bg-rose-50 text-rose-700' :
                                  'bg-slate-100 text-slate-600'">
                        {{ s.status === 'new' ? 'New' : s.status === 'active' ? 'Active' : s.status === 'stale' ? 'Stale' : 'Flagged' }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-center">
                      @if (s.status === 'stale' || s.status === 'flagged') {
                        <button (click)="nudgeClicked.emit(s.id); $event.stopPropagation()"
                          [disabled]="nudgingSupplier() === s.id"
                          class="inline-flex items-center gap-1 px-2.5 py-1 bg-indigo-50 text-indigo-700 rounded-lg text-xs font-medium hover:bg-indigo-100 disabled:opacity-50 transition-colors">
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                          </svg>
                          {{ nudgingSupplier() === s.id ? 'Sending...' : 'Remind' }}
                        </button>
                      }
                    </td>
                  </tr>
                }
                @if (data.suppliers.length === 0) {
                  <tr>
                    <td colspan="6" class="px-4 py-8 text-center text-slate-400">No suppliers in this tenant</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }
  `,
})
export class SupplierEngagementPanelComponent {
  engagement = input.required<SupplierEngagement | null>();
  nudgingSupplier = input<string | null>(null);
  nudgeClicked = output<string>();
  expanded = signal(false);
}
