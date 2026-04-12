import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UnreadCommunicationDto {
  communicationId: string;
  claimId: number;
  claimNumber: string;
  policyNumber: string;
  partyId: number;
  senderName: string;
  communicationMode: string;
  messagePreview: string;
  receivedAt: Date;
  isRead: boolean;
  status: string;
}

export interface UpdateReadStatusRequest {
  isRead: boolean;
}

export interface SendCommunicationRequest {
  claimId: number;
  partyId: number;
  mode: string;
  to: string;
  cc?: string;
  subject?: string;
  body: string;
  signature?: string;
  attachmentUrls?: string[];
  attachmentIds?: string[];
}

export interface SendCommunicationResponse {
  communicationId: string;
  warningMessage?: string;
}

export interface UpdateNotesRequest {
  notes: string;
}

@Injectable({
  providedIn: 'root'
})
export class CommunicationService {
  private apiUrl = 'http://localhost:5192/api/communications';

  constructor(private http: HttpClient) { }


  getUnreadCommunications(): Observable<UnreadCommunicationDto[]> {
    return this.http.get<UnreadCommunicationDto[]>(`${this.apiUrl}/unread`);
  }

  /**
   * Updates the read status of a communication
   */
  updateReadStatus(commId: string, request: UpdateReadStatusRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/${commId}/read-status`, request);
  }

  /**
   * Gets the communication thread for a specific claim and party
   */
  getCommunicationThread(claimId: number, partyId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/claim/${claimId}/party/${partyId}`);
  }

  /**
   * Updates the notes for a communication
   */
  updateNotes(commId: string, request: UpdateNotesRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/${commId}/notes`, request);
  }

  /**
   * Sends a new communication
   */
  sendCommunication(request: SendCommunicationRequest): Observable<SendCommunicationResponse> {
    return this.http.post<SendCommunicationResponse>(`${this.apiUrl}/send`, request);
  }

  /**
   * Gets all enabled communication channels
   */
  getEnabledChannels(): Observable<{ [key: string]: boolean }> {
    return this.http.get<{ [key: string]: boolean }>(`${this.apiUrl}/channels`);
  }

  /**
   * Uploads an attachment to S3
   */
  uploadAttachment(file: File): Observable<{ attachmentId: string, fileName: string, s3Key: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ attachmentId: string, fileName: string, s3Key: string }>(
      'http://localhost:5192/api/attachments/upload', 
      formData
    );
  }

  /**
   * Gets a pre-signed or direct URL to download an attachment
   */
  getAttachmentUrl(attachmentId: string): Observable<{ url: string, isPreSigned: boolean }> {
    return this.http.get<{ url: string, isPreSigned: boolean }>(`http://localhost:5192/api/attachments/${attachmentId}/url`);
  }
}
