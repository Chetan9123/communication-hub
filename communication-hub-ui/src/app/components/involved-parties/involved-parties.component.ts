import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GridModule, PageService, SortService, FilterService } from '@syncfusion/ej2-angular-grids';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { InvolvedPartyDto } from '../../api/models';

@Component({
  selector: 'app-involved-parties',
  standalone: true,
  imports: [CommonModule, GridModule, ButtonModule],
  providers: [PageService, SortService, FilterService],
  template: `
    <div class="parties-container">
      <ejs-grid [dataSource]="parties" [allowPaging]="true" [pageSettings]="{ pageSize: 5 }"
                [allowSorting]="true" [allowFiltering]="true" [filterSettings]="{ type: 'Menu' }">
        <e-columns>
          <e-column field="firstName" headerText="Name" width="180">
            <ng-template #template let-data>
              <div class="party-name">{{ data.firstName }} {{ data.lastName }}</div>
              <div class="text-muted" style="font-size: 11px">{{ data.involvedPartyType }}</div>
            </ng-template>
          </e-column>
          <e-column field="phone" headerText="Phone" width="150"></e-column>
          <e-column field="email" headerText="Email" width="200"></e-column>
          <e-column headerText="Actions" width="140" textAlign="Center">
            <ng-template #template let-data>
              <div class="flex gap-2 justify-center">
                <button ejs-button [disabled]="!data.phone" (click)="sms.emit(data)"
                        cssClass="e-small e-round" title="Send SMS">
                  <span class="e-btn-icon e-icons e-comment"></span>
                </button>
                <button ejs-button [disabled]="!data.email" (click)="email.emit(data)"
                        cssClass="e-small e-round" title="Send Email">
                  <span class="e-btn-icon e-icons e-envelope"></span>
                </button>
              </div>
            </ng-template>
          </e-column>
        </e-columns>
      </ejs-grid>
    </div>
  `,
  styles: [`
    .party-name { font-weight: 600; color: var(--text-main); }
    .flex { display: flex; }
    .gap-2 { gap: 8px; }
    .justify-center { justify-content: center; }
  `]
})
export class InvolvedPartiesComponent {
  @Input() parties: InvolvedPartyDto[] = [];
  @Output() sms = new EventEmitter<InvolvedPartyDto>();
  @Output() email = new EventEmitter<InvolvedPartyDto>();
}
