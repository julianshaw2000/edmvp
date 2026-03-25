import { Injectable, inject } from '@angular/core';
import { AdminStore } from './admin.store';
import { CreateUserRequest } from './data/admin.models';
import { AuditLogFilters } from './data/audit-log.models';

@Injectable({ providedIn: 'root' })
export class AdminFacade {
  private store = inject(AdminStore);

  // Read-only signals
  readonly users = this.store.users;
  readonly usersLoading = this.store.usersLoading;
  readonly usersError = this.store.usersError;
  readonly totalUsers = this.store.totalUsers;

  readonly batches = this.store.batches;
  readonly batchesLoading = this.store.batchesLoading;
  readonly batchesError = this.store.batchesError;
  readonly totalBatches = this.store.totalBatches;

  readonly flaggedBatches = this.store.flaggedBatches;
  readonly totalComplianceFlags = this.store.totalComplianceFlags;

  readonly rmapUploading = this.store.rmapUploading;
  readonly rmapUploadSuccess = this.store.rmapUploadSuccess;
  readonly rmapUploadError = this.store.rmapUploadError;
  readonly smelters = this.store.smelters;
  readonly smeltersLoading = this.store.smeltersLoading;

  readonly submitting = this.store.submitting;
  readonly submitError = this.store.submitError;

  // Audit log signals
  readonly auditLogs = this.store.auditLogs;
  readonly auditLogsTotalCount = this.store.auditLogsTotalCount;
  readonly auditLogsPage = this.store.auditLogsPage;
  readonly auditLogsPageSize = this.store.auditLogsPageSize;
  readonly auditLogsLoading = this.store.auditLogsLoading;
  readonly auditLogsError = this.store.auditLogsError;
  readonly auditLogsTotalPages = this.store.auditLogsTotalPages;

  // Actions
  loadUsers(tenantId?: string) { this.store.loadUsers(tenantId); }
  loadBatches() { this.store.loadBatches(); }

  inviteUser(req: CreateUserRequest) { this.store.inviteUser(req); }
  updateUser(id: string, data: Partial<CreateUserRequest>) { this.store.updateUser(id, data); }
  deactivateUser(id: string) { this.store.deactivateUser(id); }

  uploadRmapList(file: File) { this.store.uploadRmapList(file); }
  loadSmelters() { this.store.loadSmelters(); }

  loadAuditLogs(filters: AuditLogFilters) { this.store.loadAuditLogs(filters); }
}
