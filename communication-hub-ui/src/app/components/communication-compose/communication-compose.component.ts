import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommunicationService, SendCommunicationRequest } from '../../services/communication.service';
import { ClaimService, InvolvedPartyDto } from '../../services/claim.service';

@Component({
  selector: 'app-communication-compose',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './communication-compose.component.html',
  styleUrls: ['./communication-compose.component.scss']
})
export class CommunicationComposeComponent implements OnInit {
  @Input() claimId: number = 0;
  @Input() partyId: number = 0;
  @Input() initialMode: string = 'Email';
  @Output() closed = new EventEmitter<void>();
  @Output() sent = new EventEmitter<void>();

  selectedMode: string = 'Email';
  communicationModes: string[] = ['Email', 'SMS', 'WhatsApp'];
  partyInfo: InvolvedPartyDto | null = null;

  // Form Data
  toField: string = '';     // populated dynamically based on selected mode
  toEmail: string = '';     // party's email address
  toPhone: string = '';     // party's phone number
  ccEmail: string = '';
  subject: string = '';
  body: string = '';
  signature: string = 'Best regards,\nAdjuster';
  attachmentUrls: string[] = [];

  isLoading: boolean = false;
  isSending: boolean = false;
  errorMessage: string = '';

  modeIcons: { [key: string]: string } = {
    'Email': '📧',
    'SMS': '📱',
    'WhatsApp': '💬'
  };

  constructor(
    private communicationService: CommunicationService,
    private claimService: ClaimService
  ) { }

  ngOnInit(): void {
    if (this.initialMode && this.communicationModes.includes(this.initialMode)) {
      this.selectedMode = this.initialMode;
    }
    this.loadPartyInfo();
  }

  loadPartyInfo(): void {
    this.isLoading = true;
    this.claimService.getInvolvedParties(this.claimId).subscribe({
      next: (parties) => {
        this.partyInfo = parties.find(p => p.partyId === this.partyId) || null;
        if (this.partyInfo) {
          this.toEmail = this.partyInfo.email || '';
          this.toPhone = this.partyInfo.phone || '';
          // Set the initial To field based on current mode
          this.updateToField();
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading party info:', error);
        this.isLoading = false;
      }
    });
  }

  /** Switches the mode and swaps the To field accordingly */
  selectMode(mode: string): void {
    this.selectedMode = mode;
    this.errorMessage = '';
    this.updateToField();
  }

  /** Populates toField with email or phone depending on the active mode */
  private updateToField(): void {
    if (this.selectedMode === 'Email') {
      this.toField = this.toEmail;
    } else {
      // SMS / WhatsApp → use phone number
      this.toField = this.toPhone;
    }
  }

  sendCommunication(): void {
    if (!this.validateForm()) {
      return;
    }

    this.isSending = true;
    const request: SendCommunicationRequest = {
      claimId: this.claimId,
      partyId: this.partyId,
      mode: this.selectedMode,
      to: this.toField,     // always the correct contact for the active mode
      cc: this.ccEmail,
      subject: this.subject,
      body: this.body,
      signature: this.signature,
      attachmentUrls: this.attachmentUrls
    };

    this.communicationService.sendCommunication(request).subscribe({
      next: (response) => {
        this.isSending = false;
        if (response.warningMessage) {
          alert('Note: ' + response.warningMessage);
        }
        this.sent.emit();
        this.closeModal();
      },
      error: (error) => {
        this.isSending = false;
        this.errorMessage = error.error?.message || 'Error sending communication';
        console.error('Error sending communication:', error);
      }
    });
  }

  validateForm(): boolean {
    if (!this.toField) {
      this.errorMessage = this.selectedMode === 'Email'
        ? 'Recipient email is required'
        : 'Recipient phone number is required';
      return false;
    }

    if (this.selectedMode === 'Email' && !this.subject) {
      this.errorMessage = 'Email subject is required';
      return false;
    }

    if (!this.body) {
      this.errorMessage = 'Message body is required';
      return false;
    }

    return true;
  }

  addAttachment(fileUrl: string): void {
    if (fileUrl && !this.attachmentUrls.includes(fileUrl)) {
      this.attachmentUrls.push(fileUrl);
    }
  }

  removeAttachment(index: number): void {
    this.attachmentUrls.splice(index, 1);
  }

  closeModal(): void {
    this.closed.emit();
  }

  get isEmailMode(): boolean {
    return this.selectedMode === 'Email';
  }

  get isSmsMode(): boolean {
    return this.selectedMode === 'SMS' || this.selectedMode === 'WhatsApp';
  }
}
