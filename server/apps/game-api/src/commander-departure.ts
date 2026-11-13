import { sha256Hex, canonicalJson } from '@lastrail/common';
import { CommanderError, commanderProgression, validateCommanderRanks, type CommanderSupport } from '../../../packages/common/src/commander-traits';
import { commanderSupport } from './commander-definition';
import { readCommanderAuthority, commanderAuthorityGuards, commanderStoredRanks } from './commander-preset-store';
import { ActivityError } from './activity-policy';
export async function readCommanderDeparture(db: D1Database, accountId: string, support: CommanderSupport = commanderSupport) {
    try {
        const authority = await readCommanderAuthority(db, accountId);
        if (!authority.pin) {
            if (authority.presets.length || authority.selection)
                throw new CommanderError(503, 'commander_definition_unavailable');
            const legacy = await db.prepare('SELECT b.version,b.config_json FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1').first<{
                version: number;
                config_json: string;
            }>();
            if (!legacy || Object.hasOwn(JSON.parse(legacy.config_json), 'commanderTraits'))
                throw new CommanderError(409, 'commander_state_changed');
            return { authority, legacy, snapshot: null };
        }
        const { pin, profile, selection, presets } = authority;
        if (!profile)
            throw new CommanderError(503, 'commander_state_unavailable');
        const preset = selection ? presets.find(p => p.slot_id === selection.slot_id) : undefined;
        if (selection && !preset)
            throw new CommanderError(503, 'commander_state_unavailable');
        const d = pin.definition;
        if (preset && (preset.rules_version !== d.rulesVersion || preset.trait_definition_version !== d.traitDefinitionVersion))
            throw new CommanderError(503, 'commander_definition_unsupported');
        const progression = commanderProgression(profile.commander_xp, profile.commander_progression_revision, d);
        const { ranks } = validateCommanderRanks(preset ? commanderStoredRanks(preset) : {}, progression, d, support);
        return { authority, legacy: null, snapshot: { contractVersion: 'commander-s1-v2' as const, rulesVersion: d.rulesVersion, traitDefinitionVersion: d.traitDefinitionVersion, balanceVersion: pin.balanceVersion, definitionHash: await sha256Hex(canonicalJson(d)), sourceHash: d.sourceHash, xp: progression.xp, level: progression.level, earnedPoints: progression.earnedPoints, progressionRevision: progression.revision, slotId: selection?.slot_id ?? null, selectionRevision: selection?.revision ?? 0, presetRevision: preset?.revision ?? 0, ranks, effectHandlers: Object.fromEntries(Object.keys(ranks).map(id => [id, d.handlers[id]!])) } };
    }
    catch (error) {
        if (error instanceof CommanderError)
            throw new ActivityError(error.status === 404 ? 503 : error.status, error.code);
        if (error instanceof ActivityError)
            throw error;
        throw new ActivityError(503, 'commander_state_unavailable');
    }
}
export type CommanderDepartureState = Awaited<ReturnType<typeof readCommanderDeparture>>;
export function commanderDepartureGuards(db: D1Database, accountId: string, state: CommanderDepartureState): D1PreparedStatement[] {
    if (state.legacy)
        return [db.prepare('INSERT INTO commander_trait_guard SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM commander_trait_presets WHERE account_id=?) AND NOT EXISTS(SELECT 1 FROM commander_trait_selections WHERE account_id=?) AND EXISTS(SELECT 1 FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1 AND b.version=? AND b.config_json=?) THEN 1 ELSE 0 END').bind(accountId, accountId, state.legacy.version, state.legacy.config_json)];
    return commanderAuthorityGuards(db, accountId, state.authority, state.authority.selection?.slot_id ?? 1);
}
