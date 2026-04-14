import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommunicationService, UnreadCommunicationDto } from '../../services/communication.service';
import { AttachmentViewerComponent } from '../attachment-viewer/attachment-viewer.component';

@Component({
  selector: 'app-communication-hub',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AttachmentViewerComponent],
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

  formatDate(date: Date): string {
    const now = new Date();
    const diffInHours = (now.getTime() - new Date(date).getTime()) / (1000 * 60 * 60);
    if (diffInHours < 1) return 'Just now';
    if (diffInHours < 24) return `${Math.floor(diffInHours)}h ago`;
    return new Date(date).toLocaleDateString();
  }
}