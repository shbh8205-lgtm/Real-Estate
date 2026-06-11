import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PropertyService } from '../../../core/services/property.service';
import {
  PriceEstimateService,
  PriceEstimateResponse,
} from '../../../core/services/price-estimate.service';

@Component({
  selector: 'app-property-create',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './property-create.html',
  styleUrl: './property-create.css',
})
export class PropertyCreateComponent implements OnInit {
  private fb = inject(FormBuilder);
  private propertyService = inject(PropertyService);
  private priceEstimateService = inject(PriceEstimateService);
  private router = inject(Router);

  isSubmitting = signal<boolean>(false);
  error = signal<string | null>(null);

  cities = signal<string[]>([]);
  estimate = signal<PriceEstimateResponse | null>(null);
  isEstimating = signal<boolean>(false);
  estimateError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    price: [0, [Validators.required, Validators.min(1)]],
    address: ['', [Validators.required]],
  });

  estimateForm = this.fb.nonNullable.group({
    city: ['TelAviv', Validators.required],
    rooms: [4, [Validators.required, Validators.min(1)]],
    sizeSqm: [100, [Validators.required, Validators.min(20)]],
    floor: [3, [Validators.required, Validators.min(0)]],
    age: [10, [Validators.required, Validators.min(0)]],
    hasParking: [true],
    hasElevator: [true],
  });

  ngOnInit(): void {
    this.priceEstimateService.getCities().subscribe({
      next: (cities) => this.cities.set(cities),
      error: (err) => console.error('Failed to load cities:', err),
    });
  }

  runEstimate() {
    if (this.estimateForm.invalid) {
      this.estimateForm.markAllAsTouched();
      return;
    }
    this.estimateError.set(null);
    this.isEstimating.set(true);
    this.priceEstimateService.estimate(this.estimateForm.getRawValue()).subscribe({
      next: (res) => {
        this.estimate.set(res);
        this.isEstimating.set(false);
      },
      error: (err) => {
        this.isEstimating.set(false);
        this.estimateError.set('שגיאה בחישוב הערכה');
        console.error('Estimate error:', err);
      },
    });
  }

  useEstimatedPrice() {
    const est = this.estimate();
    if (est) {
      this.form.patchValue({ price: est.estimatedPrice });
    }
  }

  submit() {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.propertyService.create(this.form.getRawValue()).subscribe({
      next: (newId) => {
        this.isSubmitting.set(false);
        this.router.navigate(['/properties', newId]);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const serverMsg = err.error?.title || err.error?.message || (typeof err.error === 'string' ? err.error : null);
        this.error.set(serverMsg || 'שגיאה ביצירת הנכס');
        console.error('Create property error:', err);
      },
    });
  }
}
