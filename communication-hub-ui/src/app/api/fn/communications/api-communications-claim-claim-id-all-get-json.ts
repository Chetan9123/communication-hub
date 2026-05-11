import { HttpClient, HttpContext, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { filter, map } from 'rxjs/operators';
import { StrictHttpResponse } from '../../strict-http-response';
import { RequestBuilder } from '../../request-builder';
import { CommunicationThreadDto } from '../../models/communication-thread-dto';

export interface ApiCommunicationsClaimClaimIdAllGet$Json$Params {
  claimId: (number | string);
}

export function apiCommunicationsClaimClaimIdAllGet$Json(http: HttpClient, rootUrl: string, params: ApiCommunicationsClaimClaimIdAllGet$Json$Params, context?: HttpContext): Observable<StrictHttpResponse<CommunicationThreadDto>> {
  const rb = new RequestBuilder(rootUrl, apiCommunicationsClaimClaimIdAllGet$Json.PATH, 'get');
  if (params) {
    rb.path('claimId', params.claimId, {});
  }

  return http.request(
    rb.build({ responseType: 'json', accept: 'text/json', context })
  ).pipe(
    filter((r: any): r is HttpResponse<any> => r instanceof HttpResponse),
    map((r: HttpResponse<any>) => {
      return r as StrictHttpResponse<CommunicationThreadDto>;
    })
  );
}

apiCommunicationsClaimClaimIdAllGet$Json.PATH = '/api/Communications/claim/{claimId}/all';
