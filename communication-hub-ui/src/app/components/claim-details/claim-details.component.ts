import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TabModule, TabComponent } from '@syncfusion/ej2-angular-navigations';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { Api } from '../../api/api';
import { apiClaimsClaimIdGet$Json } from '../../api/fn/claims/api-claims-claim-id-get-json';
import { apiCommunicationsClaimClaimIdPartyPartyIdGet$Json } from '../../api/fn/communications/api-communications-claim-claim-id-party-party-id-get-json';
import { apiCommunicationsCommIdNotesPut$Json } from '../../api/fn/communications/api-communications-comm-id-notes-put-json';
import { apiAttachmentsAttachmentIdUrlGet } from '../../api/fn/attachments/api-attachments-attachment-id-url-get';
import { ClaimDetailsDto, InvolvedPartyDto, CommunicationMessageDto, CommunicationThreadDto } from '../../api/models';
import { ToastService } from '../../services/toast.service';
import { SmsDialogComponent } from '../sms-dialog/sms-dialog.component';
import { EmailDialogComponent } from '../email-dialog/email-dialog.component';
import { WhatsAppDialogComponent } from '../whatsapp-dialog/whatsapp-dialog.component';
import { AttachmentViewerComponent } from '../attachment-viewer/attachment-viewer.component';

@Component({
  selector: 'app-claim-details',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterModule, TabModule, ButtonModule, 
    SmsDialogComponent, EmailDialogComponent, WhatsAppDialogComponent,
    AttachmentViewerComponent
  ],
  templateUrl: './claim-details.component.html',
  styleUrls: ['./claim-details.component.scss']
})
export class ClaimDetailsComponent implements OnInit {
  @ViewChild('smsDialog') smsDialog!: SmsDialogComponent;
  @ViewChild('emailDialog') emailDialog!: EmailDialogComponent;
  @ViewChild('whatsappDialog') whatsappDialog!: WhatsAppDialogComponent;

  public viewMode: 'parties' | 'history' = 'parties';

  public claimId!: number;
  public claim: ClaimDetailsDto | null = null;
  public parties: InvolvedPartyDto[] = [];
  public messages: CommunicationMessageDto[] = [];
  public isLoading = false;
  public historyLoading = false;
  public selectedParty: InvolvedPartyDto | null = null;
  public activeTab: string = 'All';
  public tabs: string[] = ['All', 'Email', 'SMS', 'WhatsApp'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: Api,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe(p => {
      const id = p.get('id') || p.get('claimId');
      const partyId = p.get('partyId');
      
      if (id) {
        this.claimId = Number(id);
        this.loadClaim(partyId ? Number(partyId) : undefined);
      }
    });
  }

  loadClaim(partyIdToSelect?: number) {
    this.isLoading = true;
    this.api.invoke(apiClaimsClaimIdGet$Json, { claimId: this.claimId }).then((c: ClaimDetailsDto) => {
      this.claim = c;
      this.parties = c.involvedParties || [];
      this.isLoading = false;

      // Handle Deep-linking
      if (partyIdToSelect) {
        const party = this.parties.find(p => p.partyId === partyIdToSelect);
        if (party) {
          this.showHistory(party);
        }
      }
    }).catch(() => {
      this.isLoading = false;
      this.toast.error('Error', 'Could not load claim details.');
    });
  }

  backToParties() {
    this.viewMode = 'parties';
    this.selectedParty = null;
    this.messages = [];
  }

  showHistory(party: InvolvedPartyDto) {
    this.selectedParty = party;
    this.viewMode = 'history';
    this.loadHistory(party.partyId as number);
  }

  get filteredMessages(): CommunicationMessageDto[] {
    if (this.activeTab === 'All') return this.messages;
    return this.messages.filter(m => {
      if (this.activeTab === 'SMS') return m.mode === 'SMS';
      if (this.activeTab === 'Email') return m.mode === 'Email';
      if (this.activeTab === 'WhatsApp') return m.mode === 'WhatsApp';
      return true;
    });
  }

  setActiveTab(tab: string) {
    this.activeTab = tab;
  }

  loadHistory(partyId: number) {
    this.historyLoading = true;
    this.api.invoke(apiCommunicationsClaimClaimIdPartyPartyIdGet$Json, { 
      claimId: this.claimId,
      partyId: partyId
    }).then((t: CommunicationThreadDto) => {
      this.messages = t.messages || [];
      this.historyLoading = false;
    }).catch((err) => {
      console.error('Error loading history:', err);
      this.historyLoading = false;
    });
  }

  openSms(party: InvolvedPartyDto, event?: Event) {
    if (event) event.stopPropagation();
    this.selectedParty = party;
    setTimeout(() => this.smsDialog.show(), 0);
  }

  openEmail(party: InvolvedPartyDto, event?: Event) {
    if (event) event.stopPropagation();
    this.selectedParty = party;
    setTimeout(() => this.emailDialog.show(), 0);
  }

  openWhatsApp(party: InvolvedPartyDto, event?: Event) {
    if (event) event.stopPropagation();
    this.selectedParty = party;
    setTimeout(() => this.whatsappDialog.show(), 0);
  }

  onCommSent() {
    if (this.selectedParty) {
      this.loadHistory(this.selectedParty.partyId as number);
    }
  }

  updateMessageNotes(msg: CommunicationMessageDto) {
    this.api.invoke(apiCommunicationsCommIdNotesPut$Json, { 
      commId: msg.communicationId, 
      body: { notes: msg.notes } 
    }).then(() => {
      this.toast.success('Saved', 'Note updated successfully');
    }).catch(err => {
      console.error('Error updating notes:', err);
      this.toast.error('Error', 'Failed to save note');
    });
  }

  downloadAttachment(attachmentId: string) {
    this.api.invoke(apiAttachmentsAttachmentIdUrlGet, { attachmentId }).then((res: any) => {
      // Handle download redirect or direct URL
      if (res && res.url) window.open(res.url, '_blank');
    }).catch(() => {
      this.toast.error('Error', 'Failed to download attachment.');
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}