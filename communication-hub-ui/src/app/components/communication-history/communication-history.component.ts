import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommunicationMessageDto } from '../../api/models';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { AttachmentViewerComponent } from '../attachment-viewer/attachment-viewer.component';

@Component({
  selector: 'app-communication-history',
  standalone: true,
  imports: [CommonModule, ButtonModule, AttachmentViewerComponent],
  template: `
    <div class="timeline-container">
      <div class="timeline-header flex justify-between items-center mb-4">
        <h3>Communication History</h3>
        <span class="text-muted">{{ messages.length }} messages</span>
      </div>

      <div *ngIf="messages.length === 0" class="empty-history text-center p-8">
        <i class="e-icons e-comment" style="font-size: 48px; color: #e2e8f0"></i>
        <p class="text-muted mt-2">No communication history found.</p>
      </div>

      <div class="message-stack">
        <div *ngFor="let msg of messages" class="message-card" [class.hover-shadow]="true">
          <div class="card-top flex justify-between items-center mb-2">
            <div class="flex items-center gap-2">
              <span class="badge" [ngClass]="msg.direction?.toLowerCase() === 'inbound' ? 'badge-blue' : 'badge-green'">
                {{ msg.direction }}
              </span>
              <span class="channel-icon">
                <i class="e-icons" [ngClass]="msg.mode?.toLowerCase() === 'sms' ? 'e-comment' : 'e-envelope'"></i>
              </span>
            </div>
            <span class="timestamp">{{ msg.timestamp | date:'MMM dd, yyyy h:mm a' }}</span>
          </div>

          <div class="card-body">
            <div class="sender-info mb-2">
              <strong>{{ msg.direction?.toLowerCase() === 'inbound' ? 'Inbound Message' : 'Me (Outbound)' }}</strong>
            </div>

            <div class="message-content" [class.expanded]="expandedMap[msg.communicationId!]">
              <div [innerHTML]="msg.messageBody"></div>
            </div>

            <button *ngIf="msg.messageBody && msg.messageBody.length > 150" ejs-button [isPrimary]="false" cssClass="e-small e-flat" 
                    (click)="toggleExpand(msg.communicationId!)">
              {{ expandedMap[msg.communicationId!] ? 'Show Less' : 'Read Full Message' }}
            </button>
            <app-attachment-viewer [attachments]="msg.attachments || []" class="mt-4 block"></app-attachment-viewer>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .timeline-container { padding: 8px; }
    .message-stack { display: flex; flex-direction: column; gap: 16px; }
    .message-card {
      background: var(--surface);
      border-radius: var(--radius-md);
      padding: 20px;
      border: 1px solid var(--border-color);
      transition: all 0.2s;
    }
    .hover-shadow:hover { box-shadow: var(--shadow-md); border-color: #cbd5e1; }
    .channel-icon { color: #64748b; }
    .timestamp { font-size: 12px; color: #94a3b8; }
    .sender-info { font-size: 13px; }
    .message-content {
      font-size: 14px;
      line-height: 1.6;
      max-height: 80px;
      overflow: hidden;
      position: relative;
    }
    .message-content.expanded { max-height: none; }
  `]
})
export class CommunicationHistoryComponent {
  @Input() messages: CommunicationMessageDto[] = [];

  expandedMap: { [key: string]: boolean } = {};

  toggleExpand(id: string) {
    this.expandedMap[id] = !this.expandedMap[id];
  }
}

