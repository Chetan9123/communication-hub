import { Component, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { RichTextEditorModule, ToolbarService, LinkService, ImageService, HtmlEditorService, QuickToolbarService } from '@syncfusion/ej2-angular-richtexteditor';
import { UploaderModule, SelectedEventArgs } from '@syncfusion/ej2-angular-inputs';
import { InvolvedPartyDto } from '../../api/models';
import { Api } from '../../api/api';
import { apiAttachmentsUploadPost$Json, apiCommunicationsSendPost } from '../../api/functions';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-email-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, ButtonModule, RichTextEditorModule, UploaderModule],
  providers: [ToolbarService, LinkService, ImageService, HtmlEditorService, QuickToolbarService],
  template: `
    <ejs-dialog #emailDialog [header]="'Compose Email'" [visible]="false"
                [showCloseIcon]="true" [target]="'body'"
                [width]="'780px'" [isModal]="true" [animationSettings]="{ effect: 'Zoom' }"
                cssClass="comm-dialog email-theme">
      <ng-template #content>
        <div class="comm-form-container p-6">
          <div class="context-card email-card mb-6">
            <div class="card-section border-r border-indigo-100/50">
              <div class="label">Regarding</div>
              <div class="value font-black text-indigo-900">Claim #{{ claimId }}</div>
              <div class="subtitle">Property Claim</div>
            </div>
            <div class="card-section">
              <div class="label text-right">Recipient</div>
              <div class="value font-black text-gray-900 text-right">{{ party?.fullName }}</div>
              <div class="subtitle text-right">Primary Contact</div>
            </div>
            <div class="card-icon absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-full p-2 shadow-sm border border-indigo-50">
              <span class="text-lg">📧</span>
            </div>
          </div>

          <div class="flex gap-4 mb-6">
            <div class="flex-1">
              <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">To Address</label>
              <div class="input-wrapper rounded-xl p-1 bg-gray-50 border border-gray-100 focus-within:border-indigo-500/50 focus-within:ring-4 focus-within:ring-indigo-500/5 transition-all">
                <input class="modern-input" type="email" [(ngModel)]="toField" placeholder="recipient@example.com">
              </div>
            </div>
            <div class="flex-1">
              <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">CC (Optional)</label>
              <div class="input-wrapper rounded-xl p-1 bg-gray-50 border border-gray-100 focus-within:border-indigo-500/50 focus-within:ring-4 focus-within:ring-indigo-500/5 transition-all">
                <input class="modern-input" type="email" [(ngModel)]="ccField" placeholder="others@example.com">
              </div>
            </div>
          </div>

          <div class="mb-6">
            <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">Subject Line</label>
            <div class="input-wrapper rounded-xl p-1 bg-gray-50 border border-gray-100 focus-within:border-indigo-500/50 focus-within:ring-4 focus-within:ring-indigo-500/5 transition-all">
              <input class="modern-input" [(ngModel)]="subject" placeholder="Enter subject...">
            </div>
          </div>

          <div class="mb-6">
            <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">Message Content</label>
            <div class="rte-wrapper rounded-2xl border border-gray-200 overflow-hidden shadow-sm focus-within:border-indigo-400 transition-all">
              <ejs-richtexteditor #rte [(value)]="body" [toolbarSettings]="toolbarSettings" height="300px" cssClass="modern-rte"></ejs-richtexteditor>
            </div>
          </div>

          <div class="mb-2">
            <label class="text-[10px] uppercase font-black text-gray-400 tracking-widest block mb-2">Attachments</label>
            <div class="uploader-wrapper p-4 border-2 border-dashed border-gray-200 rounded-xl hover:border-indigo-400 transition-colors">
              <ejs-uploader #uploader [autoUpload]="false" (selected)="onFileSelect($event)" 
                            [multiple]="true" [showFileList]="true"
                            [buttons]="{ browse: 'Attach Files' }"></ejs-uploader>
            </div>
          </div>
        </div>
      </ng-template>

      <ng-template #footerTemplate>
        <div class="p-4 border-t border-gray-50 flex justify-end gap-3 bg-gray-50/50 rounded-b-xl">
          <button ejs-button [isPrimary]="false" (click)="close()" cssClass="e-flat">Discard</button>
          <button ejs-button [isPrimary]="true" [disabled]="!toField || !subject || isSending" (click)="send()" class="send-btn">
            <span *ngIf="isSending" class="e-btn-icon e-icons e-spin e-loading"></span>
            {{ isSending ? 'Sending...' : 'Send Email' }}
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
    .comm-form-container { background: white; }
    .context-card {
      display: flex;
      position: relative;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);
    }
    .email-card { background: linear-gradient(to right, #eef2ff, #f8fafc); border-color: #e0e7ff; }
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
    .input-wrapper { background: #f8fafc; }
    .modern-input {
      width: 100%;
      border-radius: 10px;
      border: none !important;
      padding: 10px 14px;
      font-size: 0.95rem;
      background: transparent;
      outline: none !important;
      color: #1e1b4b;
    }
    .modern-input::placeholder { color: #94a3b8; }
    .rte-wrapper { background: white; }
    :host ::ng-deep .modern-rte.e-richtexteditor {
      border: none !important;
    }
    :host ::ng-deep .modern-rte.e-richtexteditor .e-rte-content {
      border-top: 1px solid #f1f5f9 !important;
    }
    .uploader-wrapper { background: white; }
    .send-btn {
      padding: 8px 24px !important;
      border-radius: 12px !important;
      font-weight: 800 !important;
      text-transform: uppercase !important;
      letter-spacing: 0.05em !important;
      background: #6366f1 !important;
      border-color: #6366f1 !important;
      box-shadow: 0 4px 6px -1px rgba(99, 102, 241, 0.2) !important;
    }
  `]
})
export class EmailDialogComponent {
  @ViewChild('emailDialog') public emailDialog!: DialogComponent;
  @Input() party: InvolvedPartyDto | null = null;
  @Input() claimId!: number;
  @Output() sent = new EventEmitter<void>();

