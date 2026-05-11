import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClaimHistoryComponent } from './claim-history.component';
import { ActivatedRoute, Router } from '@angular/router';
import { Api } from '../../api/api';
import { ToastService } from '../../services/toast.service';
import { of } from 'rxjs';
import { ClaimDetailsDto, CommunicationThreadDto, CommunicationMessageDto } from '../../api/models';
import { apiClaimsClaimIdGet$Json } from '../../api/fn/claims/api-claims-claim-id-get-json';
import { apiCommunicationsClaimClaimIdAllGet$Json } from '../../api/fn/communications/api-communications-claim-claim-id-all-get-json';
import { apiCommunicationsCommIdNotesPut$Json } from '../../api/fn/communications/api-communications-comm-id-notes-put-json';

describe('ClaimHistoryComponent', () => {
  let component: ClaimHistoryComponent;
  let fixture: ComponentFixture<ClaimHistoryComponent>;
  let mockRouter: any;
  let mockApi: any;
  let mockToastService: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockRouter = {
      navigate: jasmine.createSpy('navigate')
    };

    mockApi = {
      invoke: jasmine.createSpy('invoke').and.callFake((fn: any, params: any) => {
        if (fn === apiClaimsClaimIdGet$Json) {
          return Promise.resolve({ claimId: 1, claimNumber: 'CLM-123' } as ClaimDetailsDto);
        } else if (fn === apiCommunicationsClaimClaimIdAllGet$Json) {
          return Promise.resolve({
            messages: [
              { communicationId: 101, mode: 'Email', notes: 'Note 1' },
              { communicationId: 102, mode: 'SMS', notes: 'Note 2' }
            ]
          } as CommunicationThreadDto);
        } else if (fn === apiCommunicationsCommIdNotesPut$Json) {
          return Promise.resolve({});
        }
        return Promise.reject('Unknown API fn');
      })
    };

    mockToastService = {
      success: jasmine.createSpy('success'),
      error: jasmine.createSpy('error')
    };

    mockActivatedRoute = {
      paramMap: of({ get: (key: string) => '1' })
    };

    await TestBed.configureTestingModule({
      imports: [ClaimHistoryComponent], // Standalone component
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: Api, useValue: mockApi },
        { provide: ToastService, useValue: mockToastService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load claim and history on init', async () => {
    expect(component.claimId).toBe(1);
    expect(mockApi.invoke).toHaveBeenCalledWith(apiClaimsClaimIdGet$Json, { claimId: 1 });
    expect(mockApi.invoke).toHaveBeenCalledWith(apiCommunicationsClaimClaimIdAllGet$Json, { claimId: 1 });
    
    await fixture.whenStable();
    expect(component.claim?.claimNumber).toBe('CLM-123');
    expect(component.allMessages.length).toBe(2);
  });

  it('should filter messages based on active tab', async () => {
    await fixture.whenStable(); // wait for init data to load
    
    component.setActiveTab('Email');
    expect(component.filteredMessages.length).toBe(1);
    expect(component.filteredMessages[0].mode).toBe('Email');

    component.setActiveTab('All');
    expect(component.filteredMessages.length).toBe(2);
  });

  it('should set active tab', () => {
    component.setActiveTab('SMS');
    expect(component.activeTab).toBe('SMS');
  });

  it('should update message notes successfully', async () => {
    const msg = { communicationId: 101, mode: 'Email', notes: 'Updated note' } as CommunicationMessageDto;
    
    component.updateMessageNotes(msg);
    
    expect(mockApi.invoke).toHaveBeenCalledWith(apiCommunicationsCommIdNotesPut$Json, {
      commId: 101,
      body: { notes: 'Updated note' }
    });
    
    await fixture.whenStable();
    expect(mockToastService.success).toHaveBeenCalledWith('Saved', 'Note updated successfully');
  });

  it('should handle error when updating message notes', async () => {
    mockApi.invoke.and.callFake((fn: any, params: any) => {
      if (fn === apiCommunicationsCommIdNotesPut$Json) {
        return Promise.reject('API Error');
      }
      return Promise.resolve({});
    });
    const msg = { communicationId: 101, mode: 'Email', notes: 'Updated note' } as CommunicationMessageDto;
    
    component.updateMessageNotes(msg);
    
    await fixture.whenStable();
    expect(mockToastService.error).toHaveBeenCalledWith('Error', 'Failed to save note');
  });

  it('should go back to dashboard', () => {
    component.goBack();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/dashboard']);
  });
});
