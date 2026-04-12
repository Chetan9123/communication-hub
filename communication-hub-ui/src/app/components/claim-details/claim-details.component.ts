import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ClaimService, ClaimDetailsDto, InvolvedPartyDto } from '../../services/claim.service';
import { CommunicationComposeComponent } from '../communication-compose/communication-compose.component';

@Component({
  selector: 'app-claim-details',
  standalone: true,
  imports: [CommonModule, RouterModule, CommunicationComposeComponent],
  templateUrl: './claim-details.component.html',
  styleUrls: ['./claim-details.component.scss']
})
export class ClaimDetailsComponent implements OnInit {
  claimId: number = 0;
  claimDetails: ClaimDetailsDto | null = null;
  involvedParties: InvolvedPartyDto[] = [];
  isLoading: boolean = false;

  showComposeModal: boolean = false;
  composePartyId: number = 0;
  composeInitialMode: string = 'Email';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private claimService: ClaimService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.claimId = Number(params.get('id'));
      this.loadClaimDetails();
    });
  }

  loadClaimDetails(): void {
    this.isLoading = true;
    this.claimService.getClaimDetails(this.claimId).subscribe({
      next: (data) => {
        this.claimDetails = data;
        this.involvedParties = data.involvedParties || [];
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading claim details:', error);
        this.isLoading = false;
      }
    });
  }

  openCommunicationHub(party: InvolvedPartyDto): void {
    this.router.navigate(['/claim', this.claimId, 'party', party.partyId]);
  }

  openComposeModal(partyId: number, mode: string, event: Event): void {
    event.stopPropagation();
    this.composePartyId = partyId;
    this.composeInitialMode = mode;
    this.showComposeModal = true;
  }

  closeComposeModal(): void {
    this.showComposeModal = false;
  }

  onCommunicationSent(): void {
    this.closeComposeModal();
  }

  getRoleBadgeClass(type: string): string {
    const map: { [key: string]: string } = {
      'Policyholder': 'role-policyholder',
      'Insured':      'role-insured',
      'Claimant':     'role-claimant',
      'Witness':      'role-witness',
      'Provider':     'role-provider',
      'Adjuster':     'role-adjuster',
      'Vendor':       'role-vendor',
    };
    return map[type] || 'role-default';
  }

  getContactedCount(contacted: boolean): number {
    return this.involvedParties.filter(p => !!(p as any).contacted === contacted).length;
  }

  getCountByRole(role: string): number {
    return this.involvedParties.filter(p => p.involvedPartyType === role).length;
  }

  getPartyTypeIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'Policyholder': '👤',
      'Claimant': '🗣️',
      'Witness': '👁️',
      'Provider': '🏥',
      'Adjuster': '💼'
    };
    return icons[type] || '👤';
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}