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
];
