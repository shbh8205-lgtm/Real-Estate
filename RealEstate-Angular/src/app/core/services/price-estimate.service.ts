import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PriceEstimateRequest {
  city: string;
  rooms: number;
  sizeSqm: number;
  floor: number;
  age: number;
  hasParking: boolean;
  hasElevator: boolean;
}

export interface PriceEstimateResponse {
  estimatedPrice: number;
  lowerBound: number;
  upperBound: number;
  knownCities: string[];
}

@Injectable({ providedIn: 'root' })
export class PriceEstimateService {
  private readonly API = 'https://localhost:7144/api/price-estimate';
  private http = inject(HttpClient);

  getCities(): Observable<string[]> {
    return this.http.get<string[]>(`${this.API}/cities`);
  }

  estimate(req: PriceEstimateRequest): Observable<PriceEstimateResponse> {
    return this.http.post<PriceEstimateResponse>(this.API, req);
  }
}
