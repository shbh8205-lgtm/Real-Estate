import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PropertyService } from '../../../core/services/property.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Property } from '../../../core/models/property.model';

@Component({
  selector: 'app-property-list',
  imports: [CommonModule, RouterLink],
  templateUrl: './property-list.html',
  styleUrl: './property-list.css',
})
export class PropertyListComponent implements OnInit {
  private propertyService = inject(PropertyService);
  private auth = inject(AuthService);

  properties = signal<Property[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  isAgent = computed(() => this.auth.currentUser()?.role === 'Agent');

  ngOnInit(): void {
    this.loading.set(true);
    this.propertyService.getAll().subscribe({
      next: (data) => {
        this.properties.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('שגיאה בטעינת נכסים');
        this.loading.set(false);
        console.error(err);
      }
    });
  }
}
