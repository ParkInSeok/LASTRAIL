import { z } from "zod";
import { permissions, roles } from "./permissions";

export {
  hasPermission,
  permissions,
  rolePermissions,
  roles
} from "./permissions";
export type { AdminIdentity, AdminPermission, AdminRole } from "./permissions";

export const AdminRoleSchema = z.enum(roles);
export const AdminPermissionSchema = z.enum(permissions);

export const AdminIdentitySchema = z.object({
  id: z.string().min(1),
  displayName: z.string().min(1),
  role: AdminRoleSchema,
  permissions: z.array(AdminPermissionSchema)
});

export const BootstrapResponseSchema = z.object({
  environment: z.enum(["local", "staging", "production"]),
  admin: AdminIdentitySchema
});

export const AccountStatusSchema = z.enum([
  "ACTIVE",
  "SUSPENDED",
  "BANNED",
  "DELETED"
]);

export const RiskLevelSchema = z.enum(["LOW", "MEDIUM", "HIGH", "CRITICAL"]);
export const ReviewStatusSchema = z.enum([
  "UNREVIEWED",
  "IN_REVIEW",
  "NEEDS_MORE_EVIDENCE",
  "CONFIRMED_VIOLATION",
  "CLEARED",
  "FALSE_POSITIVE",
  "LINKED_INCIDENT"
]);

export const PlayerSearchItemSchema = z.object({
  accountId: z.string().min(1),
  displayName: z.string().min(1),
  status: AccountStatusSchema,
  syncLevel: z.number().int().min(1).max(50),
  lastSeenAt: z.string().datetime().optional(),
  risk: RiskLevelSchema.optional(),
  reviewStatus: ReviewStatusSchema.optional()
});

export const PlayerSearchResponseSchema = z.object({
  items: z.array(PlayerSearchItemSchema),
  nextCursor: z.string().optional()
});

export const TrainingCoreSlotSchema = z.object({
  slot: z.number().int().min(1).max(5),
  operatorId: z.string().min(1),
  name: z.string().min(1),
  trainedLevel: z.number().int().min(1).max(50)
});

export const OperatorProgressionSchema = z.object({
  operatorId: z.string().min(1),
  name: z.string().min(1),
  effectiveLevel: z.number().int().min(1).max(50),
  levelSource: z.enum(["TRAINED", "SYNC"]),
  trainedLevel: z.number().int().min(1).max(50),
  skillTraining: z.number().int().min(0),
  promotion: z.string(),
  coreSlot: z.number().int().min(1).max(5).optional()
});

export const PlayerProgressionSchema = z.object({
  operatorExp: z.number().int().min(0),
  syncLevel: z.number().int().min(1).max(50),
  coreLevelSpread: z.number().int().min(0),
  trainingCore: z.array(TrainingCoreSlotSchema).length(5),
  operators: z.array(OperatorProgressionSchema)
});

export const PlayerDetailSchema = z.object({
  accountId: z.string().min(1),
  displayName: z.string().min(1),
  status: AccountStatusSchema,
  createdAt: z.string().datetime(),
  progression: PlayerProgressionSchema
});

export const CreatePlayerNoteRequestSchema = z.object({
  note: z.string().trim().min(1).max(1000)
});

export const CreatePlayerNoteResponseSchema = z.object({
  note: z.object({
    accountId: z.string().min(1),
    action: z.literal("PLAYER_NOTE_CREATE"),
    result: z.literal("SUCCESS"),
    requestId: z.string().min(1)
  })
});

export const RunSummarySchema = z.object({
  runId: z.string().min(1),
  accountId: z.string().min(1),
  startedAt: z.string().datetime(),
  completedAt: z.string().datetime().optional(),
  result: z.enum(["IN_PROGRESS", "CLEARED", "FAILED", "INVALIDATED"]),
  riskTier: z.enum(["I", "II", "III", "IV", "V"]),
  balanceVersion: z.string().min(1),
  validatorVersion: z.string().optional()
});

export const RunSearchResponseSchema = z.object({
  items: z.array(RunSummarySchema),
  nextCursor: z.string().optional()
});

export const RunOperatorSnapshotSchema = z.object({
  operatorId: z.string().min(1),
  effectiveLevel: z.number().int().min(1).max(50),
  levelSource: z.enum(["TRAINED", "SYNC"]),
  syncLevelAtStart: z.number().int().min(1).max(50),
  trainedLevelAtStart: z.number().int().min(1).max(50).optional(),
  skillTraining: z.number().int().min(0),
  promotion: z.string()
});

export const RunDetailSchema = RunSummarySchema.extend({
  currentActiveBalanceVersion: z.string().min(1),
  clientBuild: z.string().min(1),
  contentVersion: z.string().min(1),
  validatorRuntimeRevision: z.string().min(1),
  snapshot: z.object({
    operatorIds: z.array(z.string().min(1)).length(5),
    ultimateOperatorId: z.string().min(1),
    operators: z.array(RunOperatorSnapshotSchema).length(5)
  })
});

export const NormalizedErrorSchema = z.object({
  error: z.object({
    code: z.string().min(1),
    message: z.string().min(1),
    requestId: z.string().min(1)
  })
});

export type BootstrapResponse = z.infer<typeof BootstrapResponseSchema>;
export type CreatePlayerNoteRequest = z.infer<typeof CreatePlayerNoteRequestSchema>;
export type CreatePlayerNoteResponse = z.infer<typeof CreatePlayerNoteResponseSchema>;
export type PlayerSearchResponse = z.infer<typeof PlayerSearchResponseSchema>;
export type PlayerDetail = z.infer<typeof PlayerDetailSchema>;
export type RunSearchResponse = z.infer<typeof RunSearchResponseSchema>;
export type RunDetail = z.infer<typeof RunDetailSchema>;
export type NormalizedError = z.infer<typeof NormalizedErrorSchema>;
