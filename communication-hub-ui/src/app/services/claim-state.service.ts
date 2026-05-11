import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ClaimDetailsDto } from './claim.service';

@Injectable({
  providedIn: 'root'
})
export class ClaimStateService {
  private currentClaimSubject = new BehaviorSubject<ClaimDetailsDto | null>(null);
  public currentClaim$ = this.currentClaimSubject.asObservable();

  setClaim(claim: ClaimDetailsDto | null): void {
    this.currentClaimSubject.next(claim);
  }

  getClaim(): ClaimDetailsDto | null {
    return this.currentClaimSubject.getValue();
  }
}
