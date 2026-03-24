import { Component, ChangeDetectionStrategy, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AdminFacade } from './admin.facade';
import { AuditLogFilters, AUDIT_ACTION_LABELS } from './data/audit-log.models';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { API_URL } from '../../core/http/api-url.token';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, JsonPipe, FormsModule, PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <div class="flex items-start justify-between mb-2">
      <app-page-header
        title="Audit Log"
        subtitle="Full record of system actions and events"
      />
      <button
        (click)="exportCsv()"
        class="mt-1 inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 transition-colors shadow-sm"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
        </svg>
        Export CSV
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-6">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div>
          <label class="block text-xs font-medium text-slate-500 mb-1.5">Action</label>
          <select
            [(ngModel)]="filterAction"
            (ngModelChange)="onFilterChange()"
            class="w-full px-3 py-2 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
          >
            <option value="">All Actions</option>
            @for (entry of actionLabelEntries; track entry.key) {
              <option [value]="entry.key">{{ entry.label }}</option>
            }
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-slate-500 mb-1.5">Entity Type</label>
          <select
            [(ngModel)]="filterEntityType"
            (ngModelChange)="onFilterChange()"
            class="w-full px-3 py-2 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
          >
            <option value="">All Types</option>
            <option value="Batch">Batch</option>
            <option value="CustodyEvent">Custody Event</option>
            <option value="Document">Document</option>
            <option value="User">User</option>
            <option value="RmapSmelter">RMAP Smelter</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-slate-500 mb-1.5">Result</label>
          <select
            [(ngModel)]="filterResult"
            (ngModelChange)="onFilterChange()"
            class="w-full px-3 py-2 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
          >
            <option value="">All Results</option>
            <option value="Success">Success</option>
            <option value="Failure">Failure</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
      @if (facade.auditLogsLoading()) {
        <div class="p-8 flex justify-center">
          <app-loading-spinner />
        </div>
      } @else if (facade.auditLogsError(); as err) {
        <div class="p-6 text-sm text-rose-600">{{ err }}</div>
      } @else {
        <table class="w-full text-sm">
          <thead class="bg-slate-50 border-b border-slate-200">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Timestamp</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">User</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Action</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Entity Type</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Result</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            @for (entry of facade.auditLogs(); track entry.id) {
              <tr
                (click)="toggleExpand(entry.id)"
                class="hover:bg-slate-50 cursor-pointer transition-colors"
              >
                <td class="px-4 py-3 text-slate-600 whitespace-nowrap">
                  {{ entry.timestamp | date:'medium' }}
                </td>
                <td class="px-4 py-3 text-slate-900 font-medium">{{ entry.userDisplayName }}</td>
                <td class="px-4 py-3 text-slate-700">
                  {{ actionLabels[entry.action] || entry.action }}
                </td>
                <td class="px-4 py-3 text-slate-600">{{ entry.entityType }}</td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold"
                    [class]="entry.result === 'Success'
                      ? 'bg-emerald-100 text-emerald-700'
                      : 'bg-rose-100 text-rose-700'"
                  >
                    {{ entry.result }}
                  </span>
                </td>
              </tr>
              @if (expandedId() === entry.id) {
                <tr class="bg-slate-50">
                  <td colspan="5" class="px-4 py-4">
                    <div class="space-y-2">
                      @if (entry.failureReason) {
                        <p class="text-sm text-rose-600">
                          <span class="font-semibold">Failure reason:</span> {{ entry.failureReason }}
                        </p>
                      }
                      @if (entry.entityId) {
                        <p class="text-xs text-slate-500">
                          <span class="font-medium">Entity ID:</span> {{ entry.entityId }}
                        </p>
                      }
                      @if (entry.ipAddress) {
                        <p class="text-xs text-slate-500">
                          <span class="font-medium">IP Address:</span> {{ entry.ipAddress }}
                        </p>
                      }
                      @if (entry.payload) {
                        <div>
                          <p class="text-xs font-medium text-slate-500 mb-1">Payload:</p>
                          <pre class="text-xs bg-white border border-slate-200 rounded-lg p-3 overflow-x-auto text-slate-700">{{ entry.payload | json }}</pre>
                        </div>
                      }
                    </div>
                  </td>
                </tr>
              }
            } @empty {
              <tr>
                <td colspan="5" class="px-4 py-10 text-center text-sm text-slate-400">
                  No audit log entries found.
                </td>
              </tr>
            }
          </tbody>
        </table>

        <!-- Pagination -->
        <div class="px-4 py-4 border-t border-slate-200 flex items-center justify-between">
          <p class="text-sm text-slate-500">
            {{ paginationSummary() }}
          </p>
          <div class="flex gap-2">
            <button
              (click)="prevPage()"
              [disabled]="facade.auditLogsPage() <= 1"
              class="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Previous
            </button>
            <button
              (click)="nextPage()"
              [disabled]="facade.auditLogsPage() >= facade.auditLogsTotalPages()"
              class="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Next
            </button>
          </div>
        </div>
      }
    </div>
  `,
})
export class AuditLogComponent implements OnInit {
  protected facade = inject(AdminFacade);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  protected readonly actionLabels = AUDIT_ACTION_LABELS;
  protected readonly actionLabelEntries = Object.entries(AUDIT_ACTION_LABELS).map(([key, label]) => ({ key, label }));

  filterAction = '';
  filterEntityType = '';
  filterResult = '';

  expandedId = signal<string | null>(null);

  protected paginationSummary = computed(() => {
    const total = this.facade.auditLogsTotalCount();
    const page = this.facade.auditLogsPage();
    const pageSize = this.facade.auditLogsPageSize();
    const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, total);
    return `Showing ${start}–${end} of ${total} entries`;
  });

  ngOnInit() {
    this.load(1);
  }

  private currentFilters(): AuditLogFilters {
    const filters: AuditLogFilters = {
      page: this.facade.auditLogsPage(),
      pageSize: this.facade.auditLogsPageSize(),
    };
    if (this.filterAction) filters.action = this.filterAction;
    if (this.filterEntityType) filters.entityType = this.filterEntityType;
    // result filter is applied client-side via the store query; backend supports action/entityType/userId/from/to
    return filters;
  }

  private load(page: number) {
    const filters: AuditLogFilters = {
      page,
      pageSize: 20,
    };
    if (this.filterAction) filters.action = this.filterAction;
    if (this.filterEntityType) filters.entityType = this.filterEntityType;
    this.facade.loadAuditLogs(filters);
  }

  onFilterChange() {
    this.expandedId.set(null);
    this.load(1);
  }

  toggleExpand(id: string) {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  prevPage() {
    const current = this.facade.auditLogsPage();
    if (current > 1) this.load(current - 1);
  }

  nextPage() {
    const current = this.facade.auditLogsPage();
    if (current < this.facade.auditLogsTotalPages()) this.load(current + 1);
  }

  exportCsv() {
    const params = new URLSearchParams();
    if (this.filterAction) params.set('action', this.filterAction);
    if (this.filterEntityType) params.set('entityType', this.filterEntityType);
    const qs = params.toString();
    const url = `${this.apiUrl}/api/admin/audit-logs/export${qs ? '?' + qs : ''}`;
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = objectUrl;
      a.download = 'audit-log.csv';
      a.click();
      URL.revokeObjectURL(objectUrl);
    });
  }
}
