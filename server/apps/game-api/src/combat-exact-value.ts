import { z } from "zod";
export const exactCombatValueVersion = "combat.exact-rational.v1";
const MAX_DIGITS = 256;
const integerText = z.string().max(MAX_DIGITS).regex(/^(0|[1-9][0-9]*)$/);
export const exactCombatValueSchema = z.strictObject({ numerator: integerText, denominator: integerText.refine(s => s !== "0") });
export type ExactCombatSnapshot = z.infer<typeof exactCombatValueSchema>;
/** Immutable nonnegative rational. Storage limits reject overflow, never truncate or clamp. */
export class ExactCombatValue {
  private constructor(private readonly numerator: bigint, private readonly denominator: bigint) {}
  private static normalized(numerator: bigint, denominator: bigint): ExactCombatValue {
    let a = numerator, b = denominator;
    while (b !== 0n) { const remainder = a % b; a = b; b = remainder; }
    const n = numerator / a, d = denominator / a;
    if (n.toString().length > MAX_DIGITS || d.toString().length > MAX_DIGITS) throw new Error("combat_numeric_range_exceeded");
    return new ExactCombatValue(n, d);
  }
  static from(value: unknown) {
    const parsed = exactCombatValueSchema.parse(value);
    return ExactCombatValue.normalized(BigInt(parsed.numerator), BigInt(parsed.denominator));
  }
  add(other: ExactCombatValue) {
    return ExactCombatValue.normalized(this.numerator * other.denominator + other.numerator * this.denominator,
      this.denominator * other.denominator);
  }
  multiply(other: ExactCombatValue) {
    return ExactCombatValue.normalized(this.numerator * other.numerator, this.denominator * other.denominator);
  }
  floor(): string { return (this.numerator / this.denominator).toString(); }
  snapshot(): ExactCombatSnapshot { return { numerator: this.numerator.toString(), denominator: this.denominator.toString() }; }
}
