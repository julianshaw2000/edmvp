export interface TenantDto {
  id: string;
  name: string;
  status: 'ACTIVE' | 'SUSPENDED';
  userCount: number;
  batchCount: number;
  createdAt: string;
}

export interface CreateTenantRequest {
  name: string;
  adminEmail: string;
}

export interface PagedTenants {
  items: TenantDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}
