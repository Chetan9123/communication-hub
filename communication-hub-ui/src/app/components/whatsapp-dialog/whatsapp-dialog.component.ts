import { Component, Input, Output, EventEmitter, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { CheckBoxModule, ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { UploaderModule, SelectedEventArgs } from '@syncfusion/ej2-angular-inputs';
import { InvolvedPartyDto } from '../../api/models';
import { Api } from '../../api/api';
import { apiCommunicationsSendPost } from '../../api/fn/communications/api-communications-send-post';
import { apiAttachmentsUploadPost$Json } from '../../api/fn/attachments/api-attachments-upload-post-json';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-whatsapp-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, CheckBoxModule, ButtonModule, UploaderModule],
  template: `
    <ejs-dialog #whatsappDialog [header]="'Send WhatsApp Message'" [visible]="false"
                [showCloseIcon]="true" [target]="'body'"
                [width]="'500px'" [isModal]="true" [animationSettings]="{ effect: 'Zoom' }">
      <ng-template #content>
        <div class="whatsapp-form p-4">
          <div class="regarding-banner mb-4">
            <span class="lbl">Regarding:</span> 
            <span class="val">Claim #{{ claimId }}</span>
          </div>

          <div class="mb-4">
            <label class="text-muted block mb-1">To: Mobile Number</label>
            <div class="font-bold flex items-center justify-between">
              <span>{{ party?.phone || 'No phone number' }}</span>
              <span class="text-muted text-xs">{{ party?.fullName }}</span>
            </div>
          </div>

          <div class="mb-4">
            <label class="text-muted block mb-1">Body</label>
            <textarea class="e-input e-field" [(ngModel)]="messageBody" rows="5" 
                      placeholder="Type your WhatsApp message here..."></textarea>
          </div>

          <div class="mb-4">
            <label class="text-muted block mb-1">Attachments</label>
            <ejs-uploader #uploader [autoUpload]="false" (selected)="onFileSelect($event)" 
                          [multiple]="true" [showFileList]="true"></ejs-uploader>
          </div>

          <div class="mb-2">
            <ejs-checkbox label="Create Reminder to follow up" [(ngModel)]="createFollowUp"></ejs-checkbox>
          </div>
        </div>
      </ng-template>

      <ng-template #footerTemplate>
        <button ejs-button [isPrimary]="false" (click)="close()">Cancel</button>
        <button ejs-button [isPrimary]="true" [disabled]="!messageBody || isSending" (click)="send()">
          <span *ngIf="isSending" class="e-btn-icon e-icons e-spin e-loading"></span>
          {{ isSending ? 'Sending...' : 'Send WhatsApp' }}
        </button>
      </ng-template>
    </ejs-dialog>
  `,
  styles: [`
    .block { display: block; }
    .mb-1 { margin-bottom: 4px; }
    .mb-2 { margin-bottom: 8px; }
    .mb-4 { margin-bottom: 16px; }
    .regarding-banner { background: #f0fdf4; padding: 8px 12px; border-radius: 4px; font-size: 0.85rem; border: 1px solid #bbf7d0; }
    .regarding-banner .lbl { font-weight: 600; color: #166534; margin-right: 8px; }
    .regarding-banner .val { font-weight: 700; color: #14532d; }
    .e-field { width: 100%; border-radius: var(--radius-sm); padding: 10px; border: 1px solid #cbd5e1; }
    .font-bold { font-weight: 700; color: var(--text-main); }
  `]
})
export class WhatsAppDialogComponent implements OnInit {
  @ViewChild('whatsappDialog') public whatsappDialog!: DialogComponent;
  @Input() party: InvolvedPartyDto | null = null;
  @Input() claimId!: number;
  @Output() sent = new EventEmitter<void>();

  public messageBody: string = '';
  public createFollowUp: boolean = false;
  public isSending = false;
  public selectedFiles: File[] = [];

  constructor(private api: Api, private toast: ToastService) {}

  ngOnInit() {
    this.resetForm();
  }

  public show(): void {
    this.resetForm();
    this.whatsappDialog.show();
  }

  public close(): void {
    this.whatsappDialog.hide();
  }

  private resetForm(): void {
    this.messageBody = `Hello ${this.party?.firstName || ''}, this is regarding Claim #${this.claimId}. `;
    this.createFollowUp = false;
    this.isSending = false;
    this.selectedFiles = [];
  }

  public onFileSelect(args: SelectedEventArgs): void {
    this.selectedFiles = args.filesData.map((f: any) => f.rawFile);
  }

  public async send(): Promise<void> {
    if (!this.party || !this.messageBody) return;

    this.isSending = true;
    
    try {
      // 1. Upload Attachments
      const attachmentIds: string[] = [];
      for (const file of this.selectedFiles) {
        const uploadRes: any = await this.api.invoke(apiAttachmentsUploadPost$Json, { body: { file } as any });
        if (uploadRes && uploadRes.attachmentId) {
          attachmentIds.push(uploadRes.attachmentId);
        }
      }

      // 2. Send WhatsApp
      const request = {
        claimId: this.claimId,
        partyId: this.party.partyId as number,
        mode: 'WhatsApp',
        to: this.party.phone,
        body: this.messageBody,
        createFollowUp: this.createFollowUp,
        attachmentIds
      };

      await this.api.invoke(apiCommunicationsSendPost, { body: request } as any);
      this.isSending = false;
      this.toast.success('Sent', 'WhatsApp message has been sent successfully.');
      this.sent.emit();
      this.close();
    } catch (error) {
      this.isSending = false;
      this.toast.error('Error', 'Failed to send WhatsApp message.');
    }
  }
}
