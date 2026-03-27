import { Injectable } from '@angular/core';
import { BatchFacade } from '../../shared/state/batch.facade';

/**
 * @deprecated Use BatchFacade directly. Kept for backward compatibility.
 */
@Injectable({ providedIn: 'root' })
export class SupplierFacade extends BatchFacade {}
