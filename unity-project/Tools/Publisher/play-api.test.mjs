import test from 'node:test';
import assert from 'node:assert/strict';
import { inspect } from 'node:util';
import { PlayApi } from './play-api.mjs';

// Every transport dependency is synthetic; these tests never use global fetch.
const root = 'https://androidpublisher.googleapis.com/androidpublisher/v3/applications/app.lastrail.game.dev';
const release = { name: 'fixture', versionCodes: ['7'], status: 'completed' };
function fixture(response = () => Response.json({ fixture: true }), tokenProvider = () => 'fixture-token') {
  const calls = [];
  const api = new PlayApi({ tokenProvider, fetchImpl: async (url, init) => {
    calls.push({ url, ...init });
    return response();
  } });
  return { api, calls };
}

test('exports the planned PlayApi transport', () => {
  assert.equal(typeof PlayApi, 'function');
});

test('all operations use fixed DEV routes, methods and safe request bodies', async () => {
  const { api, calls } = fixture();
  const bytes = new Uint8Array([1, 2, 3]);
  const cases = [
    ['readCommittedReleases', [], 'GET', `${root}/tracks/internal/releases`, undefined],
    ['createEdit', [], 'POST', `${root}/edits`, {}],
    ['getEdit', ['edit_42-A'], 'GET', `${root}/edits/edit_42-A`, undefined],
    ['listTracks', ['edit_42-A'], 'GET', `${root}/edits/edit_42-A/tracks`, undefined],
    ['listBundles', ['edit_42-A'], 'GET', `${root}/edits/edit_42-A/bundles`, undefined],
    ['uploadBundle', ['edit_42-A', bytes], 'POST', 'https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications/app.lastrail.game.dev/edits/edit_42-A/bundles?uploadType=media', bytes],
    ['updateInternal', ['edit_42-A', release], 'PUT', `${root}/edits/edit_42-A/tracks/internal`, { track: 'internal', releases: [release] }],
    ['validateEdit', ['edit_42-A'], 'POST', `${root}/edits/edit_42-A:validate`, undefined],
    ['commitEdit', ['edit_42-A'], 'POST', `${root}/edits/edit_42-A:commit?changesInReviewBehavior=ERROR_IF_IN_REVIEW`, undefined],
  ];
  for (const [method, args, verb, url, body] of cases) {
    assert.deepEqual(await api[method](...args), { fixture: true });
    const request = calls.at(-1);
    assert.equal(request.url, url);
    assert.equal(request.method, verb);
    assert.equal(request.redirect, 'error');
    assert.equal(new Headers(request.headers).get('authorization'), 'Bearer fixture-token');
    if (body === bytes) {
      assert.deepEqual(request.body, bytes);
      assert.equal(new Headers(request.headers).get('content-type'), 'application/octet-stream');
    } else if (body !== undefined) {
      assert.deepEqual(JSON.parse(request.body), body);
      assert.equal(new Headers(request.headers).get('content-type'), 'application/json');
    } else assert.equal(request.body, undefined);
    assert.doesNotMatch(request.url + (typeof request.body === 'string' ? request.body : ''), /CANCEL|changesNotSentForReview/);
  }
  assert.equal(calls.length, cases.length);
});

test('readiness only reads committed releases', async () => {
  const { api, calls } = fixture();
  await api.readCommittedReleases();
  assert.equal(calls.length, 1);
  assert.equal(calls[0].method, 'GET');
  assert.equal(calls[0].url, `${root}/tracks/internal/releases`);
});

test('tokens are acquired per request and not retained on the adapter', async () => {
  let count = 0;
  const { api, calls } = fixture(undefined, () => `synthetic-${++count}`);
  await api.createEdit();
  await api.readCommittedReleases();
  assert.equal(new Headers(calls[0].headers).get('authorization'), 'Bearer synthetic-1');
  assert.equal(new Headers(calls[1].headers).get('authorization'), 'Bearer synthetic-2');
  assert.doesNotMatch(inspect(api, { showHidden: true }), /synthetic-[12]/);
});

