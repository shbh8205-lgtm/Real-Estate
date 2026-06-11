import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { AuthResponse, LoginRequest, RegisterRequest, Client } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly API = 'https://localhost:7144/api';

  // Signals
  private _currentUser = signal<Client | null>(this.loadUserFromStorage());
  private _isLoading = signal<boolean>(false);

  // Public computed (read-only)
  currentUser = this._currentUser.asReadonly();
  isLoading = this._isLoading.asReadonly();
  isLoggedIn = computed(() => this._currentUser() !== null);

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest) {
    this._isLoading.set(true);
    return this.http.post<AuthResponse>(`${this.API}/auth/login`, request).pipe(
      tap({
        next: (response) => this.handleAuthSuccess(response),
        error: () => this._isLoading.set(false)
      })
    );
  }

  register(request: RegisterRequest) {
    this._isLoading.set(true);
    return this.http.post<AuthResponse>(`${this.API}/auth/register`, request).pipe(
      tap({
        next: (response) => this.handleAuthSuccess(response),
        error: () => this._isLoading.set(false)
      })
    );
  }

  private handleAuthSuccess(response: AuthResponse) {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem('current_user', JSON.stringify(response.client));
    this._currentUser.set(response.client);
    this._isLoading.set(false);
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem('current_user');
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private loadUserFromStorage(): Client | null {
    const user = localStorage.getItem('current_user');
    return user ? JSON.parse(user) : null;
  }
}