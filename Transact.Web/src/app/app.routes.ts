import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'transactions' },
  {
    path: 'transactions',
    loadComponent: () =>
      import('./transactions/transaction-list/transaction-list.component').then(
        (m) => m.TransactionListComponent
      )
  },
  {
    path: 'transactions/new',
    loadComponent: () =>
      import('./transactions/transaction-form/transaction-form.component').then(
        (m) => m.TransactionFormComponent
      )
  },
  {
    path: 'transactions/:id/edit',
    loadComponent: () =>
      import('./transactions/transaction-form/transaction-form.component').then(
        (m) => m.TransactionFormComponent
      )
  },
  {
    path: 'health',
    loadComponent: () =>
      import('./health/health.component').then((m) => m.HealthComponent)
  },
  { path: '**', redirectTo: 'transactions' }
];
