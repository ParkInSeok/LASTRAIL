import { canonicalJson, sha256Hex } from '@lastrail/common';
import { CommanderError, parseCommanderReceipt, commanderSnapshotSchema, commanderSaveSchema, commanderActivateSchema, commanderKeySchema, commanderRanksSchema, normalizeCommanderRanks, commanderProgression, validateCommanderRanks, type CommanderSupport, type CommanderSave, type CommanderActivate, type CommanderReceipt } from '../../../packages/common/src/commander-traits';
import { readCommanderDefinition, commanderSupport } from './commander-definition';
export type CommanderPresetRow = {
    slot_id: 1 | 2 | 3;
    revision: number;
    rules_version: string;
    trait_definition_version: string;
    balance_version: number;
    ranks_json: string;
};
export type CommanderSelectionRow = {
    slot_id: 1 | 2 | 3;
    revision: number;
};
export type CommanderProfileRow = {
    commander_xp: number;
    commander_progression_revision: number;
};
async function readCommanderRows(db: D1Database, accountId: string) {
    const profile = await db.prepare('SELECT commander_xp,commander_progression_revision FROM player_profiles WHERE account_id=?').bind(accountId).first<CommanderProfileRow>();
    const presets = (await db.prepare('SELECT slot_id,revision,rules_version,trait_definition_version,balance_version,ranks_json FROM commander_trait_presets WHERE account_id=? ORDER BY slot_id').bind(accountId).all<CommanderPresetRow>()).results;
    const selection = await db.prepare('SELECT slot_id,revision FROM commander_trait_selections WHERE account_id=?').bind(accountId).first<CommanderSelectionRow>();
    return { profile, presets, selection };
}
export async function readCommanderAuthority(db: D1Database, accountId: string) {
    return { ...await readCommanderRows(db, accountId), pin: await readCommanderDefinition(db) };
}
export type CommanderAuthority = Awaited<ReturnType<typeof readCommanderAuthority>>;
export function commanderAuthorityGuards(db: D1Database, accountId: string, s: CommanderAuthority, slotId: 1 | 2 | 3): D1PreparedStatement[] {
    const p = s.presets.find(p => p.slot_id === slotId), v = s.selection;
    return [
        db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM player_profiles WHERE account_id=? AND commander_xp=? AND commander_progression_revision=?) THEN 1 ELSE 0 END').bind(accountId, s.profile!.commander_xp, s.profile!.commander_progression_revision),
        db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1 AND b.version=? AND b.config_json=?) THEN 1 ELSE 0 END').bind(s.pin!.balanceVersion, s.pin!.configJson),
        p ? db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM commander_trait_presets WHERE account_id=? AND slot_id=? AND revision=? AND rules_version=? AND trait_definition_version=? AND balance_version=? AND ranks_json=?) THEN 1 ELSE 0 END').bind(accountId, slotId, p.revision, p.rules_version, p.trait_definition_version, p.balance_version, p.ranks_json)
            : db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM commander_trait_presets WHERE account_id=? AND slot_id=?) THEN 1 ELSE 0 END').bind(accountId, slotId),
        v ? db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM commander_trait_selections WHERE account_id=? AND slot_id=? AND revision=?) THEN 1 ELSE 0 END').bind(accountId, v.slot_id, v.revision)
            : db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM commander_trait_selections WHERE account_id=?) THEN 1 ELSE 0 END').bind(accountId),
    ];
}
export function commanderStoredRanks(row: CommanderPresetRow): Record<string, number> {
    try {
        const input: unknown = JSON.parse(row.ranks_json);
        const ranks = commanderRanksSchema.parse(input);
        return normalizeCommanderRanks(ranks);
    }
    catch {
        throw new CommanderError(503, 'commander_state_unavailable');
    }
}
export async function readCommanderRunApplied(db: D1Database, accountId: string) {
    try {
        const row = await db.prepare("SELECT r.id,r.owner_account_id,s.snapshot_json FROM active_combat_runs r LEFT JOIN run_snapshots s ON s.run_id=r.id WHERE r.account_id=? AND r.status IN ('ACTIVE','FINISH_PENDING','QUARANTINED') ORDER BY r.started_at DESC,r.id LIMIT 1").bind(accountId).first<{
            id: string;
            owner_account_id: string;
            snapshot_json: string | null;
        }>();
        if (!row)
            return { status: 'NONE' as const };
        // Peer-specific START traits need an explicit per-account snapshot contract.
        // Never display the original owner's traits as the peer's applied traits.
        if (row.owner_account_id !== accountId)
            return { status: 'UNAVAILABLE' as const };
        if (row.snapshot_json === null)
            return { status: 'UNAVAILABLE' as const };
        const raw: unknown = JSON.parse(row.snapshot_json);
        if (!raw || typeof raw !== 'object' || Array.isArray(raw))
            throw Error();
        const snapshot = raw as Record<string, unknown>;
        const admission = snapshot.admission;
        const traits = Object.hasOwn(snapshot, 'commanderTraits') ? snapshot.commanderTraits : admission && typeof admission === 'object' && !Array.isArray(admission) ? (admission as Record<string, unknown>).commanderTraits : undefined;
        if (traits === undefined || traits === null)
            return { status: 'LEGACY_UNRECORDED' as const, runId: row.id };
        return { status: 'APPLIED' as const, runId: row.id, snapshot: commanderSnapshotSchema.parse(traits) };
    }
    catch {
        return { status: 'UNAVAILABLE' as const };
    }
}
export class CommanderPresetStore {
    private readonly support: CommanderSupport;
    constructor(private readonly db: D1Database, support: CommanderSupport = commanderSupport) { this.support = Object.freeze({ ...support }); }
    async readResult(accountId: string, key: string): Promise<CommanderReceipt | null> {
        if (!commanderKeySchema.safeParse(key).success)
            throw new CommanderError(400, 'idempotency_key_required');
        const row = await this.db.prepare('SELECT response_json FROM commander_trait_receipts WHERE account_id=? AND idempotency_key=?').bind(accountId, key).first<{
            response_json: string;
        }>();
        return row ? parseCommanderReceipt(row.response_json) : null;
    }
    async read(accountId: string) {
        const rows = await readCommanderRows(this.db, accountId);
        let pin: Awaited<ReturnType<typeof readCommanderDefinition>> = null;
        let definitionError: string | null = null;
        try {
            pin = await readCommanderDefinition(this.db);
        }
        catch (error) {
            if (!(error instanceof CommanderError))
                throw error;
            definitionError = error.code;
        }
        const s = { ...rows, pin };
        if (!s.profile)
            throw new CommanderError(503, 'commander_state_unavailable');
        const progression = s.pin ? commanderProgression(s.profile.commander_xp, s.profile.commander_progression_revision, s.pin.definition) : null;
        return { contractVersion: 'commander-s1-v2' as const, progression, rules: s.pin ? { rulesVersion: s.pin.definition.rulesVersion, traitDefinitionVersion: s.pin.definition.traitDefinitionVersion, balanceVersion: s.pin.balanceVersion, supportStatus: Object.entries(s.pin.definition.handlers).every(([id, v]) => this.support[id] === v) ? 'SUPPORTED' : 'PARTIAL_OR_UNSUPPORTED', supportedNodeIds: Object.keys(s.pin.definition.handlers).filter(id => this.support[id] === s.pin!.definition.handlers[id]) } : { supportStatus: 'UNAVAILABLE', errorCode: definitionError ?? 'commander_definition_unavailable' }, slots: ([1, 2, 3] as const).map(slotId => { const row = s.presets.find(p => p.slot_id === slotId); return { slotId, presetRevision: row?.revision ?? 0, ranks: row ? commanderStoredRanks(row) : {}, rulesVersion: row?.rules_version ?? null, traitDefinitionVersion: row?.trait_definition_version ?? null }; }), activeSelection: { slotId: s.selection?.slot_id ?? null, selectionRevision: s.selection?.revision ?? 0 }, currentRunApplied: await readCommanderRunApplied(this.db, accountId) };
    }
    save(accountId: string, command: CommanderSave, key: string) { return this.mutate(accountId, 'SAVE', command, key); }
    activate(accountId: string, command: CommanderActivate, key: string) { return this.mutate(accountId, 'ACTIVATE', command, key); }
    private async mutate(accountId: string, kind: 'SAVE' | 'ACTIVATE', input: CommanderSave | CommanderActivate, key: string): Promise<CommanderReceipt> {
        const parsed = (kind === 'SAVE' ? commanderSaveSchema : commanderActivateSchema).safeParse(input);
        if (!parsed.success)
            throw new CommanderError(400, 'contract_violation');
        if (!commanderKeySchema.safeParse(key).success)
            throw new CommanderError(400, 'idempotency_key_required');
        const command = parsed.data;
        // Validate node names against the request's definition before zero elision only on new execution.
        const hash = await sha256Hex(canonicalJson({ kind, ...command, ...('ranks' in command ? { ranks: normalizeCommanderRanks(command.ranks) } : {}) }));
        const replay = async () => { const r = await this.db.prepare('SELECT command_hash,response_json FROM commander_trait_receipts WHERE account_id=? AND idempotency_key=?').bind(accountId, key).first<{
            command_hash: string;
            response_json: string;
        }>(); if (!r)
            return null; if (r.command_hash !== hash)
            throw new CommanderError(409, 'idempotency_conflict'); return parseCommanderReceipt(r.response_json); };
        const prior = await replay();
        if (prior)
            return prior;
        try {
            const s = await readCommanderAuthority(this.db, accountId);
            if (!s.pin)
                throw new CommanderError(503, 'commander_definition_unavailable');
            if (!s.profile)
                throw new CommanderError(503, 'commander_state_unavailable');
            const d = s.pin.definition, p = s.presets.find(p => p.slot_id === command.slotId);
            if (d.rulesVersion !== command.rulesVersion || d.traitDefinitionVersion !== command.traitDefinitionVersion)
                throw new CommanderError(503, 'commander_definition_unsupported');
            if ((p?.revision ?? 0) !== command.expectedPresetRevision)
                throw new CommanderError(409, 'commander_preset_revision_conflict');
            if (s.profile.commander_progression_revision !== command.expectedProgressionRevision)
                throw new CommanderError(409, 'commander_progression_revision_conflict');
            if (kind === 'ACTIVATE' && (!p || (s.selection?.revision ?? 0) !== (command as CommanderActivate).expectedSelectionRevision))
                throw new CommanderError(409, 'commander_selection_revision_conflict');
            if (kind === 'ACTIVATE' && (p!.rules_version !== d.rulesVersion || p!.trait_definition_version !== d.traitDefinitionVersion))
                throw new CommanderError(503, 'commander_definition_unsupported');
            const ranks = kind === 'SAVE' ? (command as CommanderSave).ranks : commanderStoredRanks(p!);
            const validated = validateCommanderRanks(ranks, commanderProgression(s.profile.commander_xp, s.profile.commander_progression_revision, d), d, this.support);
            const encoded = canonicalJson(validated.ranks), changed = kind === 'SAVE' && (!p || p.ranks_json !== encoded || p.rules_version !== d.rulesVersion || p.trait_definition_version !== d.traitDefinitionVersion);
            const presetRevision = (p?.revision ?? 0) + (changed ? 1 : 0), selectionRevision = (s.selection?.revision ?? 0) + (kind === 'ACTIVATE' && s.selection?.slot_id !== command.slotId ? 1 : 0);
            if (!Number.isSafeInteger(presetRevision) || !Number.isSafeInteger(selectionRevision))
                throw new CommanderError(409, 'commander_state_changed');
            const now = Date.now(), mutationId = 'commander_' + crypto.randomUUID(), receipt: CommanderReceipt = { accepted: true, contractVersion: 'commander-s1-v2', command: kind, slotId: command.slotId, presetRevision, selectionRevision, receiptId: mutationId };
            const writes = commanderAuthorityGuards(this.db, accountId, s, command.slotId);
            writes.push(this.db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) SELECT ?,?,COALESCE(MAX(sequence),0)+1,'COMMANDER_TRAITS',?,? FROM account_mutations WHERE account_id=?").bind(mutationId, accountId, key, now, accountId));
            if (changed)
                writes.push(this.db.prepare('INSERT INTO commander_trait_presets VALUES(?,?,?,?,?,?,?,?) ON CONFLICT(account_id,slot_id) DO UPDATE SET revision=excluded.revision,rules_version=excluded.rules_version,trait_definition_version=excluded.trait_definition_version,balance_version=excluded.balance_version,ranks_json=excluded.ranks_json,updated_at=excluded.updated_at').bind(accountId, command.slotId, presetRevision, d.rulesVersion, d.traitDefinitionVersion, s.pin.balanceVersion, encoded, now));
            if (kind === 'ACTIVATE' && s.selection?.slot_id !== command.slotId)
                writes.push(this.db.prepare('INSERT INTO commander_trait_selections VALUES(?,?,?,?) ON CONFLICT(account_id) DO UPDATE SET slot_id=excluded.slot_id,revision=excluded.revision,updated_at=excluded.updated_at').bind(accountId, command.slotId, selectionRevision, now));
            writes.push(this.db.prepare('INSERT INTO commander_trait_receipts VALUES(?,?,?,?,?,?)').bind(accountId, key, hash, mutationId, JSON.stringify(receipt), now));
            await this.db.batch(writes);
            return receipt;
        }
        catch (error) {
            const committed = await replay();
            if (committed)
                return committed;
            if (error instanceof CommanderError)
                throw error;
            if (String(error).includes('commander_state_changed'))
                throw new CommanderError(409, 'commander_state_changed');
            throw new CommanderError(503, 'commander_state_unavailable');
        }
    }
}
