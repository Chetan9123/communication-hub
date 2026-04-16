import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule, DialogComponent } from '@syncfusion/ej2-angular-popups';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { TextBoxModule } from '@syncfusion/ej2-angular-inputs';
import { CheckBoxModule } from '@syncfusion/ej2-angular-buttons';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { HttpClient } from '@angular/common/http';
import { InvolvedPartyDto } from '../../api/models';
import { ToastService } from '../../services/toast.service';
import { Api } from '../../api/api';

@Component({
  selector: 'app-edit-party-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, DialogModule, ButtonModule, 
    TextBoxModule, CheckBoxModule, DropDownListModule
  ],
  templateUrl: './edit-party-dialog.component.html',
  styleUrls: ['./edit-party-dialog.component.scss']
})
export class EditPartyDialogComponent {
  @ViewChild('editDialog') editDialog!: DialogComponent;
  @Input() party: InvolvedPartyDto | null = null;
  @Input() claimId!: number;
  @Output() updated = new EventEmitter<void>();

  public isAddMode = false;

  public editModel: InvolvedPartyDto = { partyId: 0 };
  public roles: string[] = ['Attending Physician', 'Claimant', 'Defendant', 'Witness', 'Attorney', 'Employer', 'Other'];
  public isSaving = false;

  constructor(
    private http: HttpClient, 
    private toast: ToastService,
    private api: Api
  ) {}

  public show(party: InvolvedPartyDto | null) {
    this.party = party;
    this.isAddMode = !party;

    if (this.isAddMode) {
      this.editModel = { partyId: 0, involvedPartyType: 'Other', isInjured: false };
    } else {
      // Clone the model to avoid direct binding to the grid data
      this.editModel = JSON.parse(JSON.stringify(party));
    }
    this.editDialog.show();
  }

  public hide() {
    this.editDialog.hide();
  }

  public save() {
    this.isSaving = true;
    const url = this.isAddMode 
      ? `${this.api.rootUrl}/api/Claims/${this.claimId}/parties`
      : `${this.api.rootUrl}/api/Claims/parties/${this.editModel.partyId}`;
    
    const request = this.isAddMode 
      ? this.http.post(url, this.editModel) 
      : this.http.put(url, this.editModel);

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.toast.success('Success', 'Party details updated.');
        this.updated.emit();
        this.hide();
      },
      error: (err) => {
        this.isSaving = false;
        console.error('Update failed:', err);
        const errMsg = err.error?.message || err.message || 'Could not save changes.';
        this.toast.error('Update Failed', errMsg);
      }
    });
  }
}
