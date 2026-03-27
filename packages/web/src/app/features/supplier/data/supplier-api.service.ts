import { Injectable } from '@angular/core';
import { BatchApiService } from '../../../shared/services/batch-api.service';

/**
 * @deprecated Use BatchApiService directly. Kept for backward compatibility during refactoring.
 */
@Injectable({ providedIn: 'root' })
export class SupplierApiService extends BatchApiService {}
