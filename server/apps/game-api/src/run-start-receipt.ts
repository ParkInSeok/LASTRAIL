/** The stored record supplies identity; an embedded response cannot select a different run. */
export function readStoredStartResponse(value: unknown, runId: string, previewId: string | null): Record<string, unknown> | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const response = value as Record<string, unknown>;
  return response.runId === runId && response.operationPreviewId === previewId ? response : null;
}
