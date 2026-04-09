import { Routes } from '@angular/router';
import { AdjusterDashboardComponent } from './components/adjuster-dashboard/adjuster-dashboard.component';
import { CommunicationHubComponent } from './components/communication-hub/communication-hub.component';
import { CommunicationThreadComponent } from './components/communication-thread/communication-thread.component';
import { ClaimDetailsComponent } from './components/claim-details/claim-details.component';
import { LoginComponent } from './components/login/login.component';
import { SignupComponent } from './components/signup/signup.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'dashboard', component: AdjusterDashboardComponent, canActivate: [authGuard] },
  { path: 'communications', component: CommunicationHubComponent, canActivate: [authGuard] },
  { path: 'claim/:id', component: ClaimDetailsComponent, canActivate: [authGuard] },
  { path: 'claim/:claimId/party/:partyId', component: CommunicationThreadComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'dashboard' }
];
