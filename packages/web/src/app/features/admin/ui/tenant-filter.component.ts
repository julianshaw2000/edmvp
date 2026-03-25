import { Component, ChangeDetectionStrategy, inject, output, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, catchError, of } from 'rxjs';
import { AdminApiService } from '../data/admin-api.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-tenant-filter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isPlatformAdmin()) {
      <select
        class="border border-slate-300 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
        (change)="onTenantChange($event)"
      >
        <option value="">All Tenants</option>
        @for (tenant of tenants(); track tenant.id) {
          <option [value]="tenant.id">{{ tenant.name }}</option>
        }
      </select>
    }
  `,
})
export class TenantFilterComponent {
  private auth = inject(AuthService);
  private api = inject(AdminApiService);

  tenantChanged = output<string>();

  isPlatformAdmin = computed(() => this.auth.role() === 'PLATFORM_ADMIN');

  tenants = toSignal(
    this.api.listTenants(1, 100).pipe(
      map(res => res.items),
      catchError(() => of([]))
    ),
    { initialValue: [] }
  );

  onTenantChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.tenantChanged.emit(value);
  }
}
