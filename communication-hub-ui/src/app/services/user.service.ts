import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AdjusterDashboardDto {
  adjusterId: number;
  adjusterName: string;
  email: string;
  unreadCommunicationCount: number;
  assignedClaims: any[];
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = 'http://localhost:5192/api/users';

  constructor(private http: HttpClient) { }

  /**
   * Gets the adjuster dashboard information
   */
  getDashboard(): Observable<AdjusterDashboardDto> {
    return this.http.get<AdjusterDashboardDto>(`${this.apiUrl}/dashboard`);
  }
}
