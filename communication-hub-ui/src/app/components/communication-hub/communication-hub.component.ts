import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommunicationService, UnreadCommunicationDto } from '../../services/communication.service';
import { AttachmentViewerComponent } from '../attachment-viewer/attachment-viewer.component';
import { SmsDialogComponent } from '../sms-dialog/sms-dialog.component';
import { EmailDialogComponent } from '../email-dialog/email-dialog.component';
import { WhatsAppDialogComponent } from '../whatsapp-dialog/whatsapp-dialog.component';
import { ViewChild } from '@angular/core';
import { InvolvedPartyDto } from '../../api/models';

@Component({
  selector: 'app-communication-hub',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AttachmentViewerComponent, SmsDialogComponent, EmailDialogComponent, WhatsAppDialogComponent],
  templateUrl: './communication-hub.component.html',
  styleUrls: ['./communication-hub.component.scss']
})
export class CommunicationHubComponent implements OnInit {
  unreadCommunications: UnreadCommunicationDto[] = [];
  filteredCommunications: UnreadCommunicationDto[] = [];
  isLoading: boolean = false;
  searchTerm: string = '';
  modeFilter: string = '';
  isModeFilterOpen: boolean = false;

  showPreviewModal: boolean = false;
  selectedCommunication: UnreadCommunicationDto | null = null;

  @ViewChild('smsDialog') smsDialog!: SmsDialogComponent;
  @ViewChild('emailDialog') emailDialog!: EmailDialogComponent;
  @ViewChild('whatsappDialog') whatsappDialog!: WhatsAppDialogComponent;
  
  selectedParty: InvolvedPartyDto | null = null;

  modeIcons: { [key: string]: string } = {
    'Email': '📧',
    'SMS': '💬',
    'WhatsApp': '📱',
    'Text': '✉️'
  };

  communicationModes: string[] = ['Email', 'SMS', 'WhatsApp', 'Text'];

  constructor(
    private communicationService: CommunicationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadUnreadCommunications();
    setInterval(() => this.loadUnreadCommunications(), 30000);
  }

  loadUnreadCommunications(): void {
    this.isLoading = true;
    this.communicationService.getUnreadCommunications().subscribe({
      next: (data) => {
        this.unreadCommunications = data.sort(
          (a, b) => new Date(b.receivedAt).getTime() - new Date(a.receivedAt).getTime()
        );
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading unread communications:', error);
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredCommunications = this.unreadCommunications.filter(comm => {
      const matchesSearch = !this.searchTerm ||
        comm.claimNumber.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        comm.policyNumber.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        comm.senderName.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchesMode = !this.modeFilter || comm.communicationMode === this.modeFilter;
      return matchesSearch && matchesMode;
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.applyFilters();
  }

  onModeFilterChange(mode: string): void {
    this.modeFilter = mode === this.modeFilter ? '' : mode;
    this.isModeFilterOpen = false;
    this.applyFilters();
  }

  toggleModeFilter(): void {
    this.isModeFilterOpen = !this.isModeFilterOpen;
  }

  previewCommunication(communication: UnreadCommunicationDto, event: Event): void {
    event.stopPropagation();
    this.selectedCommunication = communication;
    this.showPreviewModal = true;
  }

  closePreviewModal(): void {
    this.showPreviewModal = false;
    this.selectedCommunication = null;
  }

  toggleReadStatus(communication: UnreadCommunicationDto, event: Event): void {
    event.stopPropagation();
    this.communicationService.updateReadStatus(communication.communicationId, !communication.isRead).subscribe({
      next: () => {
        communication.isRead = !communication.isRead;
        if (communication.isRead) {
          this.unreadCommunications = this.unreadCommunications.filter(
            c => c.communicationId !== communication.communicationId
          );
          this.applyFilters();
        }
      },
      error: (error) => console.error('Error updating read status:', error)
    });
  }

  openClaimThread(communication: UnreadCommunicationDto): void {
    this.closePreviewModal();
    this.communicationService.updateReadStatus(communication.communicationId, true).subscribe({
      next: () => {
        this.router.navigate(['/claims', communication.claimId, 'details', 'party', communication.partyId]);
      },
      error: (err) => {
        console.error('Error marking as read:', err);
        this.router.navigate(['/claims', communication.claimId, 'details', 'party', communication.partyId]);
      }
    });
  }

  getModeIcon(mode: string): string {
    return this.modeIcons[mode] || '📪';
  }

  getCountByMode(mode: string): number {
    return this.unreadCommunications.filter(c => c.communicationMode === mode).length;
  }

  formatDate(date: any): string {
    if (!date) return 'Unknown';
    
    // Ensure we're working with a Date object
    const dateObj = new Date(date);
    const now = new Date();
    
    // Calculate difference in hours
    const diffInMs = now.getTime() - dateObj.getTime();
    const diffInHours = diffInMs / (1000 * 60 * 60);
    
    // If the difference is negative or very small, it's "Just now"
    if (diffInHours < 0.02) return 'Just now'; // less than ~1 minute
    
    if (diffInHours < 1) {
      const mins = Math.floor(diffInMs / (1000 * 60));
      return `${mins}m ago`;
    }
    
    if (diffInHours < 24) {
      return `${Math.floor(diffInHours)}h ago`;
    }
    
    return dateObj.toLocaleDateString();
  }

  openReplyDialog(comm: UnreadCommunicationDto): void {
    // Construct a temporary party object for the dialog
    const party: InvolvedPartyDto = {
      partyId: comm.partyId,
      firstName: comm.senderName?.split(' ')[0] || '',
      lastName: comm.senderName?.split(' ').slice(1).join(' ') || '',
      phone: comm.senderPhone,
      email: comm.senderEmail,
      involvedPartyType: 'Involved Party'
    };
    this.selectedParty = party;

    // Close the preview modal so it doesn't sit on top of the reply dialog
    this.showPreviewModal = false;

    // Set inputs directly on the dialog to avoid Angular change detection timing issues
    if (comm.communicationMode === 'Email') {
      this.emailDialog.party = party;
      this.emailDialog.claimId = comm.claimId;
      this.emailDialog.show();
    } else if (comm.communicationMode === 'SMS') {
      this.smsDialog.party = party;
      this.smsDialog.claimId = comm.claimId;
      this.smsDialog.show();
    } else if (comm.communicationMode === 'WhatsApp') {
      this.whatsappDialog.party = party;
      this.whatsappDialog.claimId = comm.claimId;
      this.whatsappDialog.show();
    }
  }

  onReplySent(): void {
    if (this.selectedCommunication) {
      this.communicationService.updateReadStatus(this.selectedCommunication.communicationId, true).subscribe(() => {
        this.loadUnreadCommunications();
        this.closePreviewModal();
      });
    }
  }
}