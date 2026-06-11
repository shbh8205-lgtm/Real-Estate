import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateLeadRequest, Lead } from '../models/lead.model';

@Injectable({ providedIn: 'root' })
export class LeadService {
  private readonly API = 'https://localhost:7144/api/leads';
  private http = inject(HttpClient);

  create(payload: CreateLeadRequest): Observable<number> {
    return this.http.post<number>(this.API, payload);
  }

  getAll(): Observable<Lead[]> {
    return this.http.get<Lead[]>(this.API);
  }
}
