import { Routes } from '@angular/router';

export const BUYER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./buyer-dashboard.component').then(m => m.BuyerDashboardComponent),
  },
  {
    path: 'batch/:id',
    loadComponent: () => import('./batch-detail.component').then(m => m.BuyerBatchDetailComponent),
  },
  {
    path: 'form-sd',
    loadComponent: () => import('./form-sd-dashboard.component').then(m => m.FormSdDashboardComponent),
  },
  {
    path: 'cmrt-import',
    loadComponent: () => import('./cmrt-import.component').then(m => m.CmrtImportComponent),
  },
];
