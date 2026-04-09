import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommunicationService } from '../../services/communication.service';
import { CommunicationComposeComponent } from '../communication-compose/communication-compose.component';

@Component({
  selector: 'app-communication-thread',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, CommunicationComposeComponent],
  templateUrl: './communication-thread.component.html',
  styleUrls: ['./communication-thread.component.scss']
})
export class CommunicationThreadComponent implements OnInit {
  claimId: number = 0;
  partyId: number = 0;
  thread: any = null;
  isLoading: boolean = false;
  showComposeModal: boolean = false;
  editingNotes: { [key: string]: string } = {};

  activeTab: string = 'All';
  tabs: string[] = ['All', 'Email', 'SMS', 'WhatsApp'];

  communicationModes: string[] = ['Email', 'SMS', 'WhatsApp'];
  modeIcons: { [key: string]: string } = {
    'Email': '📧',
    'SMS': '📱',
    'WhatsApp': '💬',
    'Incoming': '📥',
    'Outgoing': '📤'
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private communicationService: CommunicationService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.claimId = Number(params.get('claimId'));
      this.partyId = Number(params.get('partyId'));
      this.loadCommunicationThread();
    });
  }

  loadCommunicationThread(): void {
    this.isLoading = true;
    this.communicationService.getCommunicationThread(this.claimId, this.partyId).subscribe({
      next: (data) => {
        this.thread = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading thread:', error);
        this.isLoading = false;
      }
    });
  }

  openComposeModal(): void { this.showComposeModal = true; }
  closeComposeModal(): void { this.showComposeModal = false; }

  onCommunicationSent(): void {
    this.closeComposeModal();
    this.loadCommunicationThread();
  }

  startEditingNotes(messageId: string, currentNotes: string): void {
    this.editingNotes[messageId] = currentNotes || '';
  }

  saveNotes(messageId: string): void {
    const notes = this.editingNotes[messageId];
    this.communicationService.updateNotes(messageId, { notes }).subscribe({
      next: () => {
        delete this.editingNotes[messageId];
        const message = this.thread.messages.find((m: any) => m.communicationId === messageId);
        if (message) message.notes = notes;
      },
      error: (error) => console.error('Error saving notes:', error)
    });
  }

  cancelEditingNotes(messageId: string): void {
    delete this.editingNotes[messageId];
  }

  getModeIcon(mode: string): string {
    return this.modeIcons[mode] || '📪';
  }

  get filteredMessages(): any[] {
    if (!this.thread || !this.thread.messages) return [];
    let messages = this.thread.messages;
    if (this.activeTab !== 'All') {
      messages = messages.filter((m: any) => m.mode === this.activeTab);
    }
    return messages.sort((a: any, b: any) =>
      new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
    );
  }

  setActiveTab(tab: string): void { this.activeTab = tab; }

  goBack(): void { this.router.navigate(['/claim', this.claimId]); }
}