import { Component, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { RichTextEditorModule, ToolbarService, LinkService, ImageService, HtmlEditorService, QuickToolbarService } from '@syncfusion/ej2-angular-richtexteditor';
import { UploaderModule, SelectedEventArgs } from '@syncfusion/ej2-angular-inputs';
import { InvolvedPartyDto } from '../../api/models';
import { Api } from '../../api/api';
import { apiAttachmentsUploadPost$Json } from '../../api/fn/attachments/api-attachments-upload-post-json';
import { apiCommunicationsSendPost } from '../../api/fn/communications/api-communications-send-post';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-email-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, ButtonModule, RichTextEditorModule, UploaderModule],
  providers: [ToolbarService, LinkService, ImageService, HtmlEditorService, QuickToolbarService],
  template: `
    <ejs-dialog #emailDialog [header]="'Compose Email'" [visible]="false"
                [showCloseIcon]="true" [target]="'body'"
                [width]="'750px'" [isModal]="true" [animationSettings]="{ effect: 'Zoom' }">
      <ng-template #content>
        <div class="email-form p-4">
          <div class="regarding-banner mb-4">
            <span class="lbl">Regarding:</span> 
            <span class="val">Claim #{{ claimId }}</span>
          </div>

          <div class="flex gap-4 mb-4">
            <div class="flex-1">
              <label class="text-muted block mb-1">To</label>
              <input class="e-input" type="email" [(ngModel)]="toField">
            </div>
            <div class="flex-1">
              <label class="text-muted block mb-1">CC</label>
              <input class="e-input" type="email" [(ngModel)]="ccField">
            </div>
          </div>

          <div class="mb-4">
            <label class="text-muted block mb-1">Subject</label>
            <input class="e-input" [(ngModel)]="subject">
          </div>

          <div class="mb-4">
            <label class="text-muted block mb-1">Body</label>
            <ejs-richtexteditor #rte [(value)]="body" [toolbarSettings]="toolbarSettings" height="250px"></ejs-richtexteditor>
          </div>

          <div class="mb-2">
            <label class="text-muted block mb-1">Attachments</label>
            <ejs-uploader #uploader [autoUpload]="false" (selected)="onFileSelect($event)" 
                          [multiple]="true" [showFileList]="true"
                          [buttons]="{ browse: 'Add Files...' }"></ejs-uploader>
          </div>
        </div>
      </ng-template>

      <ng-template #footerTemplate>
        <button ejs-button [isPrimary]="false" (click)="close()">Cancel</button>
        <button ejs-button [isPrimary]="true" [disabled]="!toField || !subject || isSending" (click)="send()">
          <span *ngIf="isSending" class="e-btn-icon e-icons e-spin e-loading"></span>
          {{ isSending ? 'Sending...' : 'Send Email' }}
        </button>
      </ng-template>
    </ejs-dialog>
  `,
  styles: [`
    .block { display: block; }
    .mb-1 { margin-bottom: 4px; }
    .mb-2 { margin-bottom: 8px; }
    .mb-4 { margin-bottom: 16px; }
    .regarding-banner { background: #eff6ff; padding: 10px 14px; border-radius: 4px; font-size: 0.9rem; border: 1px solid #bfdbfe; }
    .regarding-banner .lbl { font-weight: 600; color: #1e40af; margin-right: 8px; }
    .regarding-banner .val { font-weight: 700; color: #1e3a8a; }
    .e-input { width: 100%; border-radius: var(--radius-sm); border: 1px solid #cbd5e1; padding: 8px; }
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
