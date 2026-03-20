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
];
