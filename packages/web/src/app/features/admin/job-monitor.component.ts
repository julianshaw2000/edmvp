import { Component, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminApiService } from './data/admin-api.service';
import { JobResponse } from './data/admin.models';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'app-admin-job-monitor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageHeaderComponent, DatePipe],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header
      title="System Health"
      subtitle="Background job monitor — auto-refreshes every 10 s"
    />

    <!-- API health banner -->
    <div class="mb-6 flex items-center gap-3 p-4 rounded-xl border"
      [class]="apiHealthy() ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'">
      <span class="inline-block w-3 h-3 rounded-full"
        [class]="apiHealthy() ? 'bg-green-500' : 'bg-red-500'">
      </span>
      <span class="text-sm font-medium"
        [class]="apiHealthy() ? 'text-green-800' : 'text-red-800'">
        API {{ apiHealthy() ? 'Healthy' : 'Unreachable' }}
      </span>
      @if (lastRefreshed()) {
        <span class="ml-auto text-xs text-slate-500">
          Last refreshed: {{ lastRefreshed() | date:'mediumTime' }}
        </span>
      }
      <button
        (click)="refresh()"
        class="ml-2 px-3 py-1 text-xs bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors"
      >
        Refresh
      </button>
    </div>

    <!-- Jobs table -->
    <div class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
      <div class="px-6 py-4 border-b border-slate-200">
        <h2 class="text-base font-semibold text-slate-900">Recent Jobs (last 50)</h2>
      </div>

      @if (loading()) {
        <div class="px-6 py-8 text-center text-sm text-slate-400">Loading…</div>
      } @else if (error()) {
        <div class="px-6 py-8 text-center text-sm text-red-500">{{ error() }}</div>
      } @else {
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200">
            <thead class="bg-slate-50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Job Type</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Status</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Reference ID</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Created</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Completed</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Error</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-slate-100">
              @for (job of jobs(); track job.id) {
                <tr class="hover:bg-slate-50 transition-colors">
                  <td class="px-4 py-3 text-sm font-medium text-slate-800">{{ job.jobType }}</td>
                  <td class="px-4 py-3">
                    <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                      [class]="statusClass(job.status)">
                      {{ job.status }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-xs text-slate-500 font-mono">{{ job.referenceId }}</td>
                  <td class="px-4 py-3 text-sm text-slate-600">{{ job.createdAt | date:'short' }}</td>
                  <td class="px-4 py-3 text-sm text-slate-600">
                    {{ job.completedAt ? (job.completedAt | date:'short') : '—' }}
                  </td>
                  <td class="px-4 py-3 text-xs text-red-600 max-w-xs truncate">
                    {{ job.errorDetail ?? '' }}
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="6" class="px-4 py-8 text-center text-sm text-slate-400">No jobs found</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class JobMonitorComponent implements OnInit, OnDestroy {
  private api = inject(AdminApiService);

  jobs = signal<JobResponse[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  apiHealthy = signal(true);
  lastRefreshed = signal<Date | null>(null);

  private intervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit() {
    this.refresh();
    this.intervalId = setInterval(() => this.refresh(), 10_000);
  }

  ngOnDestroy() {
    if (this.intervalId) clearInterval(this.intervalId);
  }

  refresh() {
    this.loading.set(true);
    this.error.set(null);
    this.api.listJobs().subscribe({
      next: (res) => {
        this.jobs.set(res.jobs);
        this.apiHealthy.set(true);
        this.lastRefreshed.set(new Date());
        this.loading.set(false);
      },
      error: () => {
        this.apiHealthy.set(false);
        this.error.set('Failed to load jobs. API may be unavailable.');
        this.loading.set(false);
      },
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'COMPLETED': return 'bg-green-100 text-green-800';
      case 'FAILED': return 'bg-red-100 text-red-800';
      case 'RUNNING': return 'bg-blue-100 text-blue-800';
      default: return 'bg-slate-100 text-slate-700';
    }
  }
}
