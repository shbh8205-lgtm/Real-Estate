import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Property } from '../models/property.model';

export interface CreatePropertyPayload {
  title: string;
  description: string;
  price: number;
  address: string;
}

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly API = 'https://localhost:7144/api/properties';
  private http = inject(HttpClient);

  getAll(): Observable<Property[]> {
    return this.http.get<Property[]>(this.API);
  }

  getById(id: number): Observable<Property> {
    return this.http.get<Property>(`${this.API}/${id}`);
  }

  create(payload: CreatePropertyPayload): Observable<number> {
    return this.http.post<number>(this.API, payload);
  }
}
