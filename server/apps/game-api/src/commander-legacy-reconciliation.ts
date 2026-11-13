import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { CommanderError, parseCommanderReceipt, commanderRanksSchema, parseCommanderDefinition, commanderProgression, validateCommanderRanks, type CommanderDefinition, type CommanderSupport, type CommanderReceipt } from '../../../packages/common/src/commander-traits';
import { readCommanderAuthority, commanderAuthorityGuards, type CommanderPresetRow } from './commander-preset-store';
import { commanderSupport } from './commander-definition';
export type CommanderLegacyAdapter = {
    sourceVersion: string;
    definition: CommanderDefinition;
    cascadeRule: 'OR_DESCENDANTS';
    decode: (row: Readonly<CommanderPresetRow>) => unknown;
};
const decodedSchema = z.strictObject({ ranks: commanderRanksSchema, preferredSynergyTagId: z.string().nullable().optional() });
type MigrationReceipt = CommanderReceipt & {
    refundedPoints: number;
};
/** Internal only. No operational legacy adapter exists yet; never infer one from arbitrary JSON. */
export class CommanderLegacyReconciliation {
    private readonly adapters: readonly CommanderLegacyAdapter[];
    private readonly support: CommanderSupport;
    constructor(private readonly db: D1Database, adapters: readonly CommanderLegacyAdapter[] = [], support: CommanderSupport = commanderSupport) {
        this.adapters = adapters.map(a => ({ ...a, definition: parseCommanderDefinition(a.definition) }));
        this.support = Object.freeze({ ...support });
        if (new Set(adapters.map(a => a.sourceVersion)).size !== adapters.length)
            throw new CommanderError(503, 'commander_definition_unsupported');
    }
    async reconcile(accountId: string, slotId: 1 | 2 | 3, sourceVersion: string, targetVersion: string): Promise<MigrationReceipt | {
        status: 'NOT_APPLICABLE';
    }> {
        if (![1, 2, 3].includes(slotId) || sourceVersion === targetVersion)
            throw new CommanderError(400, 'contract_violation');
        const replay = async () => { const row = await this.db.prepare('SELECT r.response_json FROM commander_trait_migrations m JOIN commander_trait_receipts r ON r.account_id=m.account_id AND r.idempotency_key=m.receipt_key WHERE m.account_id=? AND m.slot_id=? AND m.source_definition_version=? AND m.target_definition_version=?').bind(accountId, slotId, sourceVersion, targetVersion).first<{
            response_json: string;
        }>(); if (!row)
            return null; const receipt = parseCommanderReceipt(row.response_json); if (receipt.command !== 'MIGRATE')
            throw new CommanderError(503, 'commander_state_unavailable'); return receipt; };
        const prior = await replay();
        if (prior)
            return prior;
        try {
            const s = await readCommanderAuthority(this.db, accountId), row = s.presets.find(p => p.slot_id === slotId);
            if (!row)
                return { status: 'NOT_APPLICABLE' };
            if (row.trait_definition_version === targetVersion)
                return { status: 'NOT_APPLICABLE' };
            const adapter = this.adapters.find(a => a.sourceVersion === sourceVersion);
            if (!adapter || adapter.definition.traitDefinitionVersion !== sourceVersion || row.trait_definition_version !== sourceVersion || adapter.cascadeRule !== 'OR_DESCENDANTS')
                throw new CommanderError(503, 'commander_definition_unsupported');
            if (!s.pin || s.pin.definition.traitDefinitionVersion !== targetVersion || !s.profile)
                throw new CommanderError(503, 'commander_definition_unavailable');
            const source = decodedSchema.parse(adapter.decode(Object.freeze({ ...row }))), old = adapter.definition, target = s.pin.definition;
            if (row.rules_version !== old.rulesVersion)
                throw new CommanderError(503, 'commander_definition_unsupported');
            // Historical validation checks investment, not permission to execute obsolete handlers.
            const before = validateCommanderRanks(source.ranks, commanderProgression(s.profile.commander_xp, s.profile.commander_progression_revision, old), old, old.handlers), ranks = { ...before.ranks };
            const oc = old.nodes.find(n => n.id === 'OC01'), ocRank = old.rankRows.find(r => r.nodeId === 'OC01' && r.rank === 1);
            if (!oc || oc.maxRank !== 1 || ocRank?.totalNodeCost !== 3)
                throw new CommanderError(503, 'commander_definition_unsupported');
            delete ranks.OC01;
            let changed = true;
            while (changed) {
                changed = false;
                for (const id of Object.keys(ranks)) {
                    const n = old.nodes.find(n => n.id === id)!;
                    if (n.parents.length && !n.parents.some(p => (ranks[p] ?? 0) > 0)) {
                        delete ranks[id];
                        changed = true;
                    }
                }
            }
            const removed = Object.keys(before.ranks).filter(id => !Object.hasOwn(ranks, id));
            const refund = removed.reduce((total, id) => total + old.rankRows.find(r => r.nodeId === id && r.rank === before.ranks[id])!.totalNodeCost, 0);
            const after = validateCommanderRanks(ranks, commanderProgression(s.profile.commander_xp, s.profile.commander_progression_revision, target), target, this.support);
            if (after.usedPoints !== before.usedPoints - refund)
                throw new CommanderError(503, 'commander_definition_unsupported');
            const hash = await sha256Hex(canonicalJson({ accountId, slotId, sourceVersion, targetVersion, beforeRevision: row.revision })), key = 'commander-migrate-' + hash, mutationId = 'commander_migration_' + crypto.randomUUID(), now = Date.now();
            if (row.revision >= Number.MAX_SAFE_INTEGER)
                throw new CommanderError(409, 'commander_state_changed');
            const receipt: MigrationReceipt = { accepted: true, contractVersion: 'commander-s1-v2', command: 'MIGRATE', slotId, presetRevision: row.revision + 1, selectionRevision: s.selection?.revision ?? 0, receiptId: mutationId, refundedPoints: refund };
            await this.db.batch([...commanderAuthorityGuards(this.db, accountId, s, slotId),
                this.db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) SELECT ?,?,COALESCE(MAX(sequence),0)+1,'COMMANDER_MIGRATION',?,? FROM account_mutations WHERE account_id=?").bind(mutationId, accountId, key, now, accountId),
                this.db.prepare('UPDATE commander_trait_presets SET revision=?,rules_version=?,trait_definition_version=?,balance_version=?,ranks_json=?,updated_at=? WHERE account_id=? AND slot_id=?').bind(receipt.presetRevision, target.rulesVersion, target.traitDefinitionVersion, s.pin.balanceVersion, canonicalJson(after.ranks), now, accountId, slotId),
                this.db.prepare('INSERT INTO commander_trait_receipts VALUES(?,?,?,?,?,?)').bind(accountId, key, hash, mutationId, JSON.stringify(receipt), now),
                this.db.prepare('INSERT INTO commander_trait_migrations VALUES(?,?,?,?,?,?,?,?)').bind(accountId, slotId, sourceVersion, targetVersion, row.revision, receipt.presetRevision, refund, key),
            ]);
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
