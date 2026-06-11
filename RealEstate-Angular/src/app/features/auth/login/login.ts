import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  error = signal<string | null>(null);
  isLoading = this.auth.isLoading;

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  submit() {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: (err) => {
        if (err.status === 401) {
          this.error.set('אימייל או סיסמה שגויים');
        } else {
          const serverMsg = err.error?.title || err.error?.message || (typeof err.error === 'string' ? err.error : null);
          this.error.set(serverMsg || 'שגיאה בכניסה');
        }
        console.error('Login error:', err);
      }
    });
  }
}
