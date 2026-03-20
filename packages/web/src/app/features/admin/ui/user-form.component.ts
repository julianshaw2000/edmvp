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
      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="email">Email</label>
        <input
          id="email"
          type="email"
          [(ngModel)]="email"
          name="email"
          required
          placeholder="user@example.com"
          class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="displayName">Display Name</label>
        <input
          id="displayName"
          type="text"
          [(ngModel)]="displayName"
          name="displayName"
          required
          placeholder="Full name"
          class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="role">Role</label>
        <select
          id="role"
          [(ngModel)]="role"
          name="role"
          required
          class="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
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
          class="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          Invite User
        </button>
        <button
          type="button"
          (click)="cancelled.emit()"
          class="border border-slate-300 text-slate-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-slate-50 transition-colors"
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
