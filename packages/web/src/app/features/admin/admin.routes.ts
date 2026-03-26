import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./admin-dashboard.component').then(m => m.AdminDashboardComponent),
  },
  {
    path: 'users',
    loadComponent: () => import('./user-management.component').then(m => m.UserManagementComponent),
  },
  {
    path: 'rmap',
    loadComponent: () => import('./rmap-management.component').then(m => m.RmapManagementComponent),
  },
  {
    path: 'compliance',
    loadComponent: () => import('./compliance-review.component').then(m => m.ComplianceReviewComponent),
  },
  {
    path: 'jobs',
    loadComponent: () => import('./job-monitor.component').then(m => m.JobMonitorComponent),
  },
  {
    path: 'audit-log',
    loadComponent: () => import('./audit-log.component').then(m => m.AuditLogComponent),
  },
  {
    path: 'tenants',
    loadComponent: () => import('./tenant-management.component').then(m => m.TenantManagementComponent),
  },
  {
    path: 'analytics',
    loadComponent: () => import('./analytics.component').then(m => m.AnalyticsComponent),
  },
  {
    path: 'api-keys',
    loadComponent: () => import('./api-keys.component').then(m => m.ApiKeysComponent),
  },
  {
    path: 'data-quality',
    loadComponent: () => import('./data-quality.component').then(m => m.DataQualityComponent),
  },
];
