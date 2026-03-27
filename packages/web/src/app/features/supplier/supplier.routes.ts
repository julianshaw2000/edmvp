import { Routes } from '@angular/router';

export const SUPPLIER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./supplier-dashboard.component').then(m => m.SupplierDashboardComponent),
  },
  {
    path: 'new-batch',
    loadComponent: () => import('../../shared/components/create-batch.component').then(m => m.SharedCreateBatchComponent),
    data: { returnRoute: '/supplier' },
  },
  {
    path: 'submit',
    loadComponent: () => import('./submit-event.component').then(m => m.SubmitEventComponent),
  },
  {
    path: 'batch/:id',
    loadComponent: () => import('./batch-detail.component').then(m => m.BatchDetailComponent),
  },
];
