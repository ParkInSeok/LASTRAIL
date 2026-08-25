export type Ok<T> = { readonly ok: true; readonly value: T };
export type Err<E extends string = string> = { readonly ok: false; readonly error: E; readonly message: string };
export type Result<T, E extends string = string> = Ok<T> | Err<E>;

export function ok<T>(value: T): Ok<T> {
  return { ok: true, value };
}

export function err<E extends string>(error: E, message: string): Err<E> {
  return { ok: false, error, message };
}
