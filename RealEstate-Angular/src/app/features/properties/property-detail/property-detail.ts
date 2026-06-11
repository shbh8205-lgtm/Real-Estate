import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PropertyService } from '../../../core/services/property.service';
import { LeadService } from '../../../core/services/lead.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Property } from '../../../core/models/property.model';
import {
  PriceEstimateService,
  PriceEstimateResponse,
} from '../../../core/services/price-estimate.service';

const CITY_ALIASES: Record<string, string> = {
  'תל אביב': 'TelAviv',
  'תל-אביב': 'TelAviv',
  'telaviv': 'TelAviv',
  'tel aviv': 'TelAviv',
  'ירושלים': 'Jerusalem',
  'jerusalem': 'Jerusalem',
  'רמת גן': 'RamatGan',
  'רמת-גן': 'RamatGan',
  'ramat gan': 'RamatGan',
  'הרצליה': 'Herzliya',
  'herzliya': 'Herzliya',
  'חיפה': 'Haifa',
  'haifa': 'Haifa',
  'פתח תקווה': 'PetahTikva',
  'פתח-תקווה': 'PetahTikva',
  'petah tikva': 'PetahTikva',
  'נתניה': 'Netanya',
  'netanya': 'Netanya',
  'בת ים': 'BatYam',
  'בת-ים': 'BatYam',
  'bat yam': 'BatYam',
  'באר שבע': 'BeerSheva',
  'באר-שבע': 'BeerSheva',
  'beer sheva': 'BeerSheva',
  'ראשון': 'Rishon',
  'rishon': 'Rishon',
};

function detectCity(text: string): string {
  const low = text.toLowerCase();
  for (const alias of Object.keys(CITY_ALIASES)) {
    if (low.includes(alias)) return CITY_ALIASES[alias];
  }
  return 'TelAviv';
}

@Component({
  selector: 'app-property-detail',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './property-detail.html',
  styleUrl: './property-detail.css',
})
export class PropertyDetailComponent implements OnInit {
  private propertyService = inject(PropertyService);
  private leadService = inject(LeadService);
  private priceEstimateService = inject(PriceEstimateService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  property = signal<Property | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  canSendLead = computed(() => {
    const role = this.auth.currentUser()?.role;
    return role === 'Buyer' || role === 'Renter';
  });

  leadForm = this.fb.nonNullable.group({
    message: ['', [Validators.required, Validators.minLength(5)]],
  });

  isSendingLead = signal<boolean>(false);
  leadSent = signal<boolean>(false);
  leadError = signal<string | null>(null);

  cities = signal<string[]>([]);
  estimate = signal<PriceEstimateResponse | null>(null);
  isEstimating = signal<boolean>(false);
  showEstimateForm = signal<boolean>(false);

  estimateForm = this.fb.nonNullable.group({
    city: ['TelAviv', Validators.required],
    rooms: [4, [Validators.required, Validators.min(1)]],
    sizeSqm: [100, [Validators.required, Validators.min(20)]],
    floor: [3, [Validators.required, Validators.min(0)]],
    age: [15, [Validators.required, Validators.min(0)]],
    hasParking: [true],
    hasElevator: [true],
  });

  delta = computed(() => {
    const p = this.property();
    const e = this.estimate();
    if (!p || !e) return null;
    const diff = ((p.price - e.estimatedPrice) / e.estimatedPrice) * 100;
    return Math.round(diff * 10) / 10;
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set('מזהה נכס לא תקין');
      return;
    }

    this.priceEstimateService.getCities().subscribe({
      next: (cities) => this.cities.set(cities),
    });

    this.loading.set(true);
    this.propertyService.getById(id).subscribe({
      next: (data) => {
        this.property.set(data);
        this.loading.set(false);
        const detected = detectCity(`${data.address} ${data.title} ${data.description}`);
        this.estimateForm.patchValue({ city: detected });
        this.runEstimate();
      },
      error: (err) => {
        this.error.set(err.status === 404 ? 'הנכס לא נמצא' : 'שגיאה בטעינת הנכס');
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  runEstimate(): void {
    if (this.estimateForm.invalid) {
      this.estimateForm.markAllAsTouched();
      return;
    }
    this.isEstimating.set(true);
    this.priceEstimateService.estimate(this.estimateForm.getRawValue()).subscribe({
      next: (res) => {
        this.estimate.set(res);
        this.isEstimating.set(false);
      },
      error: (err) => {
        this.isEstimating.set(false);
        console.error('Estimate error:', err);
      },
    });
  }

  toggleEstimateForm(): void {
    this.showEstimateForm.update((v) => !v);
  }

  sendLead(): void {
    const p = this.property();
    if (!p || this.leadForm.invalid) {
      this.leadForm.markAllAsTouched();
      return;
    }

    this.leadError.set(null);
    this.isSendingLead.set(true);

    this.leadService.create({
      propertyId: p.id,
      message: this.leadForm.controls.message.value,
    }).subscribe({
      next: () => {
        this.leadSent.set(true);
        this.isSendingLead.set(false);
        this.leadForm.reset({ message: '' });
      },
      error: (err) => {
        this.isSendingLead.set(false);
        const serverMsg = err.error?.title || err.error?.message;
        this.leadError.set(serverMsg || 'שגיאה בשליחת הפנייה');
        console.error(err);
      },
    });
  }
}
