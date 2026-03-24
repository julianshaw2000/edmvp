import { Component, inject, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminFacade } from './admin.facade';
import { AuthService } from '../../core/auth/auth.service';
import { AdminApiService } from './data/admin-api.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink],
  template: `
    <app-page-header
      title="Admin Dashboard"
      subtitle="System overview and management"
    />

    <!-- Tenant Status Banner (TENANT_ADMIN only) -->
    @if (isTenantAdmin()) {
      @if (tenantStatus() === 'trial') {
        <div class="mb-6 flex items-center gap-3 bg-amber-50 border border-amber-200 rounded-xl px-5 py-3">
          <svg class="w-5 h-5 text-amber-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm font-medium text-amber-800 flex-1">
            Trial &mdash; {{ trialDaysRemaining() }} day{{ trialDaysRemaining() === 1 ? '' : 's' }} remaining
          </p>
          <button
            (click)="openBillingPortal()"
            [disabled]="billingLoading()"
            class="ml-auto text-xs font-semibold text-amber-700 border border-amber-300 rounded-lg px-3 py-1.5 hover:bg-amber-100 disabled:opacity-50 transition-colors"
          >
            {{ billingLoading() ? 'Redirecting...' : 'Manage Billing' }}
          </button>
        </div>
      } @else if (tenantStatus() === 'active') {
        <div class="mb-6 flex items-center gap-3 bg-emerald-50 border border-emerald-200 rounded-xl px-5 py-3">
          <svg class="w-5 h-5 text-emerald-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm font-medium text-emerald-800 flex-1">Pro Plan &mdash; Active</p>
          <button
            (click)="openBillingPortal()"
            [disabled]="billingLoading()"
            class="ml-auto text-xs font-semibold text-emerald-700 border border-emerald-300 rounded-lg px-3 py-1.5 hover:bg-emerald-100 disabled:opacity-50 transition-colors"
          >
            {{ billingLoading() ? 'Redirecting...' : 'Manage Billing' }}
          </button>
        </div>
      }
    }

    <!-- Metric Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <div class="flex items-center justify-between mb-4">
          <div class="w-10 h-10 rounded-lg bg-indigo-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Users</span>
        </div>
        @if (facade.usersLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-slate-900">{{ facade.totalUsers() }}</p>
          <p class="text-sm text-slate-500 mt-1">Total platform users</p>
        }
      </div>

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <div class="flex items-center justify-between mb-4">
          <div class="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Batches</span>
        </div>
        @if (facade.batchesLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-slate-900">{{ facade.totalBatches() }}</p>
          <p class="text-sm text-slate-500 mt-1">Tracked in system</p>
        }
      </div>

      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <div class="flex items-center justify-between mb-4">
          <div class="w-10 h-10 rounded-lg bg-amber-50 flex items-center justify-center">
            <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
          </div>
          <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Flags</span>
        </div>
        @if (facade.batchesLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-amber-600">{{ facade.totalComplianceFlags() }}</p>
          <p class="text-sm text-slate-500 mt-1">Compliance flags raised</p>
        }
      </div>
    </div>

    <!-- Quick Actions Grid -->
    <h2 class="text-lg font-semibold text-slate-900 mb-4">Quick Actions</h2>
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <a
        routerLink="/admin/users"
        class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-indigo-300 hover:shadow-md transition-all duration-200 group"
      >
        <div class="w-12 h-12 rounded-xl bg-indigo-50 flex items-center justify-center shrink-0 group-hover:bg-indigo-100 transition-colors">
          <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
          </svg>
        </div>
        <div>
          <p class="font-semibold text-slate-900 text-sm">Manage Users</p>
          <p class="text-xs text-slate-500 mt-0.5">Invite, edit roles, deactivate</p>
        </div>
      </a>

      @if (isPlatformAdmin()) {
        <a
          routerLink="/admin/tenants"
          class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-indigo-300 hover:shadow-md transition-all duration-200 group"
        >
          <div class="w-12 h-12 rounded-xl bg-indigo-50 flex items-center justify-center shrink-0 group-hover:bg-indigo-100 transition-colors">
            <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <div>
            <p class="font-semibold text-slate-900 text-sm">Tenants</p>
            <p class="text-xs text-slate-500 mt-0.5">Manage platform tenants</p>
          </div>
        </a>

        <a
          routerLink="/admin/rmap"
          class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-indigo-300 hover:shadow-md transition-all duration-200 group"
        >
          <div class="w-12 h-12 rounded-xl bg-emerald-50 flex items-center justify-center shrink-0 group-hover:bg-emerald-100 transition-colors">
            <svg class="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
            </svg>
          </div>
          <div>
            <p class="font-semibold text-slate-900 text-sm">RMAP Smelter List</p>
            <p class="text-xs text-slate-500 mt-0.5">View conformant smelters</p>
          </div>
        </a>

        <a
          routerLink="/admin/compliance"
          class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-amber-300 hover:shadow-md transition-all duration-200 group"
        >
          <div class="w-12 h-12 rounded-xl bg-amber-50 flex items-center justify-center shrink-0 group-hover:bg-amber-100 transition-colors">
            <svg class="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <div>
            <p class="font-semibold text-slate-900 text-sm">Compliance Review</p>
            <p class="text-xs text-slate-500 mt-0.5">Review flagged batches</p>
          </div>
        </a>

        <a
          routerLink="/admin/jobs"
          class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-slate-400 hover:shadow-md transition-all duration-200 group"
        >
          <div class="w-12 h-12 rounded-xl bg-slate-100 flex items-center justify-center shrink-0 group-hover:bg-slate-200 transition-colors">
            <svg class="w-6 h-6 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          <div>
            <p class="font-semibold text-slate-900 text-sm">System Health</p>
            <p class="text-xs text-slate-500 mt-0.5">Monitor background jobs</p>
          </div>
        </a>
      }

      <a
        routerLink="/admin/audit-log"
        class="flex items-center gap-4 p-5 bg-white rounded-xl border border-slate-200 shadow-sm hover:border-violet-300 hover:shadow-md transition-all duration-200 group"
      >
        <div class="w-12 h-12 rounded-xl bg-violet-50 flex items-center justify-center shrink-0 group-hover:bg-violet-100 transition-colors">
          <svg class="w-6 h-6 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
          </svg>
        </div>
        <div>
          <p class="font-semibold text-slate-900 text-sm">Audit Log</p>
          <p class="text-xs text-slate-500 mt-0.5">Browse system action history</p>
        </div>
      </a>
    </div>
  `,
})
export class AdminDashboardComponent implements OnInit {
  protected facade = inject(AdminFacade);
  protected auth = inject(AuthService);
  private adminApi = inject(AdminApiService);
  protected isPlatformAdmin = computed(() => this.auth.role() === 'PLATFORM_ADMIN');
  protected isTenantAdmin = computed(() => this.auth.role() === 'TENANT_ADMIN');
  protected tenantStatus = computed(() => this.auth.profile()?.tenantStatus?.toLowerCase() ?? null);
  protected trialDaysRemaining = computed(() => {
    const endsAt = this.auth.profile()?.trialEndsAt;
    if (!endsAt) return 0;
    const ms = new Date(endsAt).getTime() - Date.now();
    return Math.max(0, Math.ceil(ms / 86_400_000));
  });
  protected billingLoading = signal(false);

  ngOnInit() {
    this.facade.loadUsers();
    this.facade.loadBatches();
  }

  protected openBillingPortal() {
    this.billingLoading.set(true);
    this.adminApi.createBillingPortalSession().subscribe({
      next: (res) => {
        window.location.href = res.portalUrl;
      },
      error: () => {
        this.billingLoading.set(false);
      },
    });
  }
}
