import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimService, AssignedClaimDto } from '../../services/claim.service';
import { UserService, AdjusterDashboardDto } from '../../services/user.service';

@Component({
  selector: 'app-adjuster-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './adjuster-dashboard.component.html',
  styleUrls: ['./adjuster-dashboard.component.scss']
})
export class AdjusterDashboardComponent implements OnInit {
  dashboard: AdjusterDashboardDto | null = null;
  assignedClaims: AssignedClaimDto[] = [];
  filteredClaims: AssignedClaimDto[] = [];
  searchTerm: string = '';
  statusFilter: string = '';
  isLoading: boolean = false;
  statuses: string[] = ['Open', 'In Progress', 'Closed', 'Pending'];

  // Syncfusion Grid Configuration
  gridFields: any[] = [
    { text: 'Claim Number', value: 'claimNumber' },
    { text: 'Policy Number', value: 'policyNumber' },
    { text: 'Status', value: 'status' },
    { text: 'Filed Date', value: 'claimFiledOn' },
    { text: 'Unread Messages', value: 'unreadCommunicationCount' }
  ];

  constructor(
    private claimService: ClaimService,
    private userService: UserService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;

    // Load dashboard info
    this.userService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard:', error);
        this.isLoading = false;
      }
    });

    // Load assigned claims
    this.claimService.getAssignedClaims().subscribe({
      next: (claims) => {
        this.assignedClaims = claims;
        this.applyFilters();
      },
      error: (error) => {
        console.error('Error loading claims:', error);
      }
    });
  }

  applyFilters(): void {
    this.filteredClaims = this.assignedClaims.filter(claim => {
      const matchesSearch = !this.searchTerm ||
        claim.claimNumber.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        claim.policyNumber.toLowerCase().includes(this.searchTerm.toLowerCase());

      const matchesStatus = !this.statusFilter || claim.status === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.applyFilters();
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter = value;
    this.applyFilters();
  }

  navigateToCommunicationHub(): void {
    this.router.navigate(['/communications']);
  }

  navigateToClaimDetails(claimId: number): void {
    this.router.navigate(['/claim', claimId]);
  }

  openCommunicationHub(claimId: number, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/claim', claimId]);
  }
}
