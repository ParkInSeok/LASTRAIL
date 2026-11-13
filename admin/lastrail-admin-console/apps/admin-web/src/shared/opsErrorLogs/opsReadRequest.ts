export function opsReadRequest<T>(read: (refresh: boolean) => Promise<T>) {
  let force = false;
  return {
    query: () => { const refresh = force; force = false; return read(refresh); },
    refresh: (refetch: () => Promise<unknown>) => { force = true; return refetch(); },
  };
}
