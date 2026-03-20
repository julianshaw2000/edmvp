import { z } from 'zod';
import { UserRole } from '../enums.js';

export const createUserSchema = z.object({
  email: z.string().email(),
  displayName: z.string().min(1),
  role: z.nativeEnum(UserRole),
});

export type CreateUserInput = z.infer<typeof createUserSchema>;
