import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Api } from '../../api/api';
import { apiClaimsClaimIdGet$Json } from '../../api/fn/claims/api-claims-claim-id-get-json';
import { apiCommunicationsClaimClaimIdAllGet$Json } from '../../api/fn/communications/api-communications-claim-claim-id-all-get-json';
import { apiCommunicationsCommIdNotesPut$Json } from '../../api/fn/communications/api-communications-comm-id-notes-put-json';
import { ClaimDetailsDto, CommunicationMessageDto, CommunicationThreadDto } from '../../api/models';
import { GridModule, PageService, SortService, FilterService, ToolbarService } from '@syncfusion/ej2-angular-grids';
import { AttachmentViewerComponent } from '../attachment-viewer/attachment-viewer.component';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-claim-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AttachmentViewerComponent, GridModule],
  providers: [PageService, SortService, FilterService, ToolbarService],
  templateUrl: './claim-history.component.html',
  styleUrls: ['./claim-history.component.scss']
})
export class ClaimHistoryComponent implements OnInit {
  public claimId!: number;
  public claim: ClaimDetailsDto | null = null;
  public allMessages: CommunicationMessageDto[] = [];
  public isLoading = false;
  public historyLoading = false;
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
      const id = p.get('id');
      if (id) {
        this.claimId = Number(id);
        this.loadClaim();
        this.loadHistory();
      }
    });
  }

  loadClaim() {
    this.isLoading = true;
    this.api.invoke(apiClaimsClaimIdGet$Json, { claimId: this.claimId }).then((c: ClaimDetailsDto) => {
      this.claim = c;
      this.isLoading = false;
    });
  }

  loadHistory() {
    this.historyLoading = true;
    this.api.invoke(apiCommunicationsClaimClaimIdAllGet$Json, {
      claimId: this.claimId
    }).then((t: CommunicationThreadDto) => {
      this.allMessages = t.messages || [];
      this.historyLoading = false;
    });
  }

  get filteredMessages(): CommunicationMessageDto[] {
    if (this.activeTab === 'All') return this.allMessages;
    return this.allMessages.filter(m => m.mode === this.activeTab);
  }

  setActiveTab(tab: string) {
    this.activeTab = tab;
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

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
