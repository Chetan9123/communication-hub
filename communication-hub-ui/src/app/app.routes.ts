import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { NotFoundComponent } from './components/not-found/not-found.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  
  // Auth Layout Routes
  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent) },
      { path: 'signup', loadComponent: () => import('./components/signup/signup.component').then(m => m.SignupComponent) },
    ]
  },

  // Main App Layout Routes (Protected)
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./components/adjuster-dashboard/adjuster-dashboard.component').then(m => m.AdjusterDashboardComponent) },
      { path: 'communications', loadComponent: () => import('./components/communication-hub/communication-hub.component').then(m => m.CommunicationHubComponent) },
      { path: 'claims/:id/details', loadComponent: () => import('./components/claim-details/claim-details.component').then(m => m.ClaimDetailsComponent) },
      { path: 'claims/:claimId/details/party/:partyId', loadComponent: () => import('./components/claim-details/claim-details.component').then(m => m.ClaimDetailsComponent) },
    ]
  },

  // Fallback
  { path: 'login', redirectTo: 'auth/login' },
  { path: 'signup', redirectTo: 'auth/signup' },
  { path: '404', component: NotFoundComponent },
  { path: '**', redirectTo: '404' }
];
