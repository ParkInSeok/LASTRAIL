import { describe, it, expect } from 'vitest';
import definition from '../fixtures/commander-s1-test-only.json';
import { parseCommanderDefinition, commanderProgression, validateCommanderRanks } from '../../packages/common/src/commander-traits';
describe('S1 rules', () => {
    it('validates the approved43/106 and preserves XP above level cap', () => {
        const d = parseCommanderDefinition(definition);
        expect(d.nodes).toHaveLength(43);
        expect(d.rankRows).toHaveLength(106);
        expect(commanderProgression(0, 1, d)).toMatchObject({ level: 1, earnedPoints: 1 });
        expect(commanderProgression(999999, 2, d)).toMatchObject({ xp: 999999, level: 50, earnedPoints: 50 });
    });
    it('normalizes zero ranks and checks owner-independent budgets', () => {
        const d = parseCommanderDefinition(definition), p = commanderProgression(0, 1, d);
        expect(validateCommanderRanks({ TR01: 0 }, p, d, d.handlers)).toEqual({ ranks: {}, usedPoints: 0, unusedPoints: 1 });
        expect(validateCommanderRanks({ TR01: 1 }, p, d, d.handlers).usedPoints).toBe(1);
        expect(() => validateCommanderRanks({ TR01: 1, TR03: 1 }, p, d, d.handlers)).toThrow('commander_points_exceeded');
    });
    it('rejects missing parents, unsupported handlers, unknown nodes and cycles', () => {
        const d = parseCommanderDefinition(definition), p = commanderProgression(999999, 1, d);
        expect(() => validateCommanderRanks({ OC01: 1 }, p, d, d.handlers)).toThrow('commander_parent_required');
        expect(() => validateCommanderRanks({ TR01: 1 }, p, d, {})).toThrow('commander_definition_unsupported');
        expect(() => validateCommanderRanks({ bogus: 0 }, p, d, d.handlers)).toThrow('contract_violation');
        expect(() => parseCommanderDefinition({ ...definition, nodes: definition.nodes.map(n => n.id === 'TR01' ? { ...n, parents: ['TR01'] } : n) })).toThrow();
    });
});
it('enforces level25/40 capstone limits, parent OR and exact3P OC01 cost', () => {
    const d = parseCommanderDefinition(definition), ranks: Record<string, number> = {};
    const invest = (id: string) => { ranks[id] = 1; const parent = d.nodes.find(n => n.id === id)!.parents[0]; if (parent)
        invest(parent); };
    invest('OC01');
    const at = (level: number) => commanderProgression(d.levels[level - 1]!.cumulativeXp, 1, d);
    expect(() => validateCommanderRanks(ranks, at(24), d, d.handlers)).toThrow('commander_rank_locked');
    const result = validateCommanderRanks(ranks, at(25), d, d.handlers);
    expect(result.usedPoints).toBe(Object.keys(ranks).length + 2);
    expect(d.nodes.find(n => n.id === 'OC01')!.parents).toEqual(['X14', 'X16']);
    const other = d.nodes.find(n => n.kind === 'capstone' && n.id !== 'OC01')!;
    invest(other.id);
    expect(() => validateCommanderRanks(ranks, at(39), d, d.handlers)).toThrow('commander_capstone_limit');
    expect(validateCommanderRanks(ranks, at(40), d, d.handlers).usedPoints).toBeLessThanOrEqual(40);
});
it('rejects missing parents, duplicate ranks, wrong costs and unsafe definition numbers', () => {
    for (const change of [
        (d: typeof definition) => { d.nodes[0]!.parents = ['absent']; },
        (d: typeof definition) => { d.rankRows[1] = { ...d.rankRows[0]! }; },
        (d: typeof definition) => { d.rankRows[0]!.cost = 2; },
        (d: typeof definition) => { d.levels[49]!.cumulativeXp = Number.MAX_SAFE_INTEGER + 1; },
    ]) {
        const bad = structuredClone(definition);
        change(bad);
        expect(() => parseCommanderDefinition(bad)).toThrow('commander_definition_unsupported');
    }
});

it('rejects rank2 capstones even when the malformed definition keeps106 total rows',()=>{
 const bad=structuredClone(definition),cap=bad.nodes.find(n=>n.id==='OC01')!,normal=bad.nodes.find(n=>n.kind!=='capstone'&&n.maxRank>1)!;
 cap.maxRank=2;bad.rankRows=bad.rankRows.filter(r=>r.nodeId!==normal.id||r.rank!==normal.maxRank);normal.maxRank--;
 bad.rankRows.push({...bad.rankRows.find(r=>r.nodeId==='OC01')!,rank:2,totalNodeCost:6});expect(bad.rankRows).toHaveLength(106);expect(()=>parseCommanderDefinition(bad)).toThrow('commander_definition_unsupported');
});
