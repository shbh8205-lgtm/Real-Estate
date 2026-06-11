import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  error = signal<string | null>(null);
  isLoading = this.auth.isLoading;

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phoneNumber: ['', [Validators.required]],
    maxBudget: [0, [Validators.required, Validators.min(0)]],
    role: ['Buyer' as 'Buyer' | 'Renter' | 'Agent', [Validators.required]],
  });

  submit() {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: (err) => {
        const serverMsg = err.error?.title || err.error?.message || (typeof err.error === 'string' ? err.error : null);
        this.error.set(serverMsg || 'שגיאה בהרשמה');
        console.error('Register error:', err);
      }
    });
  }
}
