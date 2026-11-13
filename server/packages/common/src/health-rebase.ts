import { z } from 'zod';

const hp = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const maximum = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const policy = z.literal('health.max-hp-rebase.v1');

function project(anchorHp: number, anchorMaxHp: number, maxHp: number): number {
  if (anchorHp === 0) return 0;
  const scaled = Number(BigInt(anchorHp) * BigInt(maxHp) / BigInt(anchorMaxHp));
  return Math.min(maxHp, Math.max(1, scaled));
}

/** Explicit new-policy state only. Legacy actors must retain their original policy. */
export const healthRebaseSnapshotSchema = z.strictObject({ version: policy, hp, maxHp: maximum,
  anchorHp: hp, anchorMaxHp: maximum }).refine(state => state.anchorHp <= state.anchorMaxHp
    && state.hp <= state.maxHp && state.hp === project(state.anchorHp, state.anchorMaxHp, state.maxHp));
export type HealthRebaseSnapshot = z.infer<typeof healthRebaseSnapshotSchema>;

/** Owns the integer health anchor. Gameplay admission, final equipment stats, real HP
 * events, shields and recovery timers remain with their authoritative owners.
 * Compose changes on a restored candidate, then commit it with equipment and receipt;
 * never mutate the live object before its enclosing transaction succeeds. */
export class HealthRebaseState {
  private constructor(private state: HealthRebaseSnapshot) {}

  static create(raw: unknown): HealthRebaseState {
    const initial = z.strictObject({ policyVersion: policy, hp, maxHp: maximum })
      .refine(state => state.hp <= state.maxHp).parse(raw);
    return new HealthRebaseState({ version: initial.policyVersion, hp: initial.hp, maxHp: initial.maxHp,
      anchorHp: initial.hp, anchorMaxHp: initial.maxHp });
  }

  static restore(raw: unknown): HealthRebaseState {
    return new HealthRebaseState(healthRebaseSnapshotSchema.parse(raw));
  }

  snapshot(): HealthRebaseSnapshot { return { ...this.state }; }

  /** One call for the final eligible maximum; this result is not damage or healing. */
  rebaseMaximum(rawMaxHp: unknown): { kind: 'MAX_HP_REBASE'; hp: number; maxHp: number } {
    const maxHp = maximum.parse(rawMaxHp);
    const nextHp = project(this.state.anchorHp, this.state.anchorMaxHp, maxHp);
    this.state = { ...this.state, hp: nextHp, maxHp };
    return { kind: 'MAX_HP_REBASE', hp: nextHp, maxHp };
  }

  /** Accept the authoritative result after actual HP damage/heal/repair/recovery.
   * Shield-only absorption and zero effective HP changes must not reset the anchor. */
  recordActualHp(rawHp: unknown): void {
    const nextHp = hp.max(this.state.maxHp).parse(rawHp);
    if (nextHp === this.state.hp) return;
    this.state = { ...this.state, hp: nextHp, anchorHp: nextHp, anchorMaxHp: this.state.maxHp };
  }
}
