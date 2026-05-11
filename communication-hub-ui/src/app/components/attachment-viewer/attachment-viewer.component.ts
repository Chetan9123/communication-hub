import { Component, Input, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AttachmentDto } from '../../api/models/attachment-dto';
import { Api } from '../../api/api';
import { apiAttachmentsAttachmentIdUrlGet } from '../../api/fn/attachments/api-attachments-attachment-id-url-get';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-attachment-viewer',
  standalone: true,
  imports: [CommonModule, DialogModule],
  templateUrl: './attachment-viewer.component.html',
  styleUrls: ['./attachment-viewer.component.scss']
})
export class AttachmentViewerComponent implements OnInit {
  @Input() attachments: AttachmentDto[] = [];
  public attachmentUrls: { [id: string]: SafeResourceUrl } = {};
  
  @ViewChild('previewDialog') public previewDialog!: DialogComponent;

  public isLoadingUrl: boolean = false;
  public previewModel: {
    attachment: AttachmentDto | null,
    url: SafeResourceUrl | null,
    type: 'image' | 'video' | 'audio' | 'pdf' | 'other' | null,
    error: string | null
  } = { attachment: null, url: null, type: null, error: null };

  constructor(private api: Api, private sanitizer: DomSanitizer, private authService: AuthService) {}

  async ngOnInit() {
    // Automatically fetch presigned URLs for image previews
    if (this.attachments) {
      for (const att of this.attachments) {
        if (this.getFileType(att.mimeType) === 'image' && att.attachmentId) {
          try {
            const url = await this.fetchPresignedUrl(att.attachmentId);
            this.attachmentUrls[att.attachmentId] = this.sanitizer.bypassSecurityTrustResourceUrl(url);
          } catch (e) {
            console.error('Failed to load inline preview', e);
          }
        }
      }
    }
  }

  public getFileIcon(mimeType: string | null | undefined): string {
    if (!mimeType) return 'fa-paperclip';
    if (mimeType.startsWith('image/')) return 'fa-image text-blue-500';
    if (mimeType.startsWith('video/')) return 'fa-file-video text-purple-500';
    if (mimeType.startsWith('audio/')) return 'fa-file-audio text-yellow-500';
    if (mimeType === 'application/pdf') return 'fa-file-pdf text-red-500';
    if (mimeType.includes('spreadsheet') || mimeType.includes('excel')) return 'fa-file-excel text-green-500';
    if (mimeType.includes('document') || mimeType.includes('word')) return 'fa-file-word text-blue-600';
    if (mimeType.includes('zip') || mimeType.includes('compressed')) return 'fa-file-archive text-gray-500';
    return 'fa-file text-gray-400';
  }

  public getFileType(mimeType: string | null | undefined): 'image' | 'video' | 'audio' | 'pdf' | 'other' {
    if (!mimeType) return 'other';
    if (mimeType.startsWith('image/')) return 'image';
    if (mimeType.startsWith('video/')) return 'video';
    if (mimeType.startsWith('audio/')) return 'audio';
    if (mimeType === 'application/pdf') return 'pdf';
    return 'other';
  }

  public formatFileSize(bytes: number | string | null | undefined): string {
    if (!bytes) return 'Unknown size';
    const numBytes = typeof bytes === 'string' ? parseInt(bytes, 10) : bytes;
    if (isNaN(numBytes) || numBytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(numBytes) / Math.log(k));
    return parseFloat((numBytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  public getFallbackName(att: AttachmentDto): string {
    return att.fileName || `Attachment_${att.attachmentId?.substring(0, 8) || 'file'}`;
  }

  public async openPreview(att: AttachmentDto, event?: Event): Promise<void> {
    const startTime = performance.now();
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    
    if (!att.attachmentId) return;

    const fileType = this.getFileType(att.mimeType);

    if (fileType === 'other') {
      this.downloadFile(att);
      return;
    }

    // Set preview model INITIAL state
    this.previewModel = {
      attachment: att,
      type: fileType,
      url: null,
      error: null
    };

    this.previewDialog.show();

    // Check if we already have a URL (e.g. from ngOnInit images)
    if (this.attachmentUrls[att.attachmentId]) {
      console.log(`[AttachmentViewer] Using cached URL for ${att.attachmentId}. Time: ${performance.now() - startTime}ms`);
      this.previewModel.url = this.attachmentUrls[att.attachmentId];
      this.isLoadingUrl = false;
      return;
    }

    this.isLoadingUrl = true;
    try {
      const url = await this.fetchPresignedUrl(att.attachmentId);
      this.previewModel.url = this.sanitizer.bypassSecurityTrustResourceUrl(url);
      
      // Cache it for next time
      this.attachmentUrls[att.attachmentId] = this.previewModel.url;
      
      console.log(`[AttachmentViewer] URL fetched for ${att.attachmentId}. Time: ${performance.now() - startTime}ms`);
      this.isLoadingUrl = false;
    } catch (err) {
      console.error(`[AttachmentViewer] Failed to fetch URL for ${att.attachmentId}:`, err);
      this.isLoadingUrl = false;
      this.previewModel.error = "Could not load preview. The file might be expired or unavailable.";
    }
  }

  public downloadFile(att: AttachmentDto, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }

    if (!att.attachmentId) return;

    const fileName = this.getFallbackName(att);
    const downloadUrl = `http://localhost:5192/api/attachments/${att.attachmentId}/download`;

    // We must use fetch() to include the Authorization header because
    // the endpoint is protected by [Authorize]
    const token = this.authService.getToken();
    const headers = new Headers();
    if (token) {
      headers.append('Authorization', `Bearer ${token}`);
    }

    this.isLoadingUrl = true;

    fetch(downloadUrl, { headers })
      .then(res => {
        if (!res.ok) throw new Error(`Server returned ${res.status}`);
        return res.blob();
      })
      .then(blob => {
        this.isLoadingUrl = false;
        const objectUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
      })
      .catch(err => {
        this.isLoadingUrl = false;
        console.error('[AttachmentViewer] Download proxy failed:', err);
        // Fallback to opening in new tab (may still fail if unauthorized but serves as a last resort)
        window.open(downloadUrl, '_blank');
      });
  }

  private async fetchPresignedUrl(attachmentId: string): Promise<string> {
    const res: any = await this.api.invoke(apiAttachmentsAttachmentIdUrlGet, { attachmentId });
    if (res && res.url) {
      return res.url;
    }
    throw new Error("No URL returned from server");
  }

  public closePreview(): void {
    this.previewDialog.hide();
    setTimeout(() => {
      this.previewModel = { attachment: null, url: null, type: null, error: null };
    }, 300); // clear after animation
  }
}
