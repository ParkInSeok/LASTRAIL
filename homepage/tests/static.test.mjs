import test from 'node:test';
import assert from 'node:assert/strict';
import {staticSurface} from '../src/static-status.mjs';
import {scenarioStatus} from '../qa/scenarios.mjs';
test('static QA categories explicitly disabled without JavaScript',()=>{const html=staticSurface('/wiki',scenarioStatus('game-maintenance'),'game-maintenance');assert.match(html,/<button disabled data-filter=/);assert.match(html,/정적 검증 화면/);});
test('maintenance navigation has no blocked notice destination',()=>{const html=staticSurface('/wiki',scenarioStatus('web-maintenance'),'web-maintenance');assert.doesNotMatch(html,/href="[^"]*\/notices"/);assert.match(html,/href="\/__qa\/web-maintenance\/status"/);});
