/* eslint-disable */
/* Manual addition for Toggle Status */

import { HttpClient, HttpContext, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { filter, map } from 'rxjs/operators';
import { StrictHttpResponse } from '../../strict-http-response';
import { RequestBuilder } from '../../request-builder';

export interface ApiUsersToggleStatusPost$Json$Params {
}

export function apiUsersToggleStatusPost$Json(http: HttpClient, rootUrl: string, params?: ApiUsersToggleStatusPost$Json$Params, context?: HttpContext): Observable<StrictHttpResponse<boolean>> {
  const rb = new RequestBuilder(rootUrl, apiUsersToggleStatusPost$Json.PATH, 'post');
  if (params) {
  }

  return http.request(
    rb.build({ responseType: 'json', accept: 'text/json', context })
  ).pipe(
    filter((r: any): r is HttpResponse<any> => r instanceof HttpResponse),
    map((r: HttpResponse<any>) => {
      return r as StrictHttpResponse<boolean>;
    })
  );
}

apiUsersToggleStatusPost$Json.PATH = '/api/Users/toggle-status';
