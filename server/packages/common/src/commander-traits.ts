import { z } from 'zod';
const natural = z.number().int().nonnegative().safe();
const positive = z.number().int().positive().safe();
const id = z.string().regex(/^[A-Za-z0-9_-]{1,100}$/u);
export class CommanderError extends Error {
    constructor(public readonly status: 400 | 403 | 404 | 409 | 503, public readonly code: string) { super(code); }
}
export const commanderKeySchema = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
export const commanderRanksSchema = z.record(id, z.number().int().min(0).max(4));
const node = z.strictObject({ id, parents: z.array(id).max(43), maxRank: z.number().int().min(1).max(4), kind: z.enum(['existing-normal', 'new-normal', 'capstone']) });
const row = z.strictObject({ nodeId: id, rank: positive, cost: positive, totalNodeCost: positive, minimumEarnedPoints: positive, minimumLevel: positive });
const level = z.strictObject({ level: positive, totalPoints: positive, cumulativeXp: natural, capstoneLimit: natural });
const schema = z.strictObject({ rulesVersion: id, traitDefinitionVersion: id, sourceHash: z.string().regex(/^[a-f0-9]{64}$/u), nodes: z.array(node).length(43), rankRows: z.array(row).length(106), levels: z.array(level).length(50), handlers: z.record(id, id) });
export type Ranks = Readonly<Record<string, number>>;
export type CommanderDefinition = z.infer<typeof schema>;
export type CommanderSupport = Readonly<Record<string, string>>;
export type CommanderProgression = {
    xp: number;
    level: number;
    earnedPoints: number;
    revision: number;
};
export type CommanderPin = {
    balanceVersion: number;
    configJson: string;
    definition: CommanderDefinition;
};
const revision = z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER - 1);
const commandBase = { slotId: z.union([z.literal(1), z.literal(2), z.literal(3)]), expectedPresetRevision: revision, expectedProgressionRevision: positive, rulesVersion: id, traitDefinitionVersion: id };
export const commanderSaveSchema = z.strictObject({ ...commandBase, ranks: commanderRanksSchema });
export const commanderActivateSchema = z.strictObject({ ...commandBase, expectedSelectionRevision: revision });
export type CommanderSave = z.infer<typeof commanderSaveSchema>;
export type CommanderActivate = z.infer<typeof commanderActivateSchema>;
const receiptBase = { accepted: z.literal(true), contractVersion: z.literal('commander-s1-v2'), slotId: commandBase.slotId, presetRevision: positive, selectionRevision: natural, receiptId: z.string().min(1).max(128) };
const receiptSchema = z.discriminatedUnion('command', [
    z.strictObject({ ...receiptBase, command: z.literal('SAVE') }),
    z.strictObject({ ...receiptBase, command: z.literal('ACTIVATE') }),
    z.strictObject({ ...receiptBase, command: z.literal('MIGRATE'), refundedPoints: natural }),
]);
export type CommanderReceipt = z.infer<typeof receiptSchema>;
export function parseCommanderReceipt(encoded: string): CommanderReceipt {
    try {
        return receiptSchema.parse(JSON.parse(encoded));
    }
    catch {
        throw new CommanderError(503, 'commander_state_unavailable');
    }
}
export const commanderSnapshotSchema = z.strictObject({
    contractVersion: z.literal('commander-s1-v2'), rulesVersion: id, traitDefinitionVersion: id, balanceVersion: positive,
    definitionHash: z.string().regex(/^[a-f0-9]{64}$/u), sourceHash: z.string().regex(/^[a-f0-9]{64}$/u),
    xp: natural, level: z.number().int().min(1).max(50), earnedPoints: z.number().int().min(1).max(50), progressionRevision: positive,
    slotId: commandBase.slotId.nullable(), selectionRevision: natural, presetRevision: natural, ranks: commanderRanksSchema, effectHandlers: z.record(id, id),
}).superRefine((snapshot, context) => {
    if (snapshot.earnedPoints !== snapshot.level || Object.keys(snapshot.ranks).length !== Object.keys(snapshot.effectHandlers).length || Object.entries(snapshot.ranks).some(([key, rank]) => rank === 0 || !Object.hasOwn(snapshot.effectHandlers, key)) ||
        (snapshot.slotId === null ? (snapshot.selectionRevision !== 0 || snapshot.presetRevision !== 0 || Object.keys(snapshot.ranks).length !== 0) : (snapshot.selectionRevision === 0 || snapshot.presetRevision === 0)))
        context.addIssue({ code: 'custom', message: 'Invalid commander snapshot' });
});
export function parseCommanderDefinition(raw: unknown): CommanderDefinition {
    try {
        const d = schema.parse(raw), nodes = new Map(d.nodes.map(n => [n.id, n]));
        if (nodes.size !== 43 || Object.keys(d.handlers).length !== 43 || d.nodes.some(n => !Object.hasOwn(d.handlers, n.id)))
            throw Error();
        const visiting = new Set<string>(), done = new Set<string>();
        const visit = (key: string) => { if (visiting.has(key))
            throw Error(); if (done.has(key))
            return; const n = nodes.get(key); if (!n || new Set(n.parents).size !== n.parents.length)
            throw Error(); visiting.add(key); n.parents.forEach(visit); visiting.delete(key); done.add(key); };
        d.nodes.forEach(n => visit(n.id));
        for (const n of d.nodes) {
            if (n.kind === 'capstone' && n.maxRank !== 1) throw Error();
            const rows = d.rankRows.filter(r => r.nodeId === n.id).sort((a, b) => a.rank - b.rank);
            let total = 0;
            if (rows.length !== n.maxRank)
                throw Error();
            rows.forEach((r, i) => { total += r.cost; if (r.rank !== i + 1 || r.cost !== (n.kind === 'capstone' ? 3 : 1) || r.totalNodeCost !== total || r.minimumLevel > 50 || r.minimumEarnedPoints > 50)
                throw Error(); });
        }
        if (d.rankRows.some(r => !nodes.has(r.nodeId)))
            throw Error();
        d.levels.forEach((l, i) => { if (l.level !== i + 1 || l.totalPoints !== i + 1 || l.capstoneLimit !== (l.level < 25 ? 0 : l.level < 40 ? 1 : 2) || (i === 0 ? l.cumulativeXp !== 0 : l.cumulativeXp <= d.levels[i - 1]!.cumulativeXp))
            throw Error(); });
        return d;
    }
    catch {
        throw new CommanderError(503, 'commander_definition_unsupported');
    }
}
export function commanderProgression(xp: number, revisionValue: number, d: CommanderDefinition): CommanderProgression {
    if (!natural.safeParse(xp).success || !positive.safeParse(revisionValue).success)
        throw new CommanderError(503, 'commander_state_unavailable');
    const level = d.levels.filter(l => l.cumulativeXp <= xp).at(-1)!;
    return { xp, level: level.level, earnedPoints: level.totalPoints, revision: revisionValue };
}
export function normalizeCommanderRanks(input: unknown): Record<string, number> {
    const parsed = commanderRanksSchema.safeParse(input);
    if (!parsed.success)
        throw new CommanderError(400, 'contract_violation');
    return Object.fromEntries(Object.entries(parsed.data).filter(([, rank]) => rank > 0).sort(([a], [b]) => a.localeCompare(b)));
}
export function validateCommanderRanks(input: unknown, p: CommanderProgression, d: CommanderDefinition, s: CommanderSupport) {
    const parsed = commanderRanksSchema.safeParse(input);
    if (!parsed.success || Object.keys(parsed.data).some(id => !d.nodes.some(n => n.id === id)))
        throw new CommanderError(400, 'contract_violation');
    const ranks = normalizeCommanderRanks(parsed.data);
    let usedPoints = 0, capstones = 0;
    for (const [id, rank] of Object.entries(ranks)) {
        const n = d.nodes.find(n => n.id === id)!, r = d.rankRows.find(r => r.nodeId === id && r.rank === rank);
        if (!r || rank > n.maxRank || p.level < r.minimumLevel || p.earnedPoints < r.minimumEarnedPoints)
            throw new CommanderError(409, 'commander_rank_locked');
        if (n.parents.length && !n.parents.some(parent => (ranks[parent] ?? 0) > 0))
            throw new CommanderError(409, 'commander_parent_required');
        usedPoints += r.totalNodeCost;
        if (n.kind === 'capstone')
            capstones++;
    }
    if (capstones > d.levels[p.level - 1]!.capstoneLimit)
        throw new CommanderError(409, 'commander_capstone_limit');
    if (usedPoints > p.earnedPoints)
        throw new CommanderError(409, 'commander_points_exceeded');
    for (const id of Object.keys(ranks))
        if (!Object.hasOwn(s, id) || s[id] !== d.handlers[id])
            throw new CommanderError(503, 'commander_definition_unsupported');
    return { ranks, usedPoints, unusedPoints: p.earnedPoints - usedPoints };
}
