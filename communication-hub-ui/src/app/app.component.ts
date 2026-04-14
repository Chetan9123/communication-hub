import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './services/toast.service';
import { ToastModule, ToastComponent, ToastPositionModel, ToastAnimationSettingsModel } from '@syncfusion/ej2-angular-notifications';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, ToastModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'CommunicationHub';
  
  toastPosition: ToastPositionModel = { X: 'Right', Y: 'Bottom' };
  toastAnimation: ToastAnimationSettingsModel = {
    show: { effect: 'SlideBottomIn' },
    hide: { effect: 'SlideBottomOut' }
  };

  @ViewChild('globalToast') globalToast!: ToastComponent;

  constructor(private toastService: ToastService) { }

  ngOnInit(): void {
    // Subscribe to global toasts
    this.toastService.message$.subscribe(msg => {
      this.globalToast.show({
        title: msg.title,
        content: msg.content,
        cssClass: `e-toast-${msg.type}`
      });
    });
  }
}
