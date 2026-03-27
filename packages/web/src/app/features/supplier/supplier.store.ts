import { Injectable } from '@angular/core';
import { BatchStore } from '../../shared/state/batch.store';

/**
 * @deprecated Use BatchStore directly. Kept for backward compatibility.
 */
@Injectable({ providedIn: 'root' })
export class SupplierStore extends BatchStore {}