test('missing, malformed and throwing token providers fail before fetch with redacted errors', async () => {
  for (const token of [undefined, null, '', ' ', 'Bearer fixture', 'fixture\r\nsecret', {}, 42]) {
    const { api, calls } = fixture(undefined, () => token);
    await assert.rejects(api.createEdit(), { code: 'AUTH_REQUIRED', status: 0 });
    assert.equal(calls.length, 0);
  }
  const { api, calls } = fixture(undefined, () => { throw new Error('PRIVATE_TOKEN_EXCEPTION'); });
  await assert.rejects(api.createEdit(), error => {
    assert.equal(error.code, 'AUTH_REQUIRED');
    assert.doesNotMatch(inspect(error), /PRIVATE_TOKEN_EXCEPTION/);
    assert.equal(error.cause, undefined);
    return true;
  });
  assert.equal(calls.length, 0);
});

test('every edit operation rejects path and query injection before requesting', async () => {
  const { api, calls } = fixture();
  for (const id of ['', '.', '..', '../production', 'a/b', 'a?x=y', 'a#x', '%2F', 'a b', '\\host', null, 123, 'a'.repeat(257)]) {
    for (const method of ['getEdit', 'listTracks', 'listBundles', 'uploadBundle', 'updateInternal', 'validateEdit', 'commitEdit']) {
      const arg = method === 'uploadBundle' ? new Uint8Array([1]) : release;
      await assert.rejects(async () => api[method](id, arg), { code: 'INVALID_EDIT_ID' });
    }
  }
  assert.equal(calls.length, 0);
});

test('updateInternal rejects production selectors, track objects and unknown fields', async () => {
  const { api, calls } = fixture();
  for (const input of [null, [], 'production', { ...release, track: 'production' },
    { ...release, packageName: 'app.lastrail.game' }, { ...release, target: 'PRD' },
    { track: 'internal', releases: [release] }, { ...release, changesInReviewBehavior: 'CANCEL_IN_REVIEW_AND_SUBMIT' },
    { ...release, toJSON: () => ({ track: 'production' }) }]) {
    await assert.rejects(async () => api.updateInternal('edit1', input), { code: 'INVALID_RELEASE' });
  }
  assert.equal(calls.length, 0);
});

test('upload rejects empty or nonbyte payloads before requesting', async () => {
  const { api, calls } = fixture();
  for (const bytes of ['', null, {}, new Uint8Array()]) {
    await assert.rejects(async () => api.uploadBundle('edit1', bytes), { code: 'INVALID_BUNDLE' });
  }
  assert.equal(calls.length, 0);
});

test('hidden serialization hooks cannot insert production selectors', async () => {
  const { api, calls } = fixture();
  const input = Object.defineProperty({ ...release }, 'toJSON', {
    value: () => ({ ...release, track: 'production' }),
  });
  await assert.rejects(async () => api.updateInternal('edit1', input), { code: 'INVALID_RELEASE' });
  assert.equal(calls.length, 0);
});

test('HTTP failures expose only bounded status/code and never read raw error bodies', async () => {
  for (const status of [400, 401, 403, 409, 429, 500, 503]) {
    let reads = 0;
    const { api, calls } = fixture(() => ({ status, ok: false, redirected: false,
      json() { reads++; throw new Error('PRIVATE_BODY'); },
      text() { reads++; return 'PRIVATE_BODY'; } }));
    await assert.rejects(api.commitEdit('edit1'), error => {
      assert.equal(error.status, status);
      assert.equal(error.code, 'HTTP_ERROR');
      assert.doesNotMatch(inspect(error), /PRIVATE_BODY|fixture-token/);
      return true;
    });
    assert.equal(reads, 0);
    assert.equal(calls.length, 1);
  }
});

test('redirects are refused even when an injected fetch returns one', async () => {
  for (const response of [() => new Response(null, { status: 302, headers: { location: 'https://example.invalid' } }),
    () => ({ status: 200, ok: true, redirected: true })]) {
    const { api, calls } = fixture(response);
    await assert.rejects(api.createEdit(), { code: 'REDIRECT_REFUSED' });
    assert.equal(calls.length, 1);
    assert.equal(calls[0].redirect, 'error');
  }
});

