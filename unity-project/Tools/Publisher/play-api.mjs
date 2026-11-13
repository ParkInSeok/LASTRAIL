const ORIGIN = 'https://androidpublisher.googleapis.com';
const APPLICATION = '/androidpublisher/v3/applications/app.lastrail.game.dev';
const RELEASE_FIELDS = new Set([
  'name', 'versionCodes', 'releaseNotes', 'status', 'userFraction',
  'countryTargeting', 'inAppUpdatePriority',
]);

function failure(code, status = 0) {
  const error = new Error(`Play API ${code}${status ? ` (${status})` : ''}`);
  error.name = 'PlayApiError';
  error.code = code;
  error.status = status;
  return error;
}

function editPath(editId) {
  if (typeof editId !== 'string' || !/^[A-Za-z0-9_-]{1,256}$/.test(editId)) {
    throw failure('INVALID_EDIT_ID');
  }
  return `/edits/${encodeURIComponent(editId)}`;
}

function internalBody(release) {
  try {
    if (!release || Object.getPrototypeOf(release) !== Object.prototype ||
        Object.getOwnPropertyNames(release).some(key => !RELEASE_FIELDS.has(key))) {
      throw failure('INVALID_RELEASE');
    }
    // Snapshot before acquiring credentials so callers cannot replace the payload mid-request.
    const value = JSON.parse(JSON.stringify(release));
    if (!Array.isArray(value.versionCodes) || value.versionCodes.length === 0 ||
        !value.versionCodes.every(code => typeof code === 'string' && /^[1-9][0-9]*$/.test(code)) ||
        !['draft', 'inProgress', 'halted', 'completed'].includes(value.status)) {
      throw failure('INVALID_RELEASE');
    }
    return JSON.stringify({ track: 'internal', releases: [value] });
  } catch {
    throw failure('INVALID_RELEASE');
  }
}

export class PlayApi {
  #tokenProvider;
  #fetch;
  #timeoutMs;

  constructor({ tokenProvider, fetchImpl = globalThis.fetch, timeoutMs = 120000 } = {}) {
    if (typeof tokenProvider !== 'function') throw failure('AUTH_REQUIRED');
    if (typeof fetchImpl !== 'function') throw failure('INVALID_TRANSPORT');
    if (!Number.isInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 600000) {
      throw failure('INVALID_TIMEOUT');
    }
    this.#tokenProvider = tokenProvider;
    this.#fetch = fetchImpl;
    this.#timeoutMs = timeoutMs;
  }

  readCommittedReleases() {
    return this.#request('GET', '/tracks/internal/releases');
  }

  createEdit() {
    return this.#request('POST', '/edits', '{}');
  }

  getEdit(editId) {
    return this.#request('GET', editPath(editId));
  }

  listTracks(editId) {
    return this.#request('GET', `${editPath(editId)}/tracks`);
  }

  listBundles(editId) {
    return this.#request('GET', `${editPath(editId)}/bundles`);
  }

  uploadBundle(editId, bytes) {
    const path = `${editPath(editId)}/bundles?uploadType=media`;
    if (!(bytes instanceof Uint8Array) || bytes.byteLength === 0) {
      throw failure('INVALID_BUNDLE');
    }
    return this.#request('POST', path, new Uint8Array(bytes), true);
  }

  updateInternal(editId, release) {
    const path = `${editPath(editId)}/tracks/internal`;
    return this.#request('PUT', path, internalBody(release));
  }

  validateEdit(editId) {
    return this.#request('POST', `${editPath(editId)}:validate`);
  }

  commitEdit(editId) {
    return this.#request('POST', `${editPath(editId)}:commit?changesInReviewBehavior=ERROR_IF_IN_REVIEW`);
  }

  async #request(method, path, body, media = false) {
    let token;
    try {
      token = await this.#tokenProvider();
      if (typeof token !== 'string' || token.length > 16384 ||
          !/^[A-Za-z0-9._~+/-]+=*$/.test(token)) throw failure('AUTH_REQUIRED');
    } catch {
      throw failure('AUTH_REQUIRED');
    }

    let response;
    try {
      const headers = { Authorization: `Bearer ${token}`, Accept: 'application/json' };
      if (body !== undefined) headers['Content-Type'] = media ? 'application/octet-stream' : 'application/json';
      response = await this.#fetch(`${ORIGIN}${media ? '/upload' : ''}${APPLICATION}${path}`, {
        method, headers, body, redirect: 'error', signal: AbortSignal.timeout(this.#timeoutMs),
      });
    } catch {
      throw failure('TRANSPORT_ERROR');
    } finally {
      token = undefined;
    }

    const status = Number.isInteger(response?.status) && response.status >= 100 && response.status <= 599
      ? response.status : 0;
    if (response?.redirected || (status >= 300 && status < 400)) {
      throw failure('REDIRECT_REFUSED', status);
    }
    // Error bodies may contain credentials or private payloads; never read or attach them.
    if (status < 200 || status >= 300) throw failure('HTTP_ERROR', status);
    try {
      return await response.json();
    } catch {
      throw failure('INVALID_RESPONSE', status);
    }
  }
}
