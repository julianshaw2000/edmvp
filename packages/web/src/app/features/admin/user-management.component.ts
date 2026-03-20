import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminFacade } from './admin.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { UserTableComponent } from './ui/user-table.component';
import { UserFormComponent } from './ui/user-form.component';
import { UserResponse, CreateUserRequest } from './data/admin.models';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, LoadingSpinnerComponent, UserTableComponent, UserFormComponent],
  template: `
    <app-page-header
      title="User Management"
      subtitle="Invite and manage platform users"
      actionLabel="Invite User"
      (actionClicked)="toggleInviteForm()"
    />

    @if (showInviteForm()) {
      <div class="bg-white rounded-xl shadow-sm border border-slate-200 p-6 mb-6">
        <h2 class="text-lg font-semibold text-slate-900 mb-4">Invite New User</h2>
        <app-user-form
          (submitted)="onInviteUser($event)"
          (cancelled)="showInviteForm.set(false)"
        />
        @if (facade.submitError()) {
          <p class="mt-3 text-sm text-red-600">{{ facade.submitError() }}</p>
        }
      </div>
    }

    <!-- Edit Role Modal -->
    @if (editingUser()) {
      <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center" (click)="editingUser.set(null)">
        <div class="bg-white rounded-xl shadow-lg p-6 w-96" (click)="$event.stopPropagation()">
          <h2 class="text-lg font-semibold text-slate-900 mb-4">Edit User Role</h2>
          <p class="text-sm text-slate-500 mb-1">{{ editingUser()!.displayName }}</p>
          <p class="text-sm text-slate-400 mb-4">{{ editingUser()!.email }}</p>
          <label class="block text-sm font-medium text-slate-700 mb-1">Role</label>
          <select
            [(ngModel)]="editRole"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 mb-4"
          >
            <option value="SUPPLIER">Supplier</option>
            <option value="BUYER">Buyer</option>
            <option value="PLATFORM_ADMIN">Platform Admin</option>
          </select>
          <div class="flex gap-3 justify-end">
            <button
              (click)="editingUser.set(null)"
              class="px-4 py-2 text-sm text-slate-600 hover:text-slate-800"
            >Cancel</button>
            <button
              (click)="saveRole()"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700"
            >Save</button>
          </div>
        </div>
      </div>
    }

    @if (facade.usersLoading()) {
      <app-loading-spinner />
    } @else if (facade.usersError()) {
      <div class="bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-sm text-red-700">{{ facade.usersError() }}</p>
      </div>
    } @else {
      <div class="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
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
  protected showInviteForm = signal(false);
  protected editingUser = signal<UserResponse | null>(null);
  protected editRole = '';

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
