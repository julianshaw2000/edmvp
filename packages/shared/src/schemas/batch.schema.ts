import { z } from 'zod';

export const createBatchSchema = z.object({
  batchNumber: z.string().min(1),
  mineralType: z.string().min(1).default('tungsten'),
  originCountry: z.string().regex(/^[A-Z]{2}$/, 'Must be ISO alpha-2 country code'),
  originMine: z.string().min(1),
  weightKg: z.number().positive(),
});

export type CreateBatchInput = z.infer<typeof createBatchSchema>;
