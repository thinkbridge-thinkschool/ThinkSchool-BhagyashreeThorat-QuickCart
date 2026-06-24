import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserProfile } from '../models';

@Injectable({ providedIn: 'root' })
export class UserApi {
  private readonly http = inject(HttpClient);

  /** Returns the current user, provisioning the local record from the token on first call. */
  me(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${environment.apiBaseUrl}/users/me`);
  }
}
