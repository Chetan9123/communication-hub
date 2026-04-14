import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AttachmentDto {
  attachmentId: string;
  fileName: string;
  fileUrl: string;
  mimeType: string;
  fileSize: number;
}

export interface CommunicationMessageDto {
  communicationId: string;
  direction: string;
  timestamp: Date;
  mode: string;
  messageBody: string;
  status: string;
  isRead: boolean;
  notes: string;
  attachments: AttachmentDto[];
}

export interface CommunicationThreadDto {
  claimId: number;
  claimNumber: string;
  policyNumber: string;
  partyId: number;
  partyName: string;
  messages: CommunicationMessageDto[];
}

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
  attachments: AttachmentDto[];
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

@Injectable({ providedIn: 'root' })
export class CommunicationService {
  private readonly base = 'http://localhost:5192/api';
  private apiUrl = `${this.base}/communications`;

  constructor(private http: HttpClient) {}

  getUnreadCommunications(): Observable<UnreadCommunicationDto[]> {
    return this.http.get<UnreadCommunicationDto[]>(`${this.apiUrl}/unread`);
  }

  getCommunicationThread(claimId: number, partyId: number): Observable<CommunicationThreadDto> {
    return this.http.get<CommunicationThreadDto>(`${this.apiUrl}/claim/${claimId}/party/${partyId}`);
  }

  updateReadStatus(commId: string, isRead: boolean): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/${commId}/read-status`, { isRead });
  }

  updateNotes(commId: string, notes: string): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiUrl}/${commId}/notes`, { notes });
  }

  sendCommunication(request: SendCommunicationRequest): Observable<SendCommunicationResponse> {
    return this.http.post<SendCommunicationResponse>(`${this.apiUrl}/send`, request);
  }

  getEnabledChannels(): Observable<{ [key: string]: boolean }> {
    return this.http.get<{ [key: string]: boolean }>(`${this.apiUrl}/channels`);
  }

  uploadAttachment(file: File): Observable<{ attachmentId: string; fileName: string; s3Key: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ attachmentId: string; fileName: string; s3Key: string }>(
      `${this.base}/attachments/upload`, formData
    );
  }

  getAttachmentUrl(attachmentId: string): Observable<{ url: string; isPreSigned: boolean }> {
    return this.http.get<{ url: string; isPreSigned: boolean }>(`${this.base}/attachments/${attachmentId}/url`);
  }
}
