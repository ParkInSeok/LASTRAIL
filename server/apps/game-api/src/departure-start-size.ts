import { ActivityError } from './activity-policy';

/** Match Unity's bounded START pending/journal envelope, including the pinned ordinary map. */
export function assertOrdinaryStartResponseSize(response: unknown) {
  const json = JSON.stringify(response);
  // Newtonsoft's default writer escapes these separators; JSON.stringify leaves them literal.
  const encodedLength = json === undefined ? Infinity : JSON.stringify(json).length + 5 * (json.match(/[\u0085\u2028\u2029]/gu)?.length ?? 0);
  if (json === undefined || new TextEncoder().encode(json).byteLength > 131072 || encodedLength > 131072)
    throw new ActivityError(409, 'ordinary_start_response_too_large');
}
