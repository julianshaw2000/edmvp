import { Component, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CreateUserRequest } from '../data/admin.models';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form (ngSubmit)="onSubmit()" class="space-y-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-1.5" for="email">Email</label>
          <input
            id="email"
            type="email"
            [(ngModel)]="email"
            name="email"
            required
            placeholder="user@example.com"
            class="w-full border border-slate-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
          />
        </div>

        <div>
          <label class="block text-sm font-semibold text-slate-700 mb-1.5" for="displayName">Display Name</label>
          <input
            id="displayName"
            type="text"
            [(ngModel)]="displayName"
            name="displayName"
            required
            placeholder="Full name"
            class="w-full border border-slate-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 placeholder:text-slate-400 transition-shadow"
          />
        </div>
      </div>

      <div>
        <label class="block text-sm font-semibold text-slate-700 mb-1.5" for="role">Role</label>
        <select
          id="role"
          [(ngModel)]="role"
          name="role"
          required
          class="w-full border border-slate-300 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-shadow"
        >
          <option value="">Select a role</option>
          <option value="SUPPLIER">Supplier</option>
          <option value="BUYER">Buyer</option>
          <option value="PLATFORM_ADMIN">Platform Admin</option>
        </select>
      </div>

      <div class="flex gap-3 pt-2">
        <button
          type="submit"
          [disabled]="!email || !displayName || !role"
          class="bg-indigo-600 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm shadow-indigo-600/20 transition-all duration-150"
        >
          Send Invite
        </button>
        <button
          type="button"
          (click)="cancelled.emit()"
          class="border border-slate-300 text-slate-600 px-5 py-2.5 rounded-xl text-sm font-medium hover:bg-slate-50 transition-all duration-150"
        >
          Cancel
        </button>
      </div>
    </form>
  `,
})
export class UserFormComponent {
  submitted = output<CreateUserRequest>();
  cancelled = output<void>();

  email = '';
  displayName = '';
  role = '';

  onSubmit() {
    if (this.email && this.displayName && this.role) {
      this.submitted.emit({
        email: this.email,
        displayName: this.displayName,
        role: this.role,
      });
      this.email = '';
      this.displayName = '';
      this.role = '';
    }
  }
}
