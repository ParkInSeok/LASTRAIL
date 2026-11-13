export const roles = ["OWNER", "OPERATOR", "VIEWER"] as const;

export const permissions = [
  "RUNTIME_POLICY_READ",
  "RUNTIME_POLICY_WRITE",
  "ACCOUNT_READ",
  "ACCOUNT_NOTE_WRITE",
  "RUN_READ",
  "ECONOMY_READ",
  "ECONOMY_GRANT",
  "ECONOMY_RESTORE",
  "INTEGRITY_READ",
  "INTEGRITY_RESOLVE",
  "CHEAT_REVIEW_READ",
  "CHEAT_REVIEW_ASSIGN",
  "CHEAT_REVIEW_RESOLVE",
  "ENFORCEMENT_SUSPEND",
  "ENFORCEMENT_BAN",
  "VALIDATOR_READ",
  "VALIDATOR_WRITE",
  "INCIDENT_READ",
  "INCIDENT_WRITE",
  "COMPENSATION_WRITE",
  "ANALYTICS_READ",
  "AUDIT_READ",
  "OFFLINE_REWARD_READ",
  "SUPPORT_READ",
  "SERVER_DEPLOYMENT_READ",
  "BALANCE_READ",
  "BALANCE_DRAFT_CREATE",
  "BALANCE_DRAFT_EDIT",
  "BALANCE_DRAFT_DISCARD",
  "BALANCE_VALIDATE",
  "BALANCE_PUBLISH",
  "BALANCE_ROLLBACK",
  "BALANCE_SIMULATION_READ",
  "BALANCE_SIMULATION_RUN",
  "BALANCE_SIMULATION_CANCEL"
] as const;

export type AdminRole = (typeof roles)[number];
export type AdminPermission = (typeof permissions)[number];

export type AdminIdentity = {
  id: string;
  displayName: string;
  role: AdminRole;
  permissions: AdminPermission[];
};

export const rolePermissions: Record<AdminRole, AdminPermission[]> = {
  OWNER: [...permissions],
  OPERATOR: [
    "RUNTIME_POLICY_READ",
    "ACCOUNT_READ",
    "ACCOUNT_NOTE_WRITE",
    "RUN_READ",
    "ECONOMY_READ",
    "INTEGRITY_READ",
    "CHEAT_REVIEW_READ",
    "CHEAT_REVIEW_ASSIGN",
    "CHEAT_REVIEW_RESOLVE",
    "VALIDATOR_READ",
    "INCIDENT_READ",
    "INCIDENT_WRITE",
    "ANALYTICS_READ",
    "AUDIT_READ",
    "OFFLINE_REWARD_READ",
    "SUPPORT_READ",
    "SERVER_DEPLOYMENT_READ",
    "BALANCE_READ",
    "BALANCE_SIMULATION_READ",
    "BALANCE_SIMULATION_RUN",
    "BALANCE_SIMULATION_CANCEL"
  ],
  VIEWER: [
    "RUNTIME_POLICY_READ",
    "ACCOUNT_READ",
    "RUN_READ",
    "ECONOMY_READ",
    "INTEGRITY_READ",
    "CHEAT_REVIEW_READ",
    "VALIDATOR_READ",
    "INCIDENT_READ",
    "ANALYTICS_READ",
    "AUDIT_READ",
    "OFFLINE_REWARD_READ",
    "SUPPORT_READ",
    "SERVER_DEPLOYMENT_READ",
    "BALANCE_READ",
    "BALANCE_SIMULATION_READ"
  ]
};

export function hasPermission(
  admin: Pick<AdminIdentity, "permissions">,
  permission: AdminPermission
): boolean {
  return admin.permissions.includes(permission);
}
