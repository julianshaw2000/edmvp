import { Injectable, inject, signal, computed } from '@angular/core';
import { AdminApiService } from './data/admin-api.service';
import { UserResponse, CreateUserRequest, ComplianceFlagResponse } from './data/admin.models';
import { BatchResponse } from '../supplier/data/supplier.models';
import { extractErrorMessage } from '../../shared/utils/error.utils';

@Injectable({ providedIn: 'root' })
export class AdminStore {
  private api = inject(AdminApiService);

  // Users
  private _users = signal<UserResponse[]>([]);
  private _usersLoading = signal(false);
  private _usersError = signal<string | null>(null);

  readonly users = this._users.asReadonly();
  readonly usersLoading = this._usersLoading.asReadonly();
  readonly usersError = this._usersError.asReadonly();
  readonly totalUsers = computed(() => this._users().length);

  // Batches
  private _batches = signal<BatchResponse[]>([]);
  private _batchesLoading = signal(false);
  private _batchesError = signal<string | null>(null);

  readonly batches = this._batches.asReadonly();
  readonly batchesLoading = this._batchesLoading.asReadonly();
  readonly batchesError = this._batchesError.asReadonly();
  readonly totalBatches = computed(() => this._batches().length);

  // Compliance flags — derived from batches with FLAGGED status
  readonly flaggedBatches = computed(() =>
    this._batches().filter(b => b.complianceStatus === 'FLAGGED')
  );
  readonly totalComplianceFlags = computed(() => this.flaggedBatches().length);

  // RMAP upload state
  private _rmapUploading = signal(false);
  private _rmapUploadSuccess = signal(false);
  private _rmapUploadError = signal<string | null>(null);

  readonly rmapUploading = this._rmapUploading.asReadonly();
  readonly rmapUploadSuccess = this._rmapUploadSuccess.asReadonly();
  readonly rmapUploadError = this._rmapUploadError.asReadonly();

  // Submission state
  private _submitting = signal(false);
  private _submitError = signal<string | null>(null);

  readonly submitting = this._submitting.asReadonly();
  readonly submitError = this._submitError.asReadonly();

  loadUsers() {
    this._usersLoading.set(true);
    this._usersError.set(null);
    this.api.listUsers().subscribe({
      next: (res) => {
        this._users.set(res.users);
        this._usersLoading.set(false);
      },
      error: (err) => {
        this._usersError.set(extractErrorMessage(err));
        this._usersLoading.set(false);
      },
    });
  }

  loadBatches() {
    this._batchesLoading.set(true);
    this._batchesError.set(null);
    this.api.listBatches().subscribe({
      next: (res) => {
        this._batches.set(res.items);
        this._batchesLoading.set(false);
      },
      error: (err) => {
        this._batchesError.set(extractErrorMessage(err));
        this._batchesLoading.set(false);
      },
    });
  }

  inviteUser(req: CreateUserRequest) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.createUser(req).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadUsers();
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  updateUser(id: string, data: Partial<CreateUserRequest>) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.updateUser(id, data).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadUsers();
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  deactivateUser(id: string) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.deleteUser(id).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadUsers();
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  uploadRmapList(file: File) {
    this._rmapUploading.set(true);
    this._rmapUploadSuccess.set(false);
    this._rmapUploadError.set(null);
    return this.api.uploadRmapList(file).subscribe({
      next: () => {
        this._rmapUploading.set(false);
        this._rmapUploadSuccess.set(true);
      },
      error: (err) => {
        this._rmapUploadError.set(extractErrorMessage(err));
        this._rmapUploading.set(false);
      },
    });
  }
}
