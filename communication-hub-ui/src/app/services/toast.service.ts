import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface ToastMessage {
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  content: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private messageSubject = new Subject<ToastMessage>();
  message$ = this.messageSubject.asObservable();

  success(title: string, content: string = '') {
    this.messageSubject.next({ type: 'success', title, content });
  }
  error(title: string, content: string = '') {
    this.messageSubject.next({ type: 'error', title, content });
  }
  warn(title: string, content: string = '') {
    this.messageSubject.next({ type: 'warning', title, content });
  }
  info(title: string, content: string = '') {
    this.messageSubject.next({ type: 'info', title, content });
  }
}
