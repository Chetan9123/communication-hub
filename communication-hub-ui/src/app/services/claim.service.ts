import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface InvolvedPartyDto {
  partyId: number;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  email: string;
  involvedPartyType: string;
  isActive: boolean;
}

export interface ClaimDetailsDto {
  claimId: number;
  claimNumber: string;
  policyNumber: string;
  claimFiledOn: Date;
  claimClosedOn: Date;
  status: string;
  involvedParties: InvolvedPartyDto[];
}

export interface AssignedClaimDto {
  claimId: number;
  claimNumber: string;
  policyNumber: string;
  status: string;
  claimFiledOn: Date;
  unreadCommunicationCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ClaimService {
  private apiUrl = 'http://localhost:5192/api/claims';

  constructor(private http: HttpClient) { }

  /**
   * Gets claim details with involved parties
   */
  getClaimDetails(claimId: number): Observable<ClaimDetailsDto> {
    return this.http.get<ClaimDetailsDto>(`${this.apiUrl}/${claimId}`);
  }

  /**
   * Gets involved parties for a specific claim
   */
  getInvolvedParties(claimId: number): Observable<InvolvedPartyDto[]> {
    return this.http.get<InvolvedPartyDto[]>(`${this.apiUrl}/${claimId}/parties`);
  }

  /**
   * Gets all claims assigned to an adjuster
   */
  getAssignedClaims(): Observable<AssignedClaimDto[]> {
    return this.http.get<AssignedClaimDto[]>(`${this.apiUrl}/assigned-to-adjuster`);
  }
}
