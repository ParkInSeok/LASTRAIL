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
    label: "현황",
    items: [{ label: "개요", path: "/" }]
  },
  {
    label: "운영",
    items: [
      { label: "플레이어", path: "/players", permission: "ACCOUNT_READ" },
      { label: "출격", path: "/runs", permission: "RUN_READ" },
      { label: "CS 지원", path: "/support/tickets", permission: "SUPPORT_READ" },
      { label: "요원 모집", path: "/gacha", permission: "ECONOMY_READ" },
      { label: "시스템 에러 로그", path: "/ops/error-logs", permission: "AUDIT_READ" },
      { label: "무결성", path: "/integrity", permission: "INTEGRITY_READ" },
      { label: "의심 플레이어", path: "/integrity/suspicious-players", permission: "INTEGRITY_READ" },
      { label: "사건", path: "/incidents", permission: "INCIDENT_READ" },
      { label: "제재", path: "/enforcement", permission: "ENFORCEMENT_SUSPEND" }
    ]
  },
  {
    label: "분석",
    items: [{ label: "지표", path: "/analytics", permission: "ANALYTICS_READ" }]
  },
  {
    label: "시스템",
    items: [
      { label: "게임 설정", path: "/game", permission: "BALANCE_READ" },
      { label: "버전 관리", path: "/versions", permission: "BALANCE_READ" },
      { label: "감사 로그", path: "/audit", permission: "AUDIT_READ" }
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

export function navigationItemUsesExactActiveMatch(
  path: string,
  items: readonly NavigationItem[]
): boolean {
  return items.some((item) => item.path !== path && item.path.startsWith(`${path}/`));
}
