import { Routes } from '@angular/router';
import { agentGuard } from '../../../core/auth/auth.guard';

export const PROPERTY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./property-list').then(m => m.PropertyListComponent)
  },
  {
    path: 'new',
    canActivate: [agentGuard],
    loadComponent: () =>
      import('../property-create/property-create').then(m => m.PropertyCreateComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('../property-detail/property-detail').then(m => m.PropertyDetailComponent)
  }
];
