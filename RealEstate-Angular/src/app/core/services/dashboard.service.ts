import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Dashboard } from '../models/lead.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly API = 'https://localhost:7144/api/dashboard';
  private http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(this.API);
  }
}
