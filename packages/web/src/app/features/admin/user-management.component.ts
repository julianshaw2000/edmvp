import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminFacade } from './admin.facade';
import { AuthService } from '../../core/auth/auth.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { UserTableComponent } from './ui/user-table.component';
import { UserFormComponent } from './ui/user-form.component';
import { UserResponse, CreateUserRequest } from './data/admin.models';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent, UserTableComponent, UserFormComponent],
  template: `
    <a routerLink="/admin" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header
      title="User Management"
      subtitle="Invite and manage platform users"
      actionLabel="Invite User"
      (actionClicked)="toggleInviteForm()"
    />

    @if (showInviteForm()) {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
        <div class="flex items-center gap-3 mb-5">
          <div class="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center">
            <svg class="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
            </svg>
          </div>
          <h2 class="text-lg font-semibold text-slate-900">Invite New User</h2>
        </div>
        <app-user-form
          [roles]="availableRoles()"
          (submitted)="onInviteUser($event)"
          (cancelled)="showInviteForm.set(false)"
        />
        @if (facade.submitError()) {
          <div class="mt-4 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
            <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <p class="text-sm text-rose-700">{{ facade.submitError() }}</p>
          </div>
        }
      </div>
    }

    <!-- Edit Role Modal -->
    @if (editingUser()) {
      <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-center justify-center" (click)="editingUser.set(null)">
        <div class="bg-white rounded-2xl shadow-xl p-6 w-96 border border-slate-200" (click)="$event.stopPropagation()">
          <div class="flex items-center gap-3 mb-5">
            <div class="w-10 h-10 rounded-full bg-indigo-600 flex items-center justify-center text-sm font-bold text-white">
              {{ editingUser()!.displayName.charAt(0).toUpperCase() }}
            </div>
            <div>
              <p class="font-semibold text-slate-900">{{ editingUser()!.displayName }}</p>
              <p class="text-xs text-slate-500">{{ editingUser()!.email }}</p>
            </div>
          </div>
          <label class="block text-sm font-semibold text-slate-700 mb-1.5">Role</label>
          <select
            [(ngModel)]="editRole"
            class="w-full px-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 mb-5 transition-shadow"
          >
            @for (role of availableRoles(); track role) {
              <option [value]="role">{{ roleLabel(role) }}</option>
            }
          </select>
          <div class="flex gap-3 justify-end">
            <button
              (click)="editingUser.set(null)"
              class="px-4 py-2.5 text-sm font-medium text-slate-500 hover:text-slate-700 rounded-xl hover:bg-slate-100 transition-all duration-150"
            >Cancel</button>
            <button
              (click)="saveRole()"
              class="px-5 py-2.5 bg-indigo-600 text-white rounded-xl text-sm font-semibold hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150"
            >Save Changes</button>
          </div>
        </div>
      </div>
    }

    @if (facade.usersLoading()) {
      <app-loading-spinner />
    } @else if (facade.usersError()) {
      <div class="bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
        <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-sm text-rose-700">{{ facade.usersError() }}</p>
      </div>
    } @else {
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <app-user-table
          [users]="facade.users()"
          (editClicked)="onEditUser($event)"
          (deactivateClicked)="onDeactivateUser($event)"
        />
      </div>
    }
  `,
})
export class UserManagementComponent implements OnInit {
  protected facade = inject(AdminFacade);
  protected auth = inject(AuthService);
  protected showInviteForm = signal(false);
  protected editingUser = signal<UserResponse | null>(null);
  protected editRole = '';

  protected availableRoles = computed(() => {
    if (this.auth.role() === 'PLATFORM_ADMIN') {
      return ['SUPPLIER', 'BUYER', 'TENANT_ADMIN'];
    }
    return ['SUPPLIER', 'BUYER'];
  });

  protected roleLabel(role: string): string {
    switch (role) {
      case 'SUPPLIER': return 'Supplier';
      case 'BUYER': return 'Buyer';
      case 'TENANT_ADMIN': return 'Tenant Admin';
      default: return role;
    }
  }

  ngOnInit() {
    this.facade.loadUsers();
  }

  toggleInviteForm() {
    this.showInviteForm.update(v => !v);
  }

  onInviteUser(req: CreateUserRequest) {
    this.facade.inviteUser(req);
    this.showInviteForm.set(false);
  }

  onEditUser(user: UserResponse) {
    this.editingUser.set(user);
    this.editRole = user.role;
  }

  saveRole() {
    const user = this.editingUser();
    if (user && this.editRole !== user.role) {
      this.facade.updateUser(user.id, { role: this.editRole });
    }
    this.editingUser.set(null);
  }

  onDeactivateUser(user: UserResponse) {
    if (confirm(`Deactivate user ${user.displayName}?`)) {
      this.facade.deactivateUser(user.id);
    }
  }
}
