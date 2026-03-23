import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { ShellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'verify/:batchId',
    loadComponent: () => import('./features/public/verify-batch.component').then(m => m.VerifyBatchComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'supplier',
        loadChildren: () => import('./features/supplier/supplier.routes').then(m => m.SUPPLIER_ROUTES),
        canActivate: [roleGuard('SUPPLIER')],
      },
      {
        path: 'buyer',
        loadChildren: () => import('./features/buyer/buyer.routes').then(m => m.BUYER_ROUTES),
        canActivate: [roleGuard('BUYER')],
      },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
        canActivate: [roleGuard('PLATFORM_ADMIN', 'TENANT_ADMIN')],
      },
    ],
  },
  {
    path: 'shared/:token',
    loadComponent: () => import('./features/shared/shared-document.component').then(m => m.SharedDocumentComponent),
  },
  {
    path: 'signup',
    loadComponent: () => import('./features/signup/signup.component').then(m => m.SignupComponent),
  },
  {
    path: 'signup/success',
    loadComponent: () => import('./features/signup/signup-success.component').then(m => m.SignupSuccessComponent),
  },
  { path: '**', loadComponent: () => import('./features/not-found.component').then(m => m.NotFoundComponent) },
];
