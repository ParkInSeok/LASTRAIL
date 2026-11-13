import { DomainError } from "./domain-error";

export function assertSafeInteger(value: number, fieldName: string): number {
  if (!Number.isSafeInteger(value)) {
    throw new DomainError("DOMAIN_INTEGER_UNSAFE", `${fieldName} must be a JavaScript safe integer`);
  }
  return value;
}

export function assertNonNegativeInteger(value: number, fieldName: string): number {
  assertSafeInteger(value, fieldName);
  if (value < 0) {
    throw new DomainError("DOMAIN_INTEGER_NEGATIVE", `${fieldName} must be >= 0`);
  }
  return value;
}
