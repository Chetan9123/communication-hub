import { Component, Input, Output, EventEmitter, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { CheckBoxModule, ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { UploaderModule, SelectedEventArgs } from '@syncfusion/ej2-angular-inputs';
import { InvolvedPartyDto } from '../../api/models';
import { Api } from '../../api/api';
import { apiCommunicationsSendPost, apiAttachmentsUploadPost$Json } from '../../api/functions';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-whatsapp-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, CheckBoxModule, ButtonModule, UploaderModule],
  template: `
    <ejs-dialog #whatsappDialog [header]="'Send WhatsApp Message'" [visible]="false"
                [showCloseIcon]="true" [target]="'body'"
                [width]="'520px'" [isModal]="true" [animationSettings]="{ effect: 'Zoom' }"
                cssClass="comm-dialog whatsapp-theme">
      <ng-template #content>
        <div class="comm-form-container p-6">
          <div class="context-card whatsapp-card mb-6">
            <div class="card-section border-r border-green-100/50">
              <div class="label">Regarding</div>
              <div class="value font-black text-green-900">Claim #{{ claimId }}</div>
              <div class="subtitle">Property Claim</div>
            </div>
            <div class="card-section">
              <div class="label text-right">Recipient</div>
              <div class="value font-black text-gray-900 text-right">{{ party?.fullName }}</div>
              <div class="subtitle text-right">{{ party?.phone || 'No phone' }}</div>
            </div>
            <div class="card-icon absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-full p-2 shadow-sm border border-green-50">
              <span class="text-lg">📱</span>
            </div>
          </div>

          <div class="form-group mb-6">
            <div class="flex items-center justify-between mb-2">
              <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest">Message Body</label>
              <div class="text-[10px] uppercase font-black text-green-500 tracking-widest opacity-60">{{ messageBody.length }} chars</div>
            </div>
            <div class="message-input-wrapper rounded-2xl p-1 bg-gray-50 border border-gray-100 focus-within:border-green-500/50 focus-within:ring-4 focus-within:ring-green-500/5 transition-all">
              <textarea class="modern-textarea" [(ngModel)]="messageBody" rows="5" 
                        placeholder="Type your WhatsApp message..."></textarea>
            </div>
          </div>

          <div class="form-group mb-4">
            <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">Attachments</label>
            <div class="uploader-wrapper p-4 border-2 border-dashed border-gray-200 rounded-xl hover:border-green-400 transition-colors">
              <ejs-uploader #uploader [autoUpload]="false" (selected)="onFileSelect($event)" 
                            [multiple]="true" [showFileList]="true"
                            [buttons]="{ browse: 'Attach Media' }"></ejs-uploader>
            </div>
          </div>

          <div class="mt-4">
            <ejs-checkbox label="Create follow-up reminder" [(ngModel)]="createFollowUp" cssClass="whatsapp-checkbox"></ejs-checkbox>
          </div>
        </div>
      </ng-template>

      <ng-template #footerTemplate>
        <div class="p-4 border-t border-gray-50 flex justify-end gap-3 bg-gray-50/50 rounded-b-xl">
          <button ejs-button [isPrimary]="false" (click)="close()" cssClass="e-flat">Discard</button>
          <button ejs-button [isPrimary]="true" [disabled]="!messageBody || isSending" (click)="send()" class="send-btn">
            <span *ngIf="isSending" class="e-btn-icon e-icons e-spin e-loading"></span>
            {{ isSending ? 'Sending...' : 'Send WhatsApp' }}
          </button>
        </div>
      </ng-template>
    </ejs-dialog>
  `,
  styles: [`
    :host ::ng-deep .comm-dialog {
      border-radius: 20px !important;
      overflow: hidden;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25) !important;
    }
    .regarding-banner { display: none; }
    .context-card {
      display: flex;
      position: relative;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);
    }
    .whatsapp-card { background: linear-gradient(to right, #f0fdf4, #f8fafc); border-color: #dcfce7; }
    .card-section {
      flex: 1;
      padding: 16px 20px;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .context-card .label {
      font-size: 9px;
      text-transform: uppercase;
      font-weight: 800;
      letter-spacing: 0.1em;
      color: #94a3b8;
    }
    .context-card .value {
      font-size: 1.1rem;
      line-height: 1.2;
    }
    .context-card .subtitle {
      font-size: 10px;
      font-weight: 700;
      color: #94a3b8;
      text-transform: uppercase;
    }
    .message-input-wrapper { background: #f8fafc; }
    .modern-textarea {
      width: 100%;
      border-radius: 12px;
      border: none !important;
      padding: 12px;
      font-size: 0.95rem;
      background: transparent;
      outline: none !important;
      resize: vertical;
      min-height: 120px;
      color: #164e63;
    }
    .modern-textarea::placeholder { color: #94a3b8; }
    .send-btn {
      padding: 8px 24px !important;
      border-radius: 12px !important;
      font-weight: 800 !important;
      text-transform: uppercase !important;
      letter-spacing: 0.05em !important;
      background: #22c55e !important;
      border-color: #22c55e !important;
      box-shadow: 0 4px 6px -1px rgba(34, 197, 94, 0.2) !important;
    }
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
