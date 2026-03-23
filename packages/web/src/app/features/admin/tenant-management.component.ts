import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, of, BehaviorSubject } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AdminApiService } from './data/admin-api.service';
import { TenantDto, CreateTenantRequest } from './data/tenant.models';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

@Component({
  selector: 'app-tenant-management',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <app-page-header
      title="Tenant Management"
      subtitle="Create and manage platform tenants"
      actionLabel="Create Tenant"
      (actionClicked)="toggleCreateForm()"
    />

    @if (showCreateForm()) {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
        <div class="flex items-center gap-3 mb-5">
          <div class="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center">
            <svg class="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <h2 class="text-lg font-semibold text-slate-900">Create New Tenant</h2>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-5">
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Tenant Name</label>
            <input
              [(ngModel)]="newName"
              type="text"
              placeholder="Acme Mining Co."
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
            />
          </div>
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Admin Email</label>
            <input
              [(ngModel)]="newAdminEmail"
              type="email"
              placeholder="admin@example.com"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
            />
          </div>
        </div>
        @if (createError()) {
          <div class="mb-4 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
            <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <p class="text-sm text-rose-700">{{ createError() }}</p>
          </div>
        }
        <div class="flex gap-3 justify-end">
          <button
            (click)="showCreateForm.set(false)"
            class="px-4 py-2.5 text-sm font-medium text-slate-500 hover:text-slate-700 rounded-xl hover:bg-slate-100 transition-all duration-150"
          >Cancel</button>
          <button
            (click)="onCreateTenant()"
            [disabled]="creating()"
            class="px-5 py-2.5 bg-indigo-600 text-white rounded-xl text-sm font-semibold hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            @if (creating()) {
              <span class="flex items-center gap-2">
                <span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                Creating...
              </span>
            } @else {
              Create Tenant
            }
          </button>
        </div>
      </div>
    }

    @if (tenantsData() === null && !loadError()) {
      <app-loading-spinner />
    } @else if (loadError()) {
      <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
        <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-sm text-rose-700">{{ loadError() }}</p>
      </div>
    } @else {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50">
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Name</th>
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Status</th>
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Users</th>
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Batches</th>
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Created</th>
              <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody>
            @for (tenant of tenants(); track tenant.id) {
              <tr class="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                <td class="px-6 py-4 font-medium text-slate-900">{{ tenant.name }}</td>
                <td class="px-6 py-4">
                  @if (tenant.status === 'ACTIVE') {
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-700">Active</span>
                  } @else {
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-700">Suspended</span>
                  }
                </td>
                <td class="px-6 py-4 text-slate-600">{{ tenant.userCount }}</td>
                <td class="px-6 py-4 text-slate-600">{{ tenant.batchCount }}</td>
                <td class="px-6 py-4 text-slate-500">{{ tenant.createdAt | date:'mediumDate' }}</td>
                <td class="px-6 py-4">
                  @if (tenant.status === 'ACTIVE') {
                    <button
                      (click)="onToggleStatus(tenant)"
                      class="px-3 py-1.5 text-xs font-semibold text-rose-600 border border-rose-200 rounded-lg hover:bg-rose-50 transition-all duration-150"
                    >Suspend</button>
                  } @else {
                    <button
                      (click)="onToggleStatus(tenant)"
                      class="px-3 py-1.5 text-xs font-semibold text-emerald-600 border border-emerald-200 rounded-lg hover:bg-emerald-50 transition-all duration-150"
                    >Reactivate</button>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6" class="px-6 py-12 text-center text-slate-400 text-sm">No tenants found.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class TenantManagementComponent {
  private api = inject(AdminApiService);

  protected showCreateForm = signal(false);
  protected creating = signal(false);
  protected createError = signal<string | null>(null);
  protected loadError = signal<string | null>(null);
  protected newName = '';
  protected newAdminEmail = '';

  private reload$ = new BehaviorSubject<void>(undefined);

  private tenantsResult = toSignal(
    this.reload$.pipe(
      switchMap(() =>
        this.api.listTenants().pipe(
          catchError(err => {
            this.loadError.set(err?.error?.title ?? 'Failed to load tenants.');
            return of(null);
          })
        )
      )
    )
  );

  protected tenantsData = computed(() => this.tenantsResult() ?? null);
  protected tenants = computed(() => this.tenantsData()?.items ?? []);

  toggleCreateForm() {
    this.showCreateForm.update(v => !v);
    this.createError.set(null);
  }

  onCreateTenant() {
    const name = this.newName.trim();
    const adminEmail = this.newAdminEmail.trim();
    if (!name || !adminEmail) {
      this.createError.set('Name and admin email are required.');
      return;
    }
    this.creating.set(true);
    this.createError.set(null);
    const req: CreateTenantRequest = { name, adminEmail };
    this.api.createTenant(req).subscribe({
      next: () => {
        this.creating.set(false);
        this.showCreateForm.set(false);
        this.newName = '';
        this.newAdminEmail = '';
        this.reload$.next();
      },
      error: (err) => {
        this.creating.set(false);
        this.createError.set(err?.error?.title ?? 'Failed to create tenant.');
      },
    });
  }

  onToggleStatus(tenant: TenantDto) {
    const newStatus: 'ACTIVE' | 'SUSPENDED' = tenant.status === 'ACTIVE' ? 'SUSPENDED' : 'ACTIVE';
    this.api.updateTenantStatus(tenant.id, newStatus).subscribe({
      next: () => this.reload$.next(),
      error: (err) => alert(err?.error?.title ?? 'Failed to update tenant status.'),
    });
  }
}
