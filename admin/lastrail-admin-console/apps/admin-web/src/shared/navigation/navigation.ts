import type { AdminPermission } from "@lastrail/admin-contracts";

export type NavigationItem = {
  label: string;
  path: string;
  permission?: AdminPermission;
};

export type NavigationGroup = {
  label: string;
  items: NavigationItem[];
};

const navigation: NavigationGroup[] = [
  {
    label: "Dashboard",
    items: [{ label: "Overview", path: "/" }]
  },
  {
    label: "Operations",
    items: [
      { label: "Players", path: "/players", permission: "ACCOUNT_READ" },
      { label: "Runs", path: "/runs", permission: "RUN_READ" },
      { label: "Integrity", path: "/integrity", permission: "INTEGRITY_READ" },
      { label: "의심 플레이어", path: "/integrity/suspicious-players", permission: "INTEGRITY_READ" },
      { label: "Incidents", path: "/incidents", permission: "INCIDENT_READ" },
      { label: "Enforcement", path: "/enforcement", permission: "ENFORCEMENT_SUSPEND" }
    ]
  },
  {
    label: "Insights",
    items: [{ label: "Analytics", path: "/analytics", permission: "ANALYTICS_READ" }]
  },
  {
    label: "System",
    items: [
      { label: "Game", path: "/game", permission: "BALANCE_READ" },
      { label: "Audit", path: "/audit", permission: "AUDIT_READ" }
    ]
  }
];

export function visibleNavigation(
  permissions: readonly AdminPermission[]
): NavigationGroup[] {
  return navigation
    .map((group) => ({
      ...group,
      items: group.items.filter(
        (item) => !item.permission || permissions.includes(item.permission)
      )
    }))
    .filter((group) => group.items.length > 0);
}
