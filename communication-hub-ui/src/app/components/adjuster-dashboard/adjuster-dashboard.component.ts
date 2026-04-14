import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { GridModule, PageService, SortService, FilterService, ToolbarService, ExcelExportService, PdfExportService, SearchService } from '@syncfusion/ej2-angular-grids';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import { Api } from '../../api/api';
import { apiClaimsAssignedToAdjusterGet$Json } from '../../api/fn/claims/api-claims-assigned-to-adjuster-get-json';
import { apiUsersDashboardGet$Json } from '../../api/fn/users/api-users-dashboard-get-json';
import { apiUsersToggleStatusPost$Json } from '../../api/fn/users/api-users-toggle-status-post-json';
import { apiCommunicationsUnreadGet$Json } from '../../api/fn/communications/api-communications-unread-get-json';
import { AssignedClaimDto, UnreadCommunicationDto, AdjusterDashboardDto } from '../../api/models';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-adjuster-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, GridModule, ButtonModule],
  providers: [PageService, SortService, FilterService, ToolbarService],
  templateUrl: './adjuster-dashboard.component.html',
  styleUrls: ['./adjuster-dashboard.component.scss']
})
export class AdjusterDashboardComponent implements OnInit {
  public claims: AssignedClaimDto[] = [];
  public isActive = true;
  public isLoading = false;
  public toolbarOptions: string[] = ['Search'];
  public pageSettings = { pageSize: 12, pageSizes: [12, 25, 50] };
  public filterSettings = { type: 'Menu' };

  constructor(
    private api: Api,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    this.isLoading = true;
    this.api.invoke(apiUsersDashboardGet$Json).then((data: AdjusterDashboardDto) => {
      this.claims = data.assignedClaims || [];
      this.isActive = data.isActive ?? true;
      this.isLoading = false;
    }).catch(err => {
      this.isLoading = false;
      console.error('[AdjusterDashboard] Error loading dashboard:', err);
      this.toast.error('Error', 'Failed to load dashboard data.');
    });
  }

  toggleStatus() {
    this.api.invoke(apiUsersToggleStatusPost$Json).then((result: boolean) => {
      this.isActive = result;
      this.toast.success('Status Updated', `You are now ${this.isActive ? 'Active' : 'Out of Office'}.`);
    }).catch(err => {
      console.error('[AdjusterDashboard] Error toggling status:', err);
      this.toast.error('Error', 'Failed to update status.');
    });
  }

  loadUnread() {
    this.isLoading = true;
    this.api.invoke(apiCommunicationsUnreadGet$Json).then((msgs: UnreadCommunicationDto[]) => {
      const claimIds = new Set(msgs.map(m => m.claimId));
      this.claims = this.claims.filter(c => claimIds.has(c.claimId as number));
      this.isLoading = false;
      this.toast.success('Filtered', `Showing ${this.claims.length} claims with unread messages.`);
    }).catch(err => {
      this.isLoading = false;
      console.error('[AdjusterDashboard] Error loading unread claims:', err);
    });
  }

  onRowSelected(args: any) {
    if (args.data) this.openClaim(args.data.claimId);
  }

  openClaim(claimId: any) {
    this.router.navigate(['/claims', claimId, 'details']);
  }

  onToolbarClick(args: any) {
    const grid = (document.getElementsByClassName('e-grid')[0] as any).ej2_instances[0];
    if (args.item.id.includes('excel-export')) grid.excelExport();
    if (args.item.id.includes('csv-export')) grid.csvExport();
  }
}