import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
// Your existing services — endpoints unchanged
import { ClaimService, AssignedClaimDto } from '../../services/claim.service';
import { UserService, AdjusterDashboardDto } from '../../services/user.service';

@Component({
  selector: 'app-adjuster-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, DatePipe],
  templateUrl: './adjuster-dashboard.component.html',
  styleUrls: ['./adjuster-dashboard.component.scss'],
})
export class AdjusterDashboardComponent implements OnInit {
  dashboard: AdjusterDashboardDto | null = null;
  assignedClaims: AssignedClaimDto[] = [];
  isLoading = false;
  dropdownOpen = false;

  // ── Strict-mode safe getters ──────────────────────────────────────────────

  get firstName(): string {
    const name = this.dashboard?.adjusterName ?? 'Jane';
    return name.split(' ')[0] ?? name;
  }

  get userInitials(): string {
    const name = this.dashboard?.adjusterName ?? 'Jane Doe';
    return name.split(' ').map((n: string) => n[0] ?? '').join('').toUpperCase().slice(0, 2);
  }

  get totalUnread(): number {
    return this.assignedClaims.reduce((sum, c) => sum + (c.unreadCommunicationCount ?? 0), 0);
  }

  get pendingCount(): number {
    return this.assignedClaims.filter(c => (c.status ?? '').toLowerCase() === 'pending').length;
  }

  get resolvedToday(): number {
    const today = new Date().toDateString();
    return this.assignedClaims.filter(c =>
      (c.status ?? '').toLowerCase() === 'closed' &&
      new Date(c.claimFiledOn).toDateString() === today
    ).length;
  }

  // ─────────────────────────────────────────────────────────────────────────

  constructor(
    private claimService: ClaimService,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isLoading = true;

    // Existing endpoints — not changed
    this.userService.getDashboard().subscribe({
      next: (data) => { this.dashboard = data; },
      error: () => {}
    });

    this.claimService.getAssignedClaims().subscribe({
      next: (claims) => { this.assignedClaims = claims; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  toggleDropdown(): void {
    this.dropdownOpen = !this.dropdownOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu')) {
      this.dropdownOpen = false;
    }
  }

  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}