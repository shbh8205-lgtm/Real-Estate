import { Routes } from '@angular/router';
import { authGuard, agentGuard } from './core/auth/auth.guard';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'properties',
        pathMatch: 'full'
    },
    {
        path: 'auth',
        loadChildren: () =>
            import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
    },
    {
        path: 'properties',
        canActivate: [authGuard],
        loadChildren: () =>
            import('./features/properties/property-list/properties.routes').then(m => m.PROPERTY_ROUTES)
    },
    {
        path: 'dashboard',
        canActivate: [authGuard, agentGuard],   // רק סוכנים
        loadComponent: () =>
            import('./features/dashboard/dashboard/dashboard').then(m => m.DashboardComponent)
    },
    {
        path: 'chat',
        canActivate: [authGuard],
        loadComponent: () =>
            import('./features/chat/chat/chat').then(m => m.ChatComponent)
    },
    {
        path: '**',
        redirectTo: 'properties'
    }
];