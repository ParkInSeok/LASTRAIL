import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import path from 'node:path';

// Local integration check against a real Player capture; not an API or combat approval.
const [dll, sourceFile, checkpointFile, outputDirectory] = process.argv.slice(2);
assert.ok(dll && sourceFile && checkpointFile && outputDirectory);
mkdirSync(outputDirectory, { recursive: true });
const hash = p => createHash('sha256').update(readFileSync(p)).digest('hex');
const runtimeHash = hash(path.join(path.dirname(dll), 'LastRail.Combat.Core.dll'));
const jsonHash = hash(path.join(path.dirname(dll), 'Newtonsoft.Json.dll'));
const invoke = (source = sourceFile, checkpoint = checkpointFile, runtime = runtimeHash, prefix) => {
  const started = performance.now();
  const child = spawnSync('dotnet', [dll, source, checkpoint, runtime, jsonHash, ...(prefix ? [prefix] : [])], {
    encoding: 'utf8', timeout: 15000, windowsHide: true, maxBuffer: 1024 * 1024,
  });
  assert.ifError(child.error);
  const result = JSON.parse(child.stdout);
  return { code: child.status, result, wallMs: performance.now() - started };
};
const first = invoke();
assert.equal(first.code, 0, JSON.stringify(first));
assert.equal(first.result.status, 'REPLAYED_UNVERIFIED');
assert.equal(first.result.completionAuthorized, false);
assert.equal(first.result.executionPhase, 'AWAITING_ENCOUNTER');
assert.equal(first.result.tick, 0);
assert.deepEqual(JSON.parse(first.result.checkpoint), JSON.parse(readFileSync(checkpointFile, 'utf8')));
const second = invoke(); // New OS process, same original state.
assert.equal(second.result.checkpoint, first.result.checkpoint);
assert.equal(second.result.checkpointSha256, first.result.checkpointSha256);
const third = invoke();
assert.equal(third.code, 0);
assert.equal(third.result.checkpoint, first.result.checkpoint);
const checks = ['actual_player_checkpoint', 'separate_process_exact_replay', 'unready_never_authorized'];
const changed = (name, value) => {
  const file = path.join(outputDirectory, name + '.json');
  writeFileSync(file, JSON.stringify(value));
  return file;
};
const hp = JSON.parse(first.result.checkpoint); hp.actors[0].vitals.hp = 1;
assert.notEqual(invoke(sourceFile, changed('tampered-hp', hp)).code, 0);
checks.push('hp_tampering_rejected');
const source = JSON.parse(readFileSync(sourceFile, 'utf8')); source.accountId = 'foreign-account';
assert.notEqual(invoke(changed('foreign-account', source)).code, 0);
checks.push('foreign_account_rejected');
assert.notEqual(invoke(sourceFile, checkpointFile, '0'.repeat(64)).code, 0);
checks.push('runtime_pin_mismatch_rejected');
const version = JSON.parse(first.result.checkpoint); version.schema = 'unsupported';
assert.notEqual(invoke(sourceFile, changed('unsupported-schema', version)).code, 0);
checks.push('unknown_schema_rejected');
const operations = JSON.parse(first.result.checkpoint);
operations.operations = [{ kind: 'PAUSE', value: true }];
assert.notEqual(invoke(sourceFile, changed('unaccepted-operations', operations)).code, 0);
checks.push('unaccepted_input_rejected');
// Prefix is supplied by the trusted local server caller, separate from the reported state.
const original = JSON.parse(readFileSync(sourceFile, 'utf8'));
const paused = JSON.parse(first.result.checkpoint);
paused.operations = [{ kind: 'PAUSE', value: true }, { kind: 'SPEED', value: 4 }, { kind: 'ULTIMATE_AUTO', value: true }];
paused.clock.paused = true; paused.clock.speed = 4; paused.ultimateAuto = true;
const pausedFile = changed('paused-checkpoint', paused);
const prefix = { version: 'ordinary.replay-prefix.v1.20261028', accountId: original.accountId, runId: original.runId,
  startSnapshotHash: createHash('sha256').update(JSON.stringify(original.originalStart)).digest('hex'),
  configurationSha256: paused.configurationSha256,
  inputs: paused.operations.map((command, i) => ({ sequence: i + 1, tick: 0, command })) };
const prefixFile = changed('server-prefix', prefix);
const accepted = invoke(sourceFile, pausedFile, runtimeHash, prefixFile);
assert.equal(accepted.code, 0, JSON.stringify(accepted));
assert.equal(accepted.result.matchedAcceptedSequence, 3);
assert.deepEqual(JSON.parse(accepted.result.checkpoint), paused);
assert.equal(invoke(sourceFile, pausedFile, runtimeHash, prefixFile).result.checkpoint, accepted.result.checkpoint);
checks.push('trusted_prefix_replayed_once_per_state', 'duplicate_request_same_state');
for (const [name, edit] of [
  ['missing_sequence', p => { p.inputs[1].sequence = 3; }],
  ['duplicate_sequence', p => { p.inputs[1].sequence = 1; }],
  ['future_tick', p => { p.inputs[0].tick = 1; }],
  ['foreign_prefix', p => { p.runId = 'other-run'; }],
  ['changed_input', p => { p.inputs[1].command.value = 2; }],
  ['changed_origin', p => { p.startSnapshotHash = '0'.repeat(64); }],
]) {
  const altered = structuredClone(prefix); edit(altered);
  assert.notEqual(invoke(sourceFile, pausedFile, runtimeHash, changed(name, altered)).code, 0, name);
  checks.push(name + '_rejected');
}
const unsupported = structuredClone(prefix), advanced = structuredClone(paused);
unsupported.inputs[0].command = { kind: 'ADVANCE', value: 10000000 };
advanced.operations[0] = unsupported.inputs[0].command;
assert.notEqual(invoke(sourceFile, changed('unsupported-command-state', advanced), runtimeHash,
  changed('unsupported-command-prefix', unsupported)).code, 0);
checks.push('unsupported_time_command_rejected');
const result = { checks, runtimeHash, jsonHash, sourceHash: hash(sourceFile), playerCheckpointHash: hash(checkpointFile),
  first, second, third, scope: 'REAL_PLAYER_PREPARATION_STATE_ONLY_NOT_COMBAT_OR_ADMISSION' };
writeFileSync(path.join(outputDirectory, 'verification.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({ passed: checks.length, scope: result.scope, first: first.wallMs, second: second.wallMs }));
