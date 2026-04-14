import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonModule],
  template: `
    <div class="not-found-container">
      <div class="illustration">
        <i class="e-icons e-search"></i>
        <span>404</span>
      </div>
      <h1>Oops! Page Not Found</h1>
      <p>The page you're looking for doesn't exist or has been moved.</p>
      <button ejs-button [isPrimary]="true" routerLink="/dashboard">
        Return to Dashboard
      </button>
    </div>
  `,
  styles: [`
    .not-found-container {
      height: 100vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 24px;
      color: #0f172a;
    }
    .illustration {
      font-size: 120px;
      font-weight: 800;
      color: #e2e8f0;
      position: relative;
      margin-bottom: 24px;
      line-height: 1;

      i {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        font-size: 48px;
        color: #3b82f6;
        opacity: 0.5;
      }
    }
    h1 { font-size: 32px; font-weight: 700; margin-bottom: 12px; }
    p { color: #64748b; margin-bottom: 32px; max-width: 400px; }
  `]
})
export class NotFoundComponent {}
