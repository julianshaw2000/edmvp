import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AdminApiService } from './data/admin-api.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

type Tab = 'health' | 'churn' | 'revenue' | 'coaching' | 'regulatory' | 'query';

@Component({
  selector: 'app-platform-ai',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, DatePipe, DecimalPipe],
  template: `
    <app-page-header
      title="AI Insights"
      subtitle="Platform-wide intelligence powered by Claude AI"
    />

    <!-- Tab Bar -->
    <div class="flex flex-wrap gap-1 mb-6 bg-slate-100 rounded-xl p-1">
      @for (tab of tabs; track tab.id) {
        <button
          (click)="activeTab.set(tab.id)"
          [class]="activeTab() === tab.id
            ? 'flex-1 px-4 py-2 rounded-lg text-sm font-semibold bg-white text-indigo-700 shadow-sm transition-all'
            : 'flex-1 px-4 py-2 rounded-lg text-sm font-medium text-slate-600 hover:text-slate-900 transition-all'"
        >{{ tab.label }}</button>
      }
    </div>

    <!-- Tab: Tenant Health -->
    @if (activeTab() === 'health') {
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Tenant Health Scores</h2>
          <button
            (click)="loadHealth()"
            [disabled]="healthLoading()"
            class="text-sm font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 flex items-center gap-1.5"
          >
            @if (healthLoading()) { <app-loading-spinner /> }
            @else { Refresh }
          </button>
        </div>

        @if (!healthData() && !healthLoading()) {
          <div class="flex flex-col items-center py-12 text-slate-400">
            <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
            <p class="text-sm">Click Refresh to analyse tenant health</p>
          </div>
        }

        @if (healthLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Analysing tenant data...</span>
          </div>
        }

        @if (healthData(); as hd) {
          <div class="bg-indigo-50 border border-indigo-200 rounded-xl px-5 py-3 mb-4 flex items-center gap-4">
            <span class="text-sm font-medium text-indigo-900">Platform Average Health:</span>
            <span class="text-2xl font-bold text-indigo-700">{{ hd.averageHealth | number:'1.0-1' }}</span>
            <span class="text-sm text-indigo-600">/ 100</span>
          </div>

          <div class="overflow-x-auto rounded-xl border border-slate-200">
            <table class="min-w-full divide-y divide-slate-200">
              <thead class="bg-slate-50">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Tenant</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Status</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Health</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Insights</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-slate-100">
                @for (t of hd.tenants; track t.tenantId) {
                  <tr class="hover:bg-slate-50 transition-colors">
                    <td class="px-4 py-3 text-sm font-medium text-slate-900">{{ t.tenantName }}</td>
                    <td class="px-4 py-3">
                      <span [class]="statusBadge(t.status)">{{ t.status }}</span>
                    </td>
                    <td class="px-4 py-3">
                      <div class="flex items-center gap-2">
                        <div class="w-20 bg-slate-200 rounded-full h-2">
                          <div
                            class="h-2 rounded-full transition-all"
                            [class]="healthBarColor(t.color)"
                            [style.width.%]="t.healthScore"
                          ></div>
                        </div>
                        <span [class]="healthScoreText(t.color)" class="text-sm font-semibold">{{ t.healthScore }}</span>
                      </div>
                    </td>
                    <td class="px-4 py-3">
                      <div class="flex flex-wrap gap-1">
                        @for (insight of t.insights.slice(0, 3); track insight) {
                          <span class="text-xs bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full">{{ insight }}</span>
                        }
                        @if (t.insights.length > 3) {
                          <span class="text-xs text-slate-400">+{{ t.insights.length - 3 }} more</span>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }

    <!-- Tab: Churn Risk -->
    @if (activeTab() === 'churn') {
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Churn Risk Prediction</h2>
          <button
            (click)="loadChurn()"
            [disabled]="churnLoading()"
            class="text-sm font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 flex items-center gap-1.5"
          >
            @if (churnLoading()) { <app-loading-spinner /> }
            @else { Refresh }
          </button>
        </div>

        @if (!churnData() && !churnLoading()) {
          <div class="flex flex-col items-center py-12 text-slate-400">
            <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6" />
            </svg>
            <p class="text-sm">Click Refresh to run churn analysis</p>
          </div>
        }

        @if (churnLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Analysing activity patterns...</span>
          </div>
        }

        @if (churnData(); as cd) {
          <div class="grid grid-cols-3 gap-3 mb-4">
            <div class="bg-red-50 border border-red-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-red-700">{{ highRiskCount() }}</p>
              <p class="text-xs font-medium text-red-600 mt-1">HIGH RISK</p>
            </div>
            <div class="bg-amber-50 border border-amber-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-amber-700">{{ mediumRiskCount() }}</p>
              <p class="text-xs font-medium text-amber-600 mt-1">MEDIUM RISK</p>
            </div>
            <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-emerald-700">{{ lowRiskCount() }}</p>
              <p class="text-xs font-medium text-emerald-600 mt-1">LOW RISK</p>
            </div>
          </div>

          <div class="overflow-x-auto rounded-xl border border-slate-200">
            <table class="min-w-full divide-y divide-slate-200">
              <thead class="bg-slate-50">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Tenant</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Risk</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Last Activity</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Reasons</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-slate-100">
                @for (t of cd.tenants; track t.tenantId) {
                  <tr class="hover:bg-slate-50 transition-colors">
                    <td class="px-4 py-3 text-sm font-medium text-slate-900">{{ t.tenantName }}</td>
                    <td class="px-4 py-3">
                      <span [class]="riskBadge(t.riskLevel)">{{ t.riskLevel }}</span>
                    </td>
                    <td class="px-4 py-3 text-sm text-slate-500">
                      {{ t.lastActivity ? (t.lastActivity | date:'mediumDate') : 'Never' }}
                    </td>
                    <td class="px-4 py-3">
                      <ul class="space-y-0.5">
                        @for (r of t.reasons; track r) {
                          <li class="text-xs text-slate-600">&#8226; {{ r }}</li>
                        }
                      </ul>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }

    <!-- Tab: Revenue Summary -->
    @if (activeTab() === 'revenue') {
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Revenue &amp; Business Summary</h2>
          <button
            (click)="loadRevenue()"
            [disabled]="revenueLoading()"
            class="text-sm font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 flex items-center gap-1.5"
          >
            @if (revenueLoading()) { <app-loading-spinner /> }
            @else { Refresh }
          </button>
        </div>

        @if (!revenueData() && !revenueLoading()) {
          <div class="flex flex-col items-center py-12 text-slate-400">
            <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <p class="text-sm">Click Refresh to generate revenue summary</p>
          </div>
        }

        @if (revenueLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Generating executive summary...</span>
          </div>
        }

        @if (revenueData(); as rd) {
          <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
            <div class="bg-white border border-slate-200 rounded-xl p-4 text-center shadow-sm">
              <p class="text-2xl font-bold text-slate-900">&#36;{{ rd.mrr | number:'1.0-0' }}</p>
              <p class="text-xs font-medium text-slate-500 mt-1">Est. MRR</p>
            </div>
            <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-emerald-700">{{ rd.activeCount }}</p>
              <p class="text-xs font-medium text-emerald-600 mt-1">ACTIVE</p>
            </div>
            <div class="bg-amber-50 border border-amber-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-amber-700">{{ rd.trialCount }}</p>
              <p class="text-xs font-medium text-amber-600 mt-1">TRIAL</p>
            </div>
            <div class="bg-red-50 border border-red-200 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-red-700">{{ rd.suspendedCount }}</p>
              <p class="text-xs font-medium text-red-600 mt-1">SUSPENDED</p>
            </div>
          </div>

          <div class="bg-white border border-slate-200 rounded-xl p-6 shadow-sm">
            <h3 class="text-sm font-semibold text-slate-700 mb-3">AI Executive Summary</h3>
            <div class="whitespace-pre-wrap text-sm text-slate-700 leading-relaxed">{{ rd.summary }}</div>
          </div>
        }
      </div>
    }

    <!-- Tab: Usage Coaching -->
    @if (activeTab() === 'coaching') {
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Usage Coaching Alerts</h2>
          <button
            (click)="loadCoaching()"
            [disabled]="coachingLoading()"
            class="text-sm font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 flex items-center gap-1.5"
          >
            @if (coachingLoading()) { <app-loading-spinner /> }
            @else { Refresh }
          </button>
        </div>

        @if (!coachingData() && !coachingLoading()) {
          <div class="flex flex-col items-center py-12 text-slate-400">
            <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
            <p class="text-sm">Click Refresh to generate coaching suggestions</p>
          </div>
        }

        @if (coachingLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Generating coaching suggestions...</span>
          </div>
        }

        @if (coachingData(); as coa) {
          @if (coa.suggestions.length === 0) {
            <div class="flex flex-col items-center py-12 text-emerald-600">
              <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm font-medium">All tenants are on track - no coaching needed right now!</p>
            </div>
          }

          <div class="space-y-3">
            @for (s of coa.suggestions; track s.tenantId + s.category) {
              <div class="bg-white border border-slate-200 rounded-xl p-5 shadow-sm">
                <div class="flex items-start gap-4">
                  <div class="flex-1">
                    <div class="flex items-center gap-2 mb-2">
                      <span class="text-sm font-semibold text-slate-900">{{ s.tenantName }}</span>
                      <span [class]="categoryBadge(s.category)">{{ s.category }}</span>
                    </div>
                    <p class="text-sm text-slate-700 mb-2">{{ s.suggestion }}</p>
                    <p class="text-xs text-indigo-600 font-medium">Action: {{ s.suggestedAction }}</p>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>
    }

    <!-- Tab: Regulatory Updates -->
    @if (activeTab() === 'regulatory') {
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-base font-semibold text-slate-900">Regulatory Monitoring</h2>
          <button
            (click)="loadRegulatory()"
            [disabled]="regulatoryLoading()"
            class="text-sm font-medium text-indigo-600 hover:text-indigo-800 disabled:opacity-50 flex items-center gap-1.5"
          >
            @if (regulatoryLoading()) { <app-loading-spinner /> }
            @else { Refresh }
          </button>
        </div>

        @if (!regulatoryData() && !regulatoryLoading()) {
          <div class="flex flex-col items-center py-12 text-slate-400">
            <svg class="w-10 h-10 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <p class="text-sm">Click Refresh to check for regulatory updates</p>
          </div>
        }

        @if (regulatoryLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Checking regulatory frameworks...</span>
          </div>
        }

        @if (regulatoryData(); as reg) {
          <div class="bg-amber-50 border border-amber-200 rounded-xl px-5 py-3 mb-4">
            <div class="flex gap-2">
              <svg class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
              <p class="text-xs text-amber-800">{{ reg.disclaimerNote }}</p>
            </div>
          </div>

          <div class="bg-white border border-slate-200 rounded-xl p-6 shadow-sm">
            <div class="whitespace-pre-wrap text-sm text-slate-700 leading-relaxed">{{ reg.analysis }}</div>
          </div>
        }
      </div>
    }

    <!-- Tab: NL Query -->
    @if (activeTab() === 'query') {
      <div class="space-y-4">
        <h2 class="text-base font-semibold text-slate-900">Natural Language Query</h2>
        <p class="text-sm text-slate-500">Ask a question in plain English - AI will translate it to structured filters and query the database.</p>

        <div class="flex gap-3">
          <input
            type="text"
            [value]="nlQuestion()"
            (input)="nlQuestion.set($any($event.target).value)"
            (keydown.enter)="runQuery()"
            placeholder="e.g. Show me all flagged tungsten batches from DRC this year"
            class="flex-1 px-4 py-2.5 text-sm border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          />
          <button
            (click)="runQuery()"
            [disabled]="queryLoading() || !nlQuestion().trim()"
            class="px-5 py-2.5 bg-indigo-600 text-white text-sm font-semibold rounded-xl hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
          >
            @if (queryLoading()) { <app-loading-spinner /> }
            Ask
          </button>
        </div>

        @if (queryLoading()) {
          <div class="flex items-center justify-center py-12">
            <app-loading-spinner />
            <span class="ml-3 text-sm text-slate-500">Translating and querying...</span>
          </div>
        }

        @if (queryData(); as qd) {
          <div class="bg-slate-50 border border-slate-200 rounded-xl p-4">
            <p class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Parsed Filters</p>
            <div class="flex flex-wrap gap-2">
              @if (qd.parsedFilters.entityType) {
                <span class="text-xs bg-indigo-100 text-indigo-700 px-2 py-1 rounded-full">Type: {{ qd.parsedFilters.entityType }}</span>
              }
              @if (qd.parsedFilters.complianceStatus) {
                <span class="text-xs bg-amber-100 text-amber-700 px-2 py-1 rounded-full">Compliance: {{ qd.parsedFilters.complianceStatus }}</span>
              }
              @if (qd.parsedFilters.status) {
                <span class="text-xs bg-slate-200 text-slate-700 px-2 py-1 rounded-full">Status: {{ qd.parsedFilters.status }}</span>
              }
              @if (qd.parsedFilters.mineralType) {
                <span class="text-xs bg-emerald-100 text-emerald-700 px-2 py-1 rounded-full">Mineral: {{ qd.parsedFilters.mineralType }}</span>
              }
              @if (qd.parsedFilters.originCountry) {
                <span class="text-xs bg-sky-100 text-sky-700 px-2 py-1 rounded-full">Country: {{ qd.parsedFilters.originCountry }}</span>
              }
              @if (qd.parsedFilters.dateFrom) {
                <span class="text-xs bg-violet-100 text-violet-700 px-2 py-1 rounded-full">From: {{ qd.parsedFilters.dateFrom | date:'mediumDate' }}</span>
              }
              @if (qd.parsedFilters.dateTo) {
                <span class="text-xs bg-violet-100 text-violet-700 px-2 py-1 rounded-full">To: {{ qd.parsedFilters.dateTo | date:'mediumDate' }}</span>
              }
            </div>
          </div>

          <div class="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-sm">
            <div class="px-5 py-3 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <span class="text-sm font-semibold text-slate-700">Results</span>
              <span class="text-xs text-slate-500">{{ qd.results.totalCount }} record(s) - showing up to 50</span>
            </div>

            @if (qd.results.items.length === 0) {
              <div class="flex flex-col items-center py-10 text-slate-400">
                <p class="text-sm">No records matched the filters</p>
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="min-w-full divide-y divide-slate-200 text-sm">
                  <thead class="bg-slate-50">
                    <tr>
                      @for (col of queryColumns(); track col) {
                        <th class="px-4 py-2.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">{{ col }}</th>
                      }
                    </tr>
                  </thead>
                  <tbody class="bg-white divide-y divide-slate-100">
                    @for (row of qd.results.items; track $index) {
                      <tr class="hover:bg-slate-50">
                        @for (col of queryColumns(); track col) {
                          <td class="px-4 py-2.5 text-slate-700 max-w-xs truncate">{{ getField(row, col) }}</td>
                        }
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class PlatformAiComponent {
  private api = inject(AdminApiService);

  protected activeTab = signal<Tab>('health');

  protected readonly tabs: { id: Tab; label: string }[] = [
    { id: 'health', label: 'Tenant Health' },
    { id: 'churn', label: 'Churn Risk' },
    { id: 'revenue', label: 'Revenue' },
    { id: 'coaching', label: 'Coaching' },
    { id: 'regulatory', label: 'Regulatory' },
    { id: 'query', label: 'NL Query' },
  ];

  // Health
  protected healthData = signal<any>(null);
  protected healthLoading = signal(false);

  // Churn
  protected churnData = signal<any>(null);
  protected churnLoading = signal(false);
  protected highRiskCount = computed(() =>
    (this.churnData()?.tenants as any[] | undefined)?.filter((t) => t.riskLevel === 'HIGH').length ?? 0
  );
  protected mediumRiskCount = computed(() =>
    (this.churnData()?.tenants as any[] | undefined)?.filter((t) => t.riskLevel === 'MEDIUM').length ?? 0
  );
  protected lowRiskCount = computed(() =>
    (this.churnData()?.tenants as any[] | undefined)?.filter((t) => t.riskLevel === 'LOW').length ?? 0
  );

  // Revenue
  protected revenueData = signal<any>(null);
  protected revenueLoading = signal(false);

  // Coaching
  protected coachingData = signal<any>(null);
  protected coachingLoading = signal(false);

  // Regulatory
  protected regulatoryData = signal<any>(null);
  protected regulatoryLoading = signal(false);

  // NL Query
  protected nlQuestion = signal('');
  protected queryData = signal<any>(null);
  protected queryLoading = signal(false);
  protected queryColumns = computed<string[]>(() => {
    const items = this.queryData()?.results?.items as any[] | undefined;
    if (!items || items.length === 0) return [];
    return Object.keys(items[0]);
  });

  protected loadHealth() {
    this.healthLoading.set(true);
    this.api.getTenantHealth().subscribe({
      next: (data) => { this.healthData.set(data); this.healthLoading.set(false); },
      error: () => this.healthLoading.set(false),
    });
  }

  protected loadChurn() {
    this.churnLoading.set(true);
    this.api.getChurnPrediction().subscribe({
      next: (data) => { this.churnData.set(data); this.churnLoading.set(false); },
      error: () => this.churnLoading.set(false),
    });
  }

  protected loadRevenue() {
    this.revenueLoading.set(true);
    this.api.getRevenueSummary().subscribe({
      next: (data) => { this.revenueData.set(data); this.revenueLoading.set(false); },
      error: () => this.revenueLoading.set(false),
    });
  }

  protected loadCoaching() {
    this.coachingLoading.set(true);
    this.api.getUsageCoaching().subscribe({
      next: (data) => { this.coachingData.set(data); this.coachingLoading.set(false); },
      error: () => this.coachingLoading.set(false),
    });
  }

  protected loadRegulatory() {
    this.regulatoryLoading.set(true);
    this.api.getRegulatoryUpdates().subscribe({
      next: (data) => { this.regulatoryData.set(data); this.regulatoryLoading.set(false); },
      error: () => this.regulatoryLoading.set(false),
    });
  }

  protected runQuery() {
    const q = this.nlQuestion().trim();
    if (!q) return;
    this.queryLoading.set(true);
    this.api.queryNaturalLanguage(q).subscribe({
      next: (data) => { this.queryData.set(data); this.queryLoading.set(false); },
      error: () => this.queryLoading.set(false),
    });
  }

  protected getField(row: any, col: string): string {
    const val = row[col];
    if (val === null || val === undefined) return '-';
    if (typeof val === 'boolean') return val ? 'Yes' : 'No';
    if (typeof val === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(val)) {
      return new Date(val).toLocaleDateString();
    }
    return String(val);
  }

  protected statusBadge(status: string): string {
    const base = 'text-xs font-semibold px-2 py-0.5 rounded-full ';
    switch (status) {
      case 'ACTIVE': return base + 'bg-emerald-100 text-emerald-700';
      case 'TRIAL': return base + 'bg-amber-100 text-amber-700';
      case 'SUSPENDED': return base + 'bg-red-100 text-red-700';
      default: return base + 'bg-slate-100 text-slate-600';
    }
  }

  protected riskBadge(risk: string): string {
    const base = 'text-xs font-semibold px-2 py-0.5 rounded-full ';
    switch (risk) {
      case 'HIGH': return base + 'bg-red-100 text-red-700';
      case 'MEDIUM': return base + 'bg-amber-100 text-amber-700';
      case 'LOW': return base + 'bg-emerald-100 text-emerald-700';
      default: return base + 'bg-slate-100 text-slate-600';
    }
  }

  protected healthBarColor(color: string): string {
    switch (color) {
      case 'green': return 'bg-emerald-500';
      case 'amber': return 'bg-amber-500';
      case 'red': return 'bg-red-500';
      default: return 'bg-slate-400';
    }
  }

  protected healthScoreText(color: string): string {
    switch (color) {
      case 'green': return 'text-emerald-700';
      case 'amber': return 'text-amber-700';
      case 'red': return 'text-red-700';
      default: return 'text-slate-600';
    }
  }

  protected categoryBadge(category: string): string {
    const base = 'text-xs font-semibold px-2 py-0.5 rounded-full ';
    switch (category) {
      case 'Engagement': return base + 'bg-indigo-100 text-indigo-700';
      case 'Feature Adoption': return base + 'bg-sky-100 text-sky-700';
      case 'Plan Limit': return base + 'bg-amber-100 text-amber-700';
      default: return base + 'bg-slate-100 text-slate-600';
    }
  }
}
