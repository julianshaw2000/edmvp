import { Component, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { AuthService } from '../../core/auth/auth.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

const COUNTRIES = [
  { code: 'AF', name: 'Afghanistan' }, { code: 'AL', name: 'Albania' }, { code: 'DZ', name: 'Algeria' },
  { code: 'AO', name: 'Angola' }, { code: 'AR', name: 'Argentina' }, { code: 'AU', name: 'Australia' },
  { code: 'AT', name: 'Austria' }, { code: 'BD', name: 'Bangladesh' }, { code: 'BE', name: 'Belgium' },
  { code: 'BJ', name: 'Benin' }, { code: 'BO', name: 'Bolivia' }, { code: 'BR', name: 'Brazil' },
  { code: 'BF', name: 'Burkina Faso' }, { code: 'BI', name: 'Burundi' }, { code: 'KH', name: 'Cambodia' },
  { code: 'CM', name: 'Cameroon' }, { code: 'CA', name: 'Canada' }, { code: 'CF', name: 'Central African Republic' },
  { code: 'TD', name: 'Chad' }, { code: 'CL', name: 'Chile' }, { code: 'CN', name: 'China' },
  { code: 'CO', name: 'Colombia' }, { code: 'CD', name: 'Congo (DRC)' }, { code: 'CG', name: 'Congo (Republic)' },
  { code: 'CR', name: 'Costa Rica' }, { code: 'CI', name: "Côte d'Ivoire" }, { code: 'HR', name: 'Croatia' },
  { code: 'CU', name: 'Cuba' }, { code: 'CZ', name: 'Czech Republic' }, { code: 'DK', name: 'Denmark' },
  { code: 'EC', name: 'Ecuador' }, { code: 'EG', name: 'Egypt' }, { code: 'ER', name: 'Eritrea' },
  { code: 'ET', name: 'Ethiopia' }, { code: 'FI', name: 'Finland' }, { code: 'FR', name: 'France' },
  { code: 'GA', name: 'Gabon' }, { code: 'DE', name: 'Germany' }, { code: 'GH', name: 'Ghana' },
  { code: 'GR', name: 'Greece' }, { code: 'GT', name: 'Guatemala' }, { code: 'GN', name: 'Guinea' },
  { code: 'GY', name: 'Guyana' }, { code: 'HN', name: 'Honduras' }, { code: 'HK', name: 'Hong Kong' },
  { code: 'HU', name: 'Hungary' }, { code: 'IN', name: 'India' }, { code: 'ID', name: 'Indonesia' },
  { code: 'IR', name: 'Iran' }, { code: 'IQ', name: 'Iraq' }, { code: 'IE', name: 'Ireland' },
  { code: 'IL', name: 'Israel' }, { code: 'IT', name: 'Italy' }, { code: 'JP', name: 'Japan' },
  { code: 'JO', name: 'Jordan' }, { code: 'KZ', name: 'Kazakhstan' }, { code: 'KE', name: 'Kenya' },
  { code: 'KR', name: 'Korea (South)' }, { code: 'KW', name: 'Kuwait' }, { code: 'LA', name: 'Laos' },
  { code: 'LR', name: 'Liberia' }, { code: 'LY', name: 'Libya' }, { code: 'MG', name: 'Madagascar' },
  { code: 'MW', name: 'Malawi' }, { code: 'MY', name: 'Malaysia' }, { code: 'ML', name: 'Mali' },
  { code: 'MX', name: 'Mexico' }, { code: 'MN', name: 'Mongolia' }, { code: 'MA', name: 'Morocco' },
  { code: 'MZ', name: 'Mozambique' }, { code: 'MM', name: 'Myanmar' }, { code: 'NA', name: 'Namibia' },
  { code: 'NP', name: 'Nepal' }, { code: 'NL', name: 'Netherlands' }, { code: 'NZ', name: 'New Zealand' },
  { code: 'NI', name: 'Nicaragua' }, { code: 'NE', name: 'Niger' }, { code: 'NG', name: 'Nigeria' },
  { code: 'NO', name: 'Norway' }, { code: 'PK', name: 'Pakistan' }, { code: 'PA', name: 'Panama' },
  { code: 'PG', name: 'Papua New Guinea' }, { code: 'PY', name: 'Paraguay' }, { code: 'PE', name: 'Peru' },
  { code: 'PH', name: 'Philippines' }, { code: 'PL', name: 'Poland' }, { code: 'PT', name: 'Portugal' },
  { code: 'RO', name: 'Romania' }, { code: 'RU', name: 'Russia' }, { code: 'RW', name: 'Rwanda' },
  { code: 'SA', name: 'Saudi Arabia' }, { code: 'SN', name: 'Senegal' }, { code: 'RS', name: 'Serbia' },
  { code: 'SL', name: 'Sierra Leone' }, { code: 'SG', name: 'Singapore' }, { code: 'SK', name: 'Slovakia' },
  { code: 'ZA', name: 'South Africa' }, { code: 'SS', name: 'South Sudan' }, { code: 'ES', name: 'Spain' },
  { code: 'LK', name: 'Sri Lanka' }, { code: 'SD', name: 'Sudan' }, { code: 'SR', name: 'Suriname' },
  { code: 'SE', name: 'Sweden' }, { code: 'CH', name: 'Switzerland' }, { code: 'TW', name: 'Taiwan' },
  { code: 'TZ', name: 'Tanzania' }, { code: 'TH', name: 'Thailand' }, { code: 'TG', name: 'Togo' },
  { code: 'TN', name: 'Tunisia' }, { code: 'TR', name: 'Turkey' }, { code: 'UG', name: 'Uganda' },
  { code: 'UA', name: 'Ukraine' }, { code: 'AE', name: 'United Arab Emirates' },
  { code: 'GB', name: 'United Kingdom' }, { code: 'US', name: 'United States' },
  { code: 'UY', name: 'Uruguay' }, { code: 'UZ', name: 'Uzbekistan' }, { code: 'VE', name: 'Venezuela' },
  { code: 'VN', name: 'Vietnam' }, { code: 'ZM', name: 'Zambia' }, { code: 'ZW', name: 'Zimbabwe' },
];

const MINERAL_TYPES = [
  { value: 'Tungsten (Wolframite)', label: 'Tungsten (Wolframite)' },
  { value: 'Tungsten (Cassiterite)', label: 'Tungsten (Cassiterite)' },
  { value: 'Tin (Cassiterite)', label: 'Tin (Cassiterite)' },
  { value: 'Tantalum (Coltan)', label: 'Tantalum (Coltan)' },
  { value: 'Tantalum (Tantalite)', label: 'Tantalum (Tantalite)' },
  { value: 'Gold (Alluvial)', label: 'Gold (Alluvial)' },
  { value: 'Gold (Hard Rock)', label: 'Gold (Hard Rock)' },
];

@Component({
  selector: 'app-create-batch',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent],
  template: `
    <a [routerLink]="returnRoute" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header
      title="Create New Batch"
      subtitle="Register a new mineral batch in the supply chain"
    />

    <div class="max-w-2xl">
      <form (ngSubmit)="onSubmit()" class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-6 sm:p-8 space-y-6">
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Batch Number</label>
            <input
              type="text"
              [(ngModel)]="batchNumber"
              name="batchNumber"
              required
              placeholder="e.g. BATCH-2026-001"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
            />
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Mineral Type</label>
            <select
              [(ngModel)]="mineralType"
              name="mineralType"
              required
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
            >
              <option value="">Select mineral type...</option>
              @for (m of mineralTypes; track m.value) {
                <option [value]="m.value">{{ m.label }}</option>
              }
            </select>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="relative">
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Origin Country</label>
              <input
                type="text"
                [(ngModel)]="countrySearchQuery"
                (ngModelChange)="onCountrySearch($event)"
                (focus)="countryDropdownOpen.set(true)"
                name="originCountry"
                required
                placeholder="Search country..."
                autocomplete="off"
                [class]="'w-full px-4 py-2.5 border rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow ' + (originCountry ? 'border-emerald-300 bg-emerald-50/30' : 'border-slate-300')"
              />
              @if (countryDropdownOpen() && filteredCountries().length > 0 && !originCountry) {
                <div class="absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg max-h-48 overflow-y-auto">
                  @for (c of filteredCountries(); track c.code) {
                    <button type="button" (click)="selectCountry(c)"
                      class="w-full text-left px-4 py-2 text-sm hover:bg-indigo-50 border-b border-slate-100 last:border-0">
                      <span class="font-medium text-slate-900">{{ c.name }}</span>
                      <span class="text-slate-400 ml-2">{{ c.code }}</span>
                    </button>
                  }
                </div>
              }
              @if (countryDropdownOpen() && filteredCountries().length === 0 && countrySearchQuery.length > 0 && !originCountry) {
                <div class="absolute z-10 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-lg px-4 py-3 text-sm text-slate-500">
                  No matching country found
                </div>
              }
            </div>
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Mine Site</label>
              <input
                type="text"
                [(ngModel)]="mineSite"
                name="mineSite"
                required
                placeholder="Mine or site name"
                class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Estimated Weight (kg)</label>
            <input
              type="number"
              [(ngModel)]="estimatedWeight"
              name="estimatedWeight"
              required
              min="0"
              step="0.01"
              placeholder="0.00"
              class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
            />
          </div>

          @if (facade.submitError()) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
              <svg class="w-5 h-5 text-rose-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700">{{ facade.submitError() }}</p>
            </div>
          }
        </div>

        <div class="px-6 sm:px-8 py-4 bg-slate-50 border-t border-slate-200 flex items-center gap-3">
          <button
            type="submit"
            [disabled]="facade.submitting()"
            class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm shadow-indigo-600/20 transition-all duration-150"
          >
            {{ facade.submitting() ? 'Creating...' : 'Create Batch' }}
          </button>
          <button
            type="button"
            (click)="onCancel()"
            class="text-sm font-medium text-slate-500 hover:text-slate-700 px-4 py-2.5 rounded-xl hover:bg-slate-100 transition-all duration-150"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  `,
})
export class CreateBatchComponent {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);
  private auth = inject(AuthService);

  mineralTypes = MINERAL_TYPES;

  batchNumber = '';
  mineralType = '';
  originCountry = '';
  mineSite = '';
  estimatedWeight: number | null = null;

  // Country typeahead
  countrySearchQuery = '';
  countryDropdownOpen = signal(false);
  private countryQuery = signal('');
  filteredCountries = computed(() => {
    const q = this.countryQuery().toLowerCase().trim();
    if (!q) return COUNTRIES;
    return COUNTRIES.filter(c =>
      c.name.toLowerCase().includes(q) || c.code.toLowerCase().includes(q)
    );
  });

  onCountrySearch(query: string) {
    this.originCountry = '';
    this.countryQuery.set(query);
    this.countryDropdownOpen.set(true);
  }

  selectCountry(c: { code: string; name: string }) {
    this.originCountry = c.code;
    this.countrySearchQuery = `${c.name} (${c.code})`;
    this.countryDropdownOpen.set(false);
  }

  protected get returnRoute(): string {
    const role = this.auth.role();
    return role === 'PLATFORM_ADMIN' || role === 'TENANT_ADMIN' ? '/admin' : '/supplier';
  }

  onSubmit() {
    if (!this.batchNumber || !this.mineralType || !this.originCountry || !this.mineSite || this.estimatedWeight == null) {
      return;
    }
    this.facade.createBatch({
      batchNumber: this.batchNumber,
      mineralType: this.mineralType,
      originCountry: this.originCountry,
      originMine: this.mineSite,
      weightKg: this.estimatedWeight,
    });
    this.router.navigate([this.returnRoute]);
  }

  onCancel() {
    this.router.navigate([this.returnRoute]);
  }
}
