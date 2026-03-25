import { Injectable, inject, signal, computed } from '@angular/core';
import { AdminApiService } from './data/admin-api.service';
import { UserResponse, CreateUserRequest, ComplianceFlagResponse, RmapSmelterResponse } from './data/admin.models';
import { BatchResponse } from '../supplier/data/supplier.models';
import { extractErrorMessage } from '../../shared/utils/error.utils';
import { AuditLogEntry, AuditLogFilters, PagedAuditLogs } from './data/audit-log.models';

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
  private _smelters = signal<RmapSmelterResponse[]>([]);
  private _smeltersLoading = signal(false);

  readonly rmapUploading = this._rmapUploading.asReadonly();
  readonly rmapUploadSuccess = this._rmapUploadSuccess.asReadonly();
  readonly rmapUploadError = this._rmapUploadError.asReadonly();
  readonly smelters = this._smelters.asReadonly();
  readonly smeltersLoading = this._smeltersLoading.asReadonly();

  // Audit logs
  private _auditLogs = signal<AuditLogEntry[]>([]);
  private _auditLogsTotalCount = signal(0);
  private _auditLogsPage = signal(1);
  private _auditLogsPageSize = signal(20);
  private _auditLogsLoading = signal(false);
  private _auditLogsError = signal<string | null>(null);

  readonly auditLogs = this._auditLogs.asReadonly();
  readonly auditLogsTotalCount = this._auditLogsTotalCount.asReadonly();
  readonly auditLogsPage = this._auditLogsPage.asReadonly();
  readonly auditLogsPageSize = this._auditLogsPageSize.asReadonly();
  readonly auditLogsLoading = this._auditLogsLoading.asReadonly();
  readonly auditLogsError = this._auditLogsError.asReadonly();
  readonly auditLogsTotalPages = computed(() =>
    Math.ceil(this._auditLogsTotalCount() / this._auditLogsPageSize())
  );

  // Submission state
  private _submitting = signal(false);
  private _submitError = signal<string | null>(null);

  readonly submitting = this._submitting.asReadonly();
  readonly submitError = this._submitError.asReadonly();

  loadUsers(tenantId?: string) {
    this._usersLoading.set(true);
    this._usersError.set(null);
    this.api.listUsers(tenantId).subscribe({
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
        this.loadSmelters();
      },
      error: (err) => {
        this._rmapUploadError.set(extractErrorMessage(err));
        this._rmapUploading.set(false);
      },
    });
  }

  loadSmelters() {
    this._smeltersLoading.set(true);
    this.api.listSmelters().subscribe({
      next: (res) => {
        this._smelters.set(res.smelters);
        this._smeltersLoading.set(false);
      },
      error: () => {
        this._smeltersLoading.set(false);
      },
    });
  }

  loadAuditLogs(filters: AuditLogFilters) {
    this._auditLogsLoading.set(true);
    this._auditLogsError.set(null);
    this._auditLogsPage.set(filters.page);
    this._auditLogsPageSize.set(filters.pageSize);
    this.api.getAuditLogs(filters).subscribe({
      next: (res) => {
        this._auditLogs.set(res.items);
        this._auditLogsTotalCount.set(res.totalCount);
        this._auditLogsLoading.set(false);
      },
      error: (err) => {
        this._auditLogsError.set(extractErrorMessage(err));
        this._auditLogsLoading.set(false);
      },
    });
  }
}