  public toField = '';
  public ccField = '';
  public subject = '';
  public body = '';
  public isSending = false;
  public selectedFiles: File[] = [];

  public toolbarSettings = {
    items: ['Bold', 'Italic', 'Underline', 'StrikeThrough', '|',
      'Formats', 'Alignments', 'OrderedList', 'UnorderedList', '|',
      'CreateLink', 'ClearFormat', '|', 'Undo', 'Redo']
  };

  constructor(private api: Api, private toast: ToastService) {}

  public show(): void {
    this.toField = this.party?.email || '';
    this.subject = `Regarding Claim: ${this.claimId}`;
    this.body = `<p>Dear ${this.party?.firstName || 'Party'},</p><p>I am writing to you regarding the claim mentioned in the subject.</p><p>Best regards,<br>Insurance Team</p>`;
    this.selectedFiles = [];
    this.emailDialog.show();
  }

  public close(): void {
    this.emailDialog.hide();
  }

  public onFileSelect(args: SelectedEventArgs): void {
    this.selectedFiles = args.filesData.map((f: any) => f.rawFile);
  }

  public async send(): Promise<void> {
    if (!this.toField || !this.subject) return;

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

      // 2. Send Communication
      const request = {
        claimId: this.claimId,
        partyId: this.party?.partyId as number,
        mode: 'Email',
        to: this.toField,
        cc: this.ccField,
        subject: this.subject,
        body: this.body,
        attachmentIds
      };

      const res: any = await this.api.invoke(apiCommunicationsSendPost, { body: request } as any);
      
      this.isSending = false;
      if (res && res.warningMessage) {
        this.toast.warn('Check Logs', res.warningMessage);
      } else {
        this.toast.success('Sent', 'Email has been sent successfully.');
      }
      this.sent.emit();
      this.close();
    } catch (error) {
      this.isSending = false;
      this.toast.error('Error', 'Failed to send email.');
      console.error(error);
    }
  }
}
