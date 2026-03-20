import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminFacade } from './admin.facade';
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

    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <!-- Total Users -->
      <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
        <p class="text-sm font-medium text-slate-500 uppercase tracking-wider mb-2">Total Users</p>
        @if (facade.usersLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-slate-900">{{ facade.totalUsers() }}</p>
        }
      </div>

      <!-- Total Batches -->
      <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
        <p class="text-sm font-medium text-slate-500 uppercase tracking-wider mb-2">Total Batches</p>
        @if (facade.batchesLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-slate-900">{{ facade.totalBatches() }}</p>
        }
      </div>

      <!-- Compliance Flags -->
      <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
        <p class="text-sm font-medium text-slate-500 uppercase tracking-wider mb-2">Compliance Flags</p>
        @if (facade.batchesLoading()) {
          <app-loading-spinner />
        } @else {
          <p class="text-4xl font-bold text-amber-600">{{ facade.totalComplianceFlags() }}</p>
        }
      </div>
    </div>

    <!-- Quick links -->
    <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
      <h2 class="text-lg font-semibold text-slate-900 mb-4">Quick Links</h2>
      <div class="flex flex-wrap gap-4">
        <a
          routerLink="/admin/users"
          class="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          Manage Users
        </a>
        <a
          routerLink="/admin/rmap"
          class="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          RMAP Smelter List
        </a>
        <a
          routerLink="/admin/compliance"
          class="bg-amber-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-amber-700 transition-colors"
        >
          Compliance Review
        </a>
      </div>
    </div>
  `,
})
export class AdminDashboardComponent implements OnInit {
  protected facade = inject(AdminFacade);

  ngOnInit() {
    this.facade.loadUsers();
    this.facade.loadBatches();
  }
}