test('network and JSON exceptions are sanitized with no retries or attached causes', async () => {
  for (const [response, code] of [
    [() => { throw new Error('PRIVATE_NETWORK fixture-token'); }, 'TRANSPORT_ERROR'],
    [() => new Response('PRIVATE_BODY fixture-token', { status: 200 }), 'INVALID_RESPONSE'],
  ]) {
    const { api, calls } = fixture(response);
    await assert.rejects(api.createEdit(), error => {
      assert.equal(error.code, code);
      assert.doesNotMatch(inspect(error), /PRIVATE_|fixture-token/);
      assert.equal(error.cause, undefined);
      return true;
    });
    assert.equal(calls.length, 1);
  }
});

test('request deadlines use the default or validated custom timeout and fresh signals', async t => {
  const durations = [];
  const timeout = AbortSignal.timeout.bind(AbortSignal);
  t.mock.method(AbortSignal, 'timeout', milliseconds => {
    durations.push(milliseconds);
    return timeout(milliseconds);
  });
  const signals = [];
  for (const timeoutMs of [undefined, 1, 600000]) {
    const api = new PlayApi({ timeoutMs, tokenProvider: () => 'fixture-token',
      fetchImpl: async (_url, { signal }) => {
        assert.ok(signal instanceof AbortSignal);
        signals.push(signal);
        return Response.json({});
      } });
    await api.createEdit();
    await api.readCommittedReleases();
  }
  assert.deepEqual(durations, [120000, 120000, 1, 1, 600000, 600000]);
  assert.equal(new Set(signals).size, 6);
});

test('invalid timeout values are rejected before auth or fetch', () => {
  let calls = 0;
  for (const timeoutMs of [0, -1, 1.5, 600001, Infinity, NaN, '100', null]) {
    assert.throws(() => new PlayApi({ timeoutMs,
      tokenProvider: () => { calls++; return 'fixture-token'; },
      fetchImpl: () => { calls++; },
    }), { code: 'INVALID_TIMEOUT', status: 0 });
  }
  assert.equal(calls, 0);
});

test('timeout aborts fetch and response body reads with sanitized errors', async () => {
  for (const phase of ['fetch', 'body']) {
    let signal;
    let calls = 0;
    const waitForAbort = () => new Promise((_resolve, reject) => {
      const fail = () => reject(new Error('PRIVATE_ABORT fixture-token', { cause: signal.reason }));
      if (signal.aborted) fail();
      else signal.addEventListener('abort', fail, { once: true });
    });
    const api = new PlayApi({ timeoutMs: 5, tokenProvider: () => 'fixture-token',
      fetchImpl: async (_url, init) => {
        calls++;
        signal = init.signal;
        if (!(signal instanceof AbortSignal)) throw new Error('Missing signal');
        return phase === 'fetch' ? waitForAbort() : { status: 200, json: waitForAbort };
      } });
    // Node timeout signals are unref'ed; keep the test alive until the abort is observed.
    const keepAlive = setTimeout(() => {}, 1000);
    try {
      await assert.rejects(api.createEdit(), error => {
        assert.equal(error.code, phase === 'fetch' ? 'TRANSPORT_ERROR' : 'INVALID_RESPONSE');
        assert.doesNotMatch(inspect(error), /PRIVATE_ABORT|fixture-token/);
        assert.equal(error.cause, undefined);
        return true;
      });
      assert.ok(signal instanceof AbortSignal);
      assert.equal(signal.aborted, true);
      assert.equal(signal.reason.name, 'TimeoutError');
      assert.equal(calls, 1);
    } finally {
      clearTimeout(keepAlive);
    }
  }
});

test('upload snapshots typed array and Buffer views before asynchronous authentication', async () => {
  for (const source of [new Uint8Array([9, 1, 2, 3, 9]), Buffer.from([9, 1, 2, 3, 9])]) {
    let provideToken;
    const token = new Promise(resolve => { provideToken = resolve; });
    const { api, calls } = fixture(undefined, () => token);
    const pending = api.uploadBundle('edit1', source.subarray(1, 4));
    assert.equal(calls.length, 0);
    source.fill(7);
    provideToken('fixture-token');
    await pending;
    assert.deepEqual([...calls[0].body], [1, 2, 3]);
    assert.notEqual(calls[0].body.buffer, source.buffer);
  }
});
