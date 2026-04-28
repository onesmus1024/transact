import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

export interface HealthResponse {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/health`;

  check(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(this.url);
  }
}
