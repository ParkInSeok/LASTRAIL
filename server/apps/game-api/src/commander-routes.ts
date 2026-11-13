import type { Context, Hono } from 'hono';
import { CommanderError, commanderSaveSchema, commanderActivateSchema, type CommanderSupport } from '../../../packages/common/src/commander-traits';
import { CommanderPresetStore } from './commander-preset-store';
import { commanderSupport } from './commander-definition';
import { readBoundedBody, type ActivityAuthResult } from './activity-routes';
import { ActivityError } from './activity-policy';
/** JSON.parse validates syntax; this scan also rejects duplicate object keys before Zod. */
function parseCommanderBody(raw: string): unknown {
    const value: unknown = JSON.parse(raw);
    const scopes: Array<Set<string> | null> = [];
    // Quoted strings are consumed as a whole, so braces inside them never open a scope.
    for (const token of raw.matchAll(/"(?:\\.|[^"\\])*"|[{}[\]]/gu)) {
        const text = token[0];
        if (text === '{') scopes.push(new Set());
        else if (text === '[') scopes.push(null);
        else if (text === '}' || text === ']') scopes.pop();
        else if (raw.slice(token.index + text.length).trimStart().startsWith(':')) {
            const keys = scopes.at(-1), key: string = JSON.parse(text);
            if (!keys || keys.has(key)) throw new CommanderError(400, 'contract_violation');
            keys.add(key);
        }
    }
    return value;
}
export function registerCommanderRoutes(app: Hono<{
    Bindings: Env;
}>, authenticate: (c: Context<{
    Bindings: Env;
}>) => Promise<ActivityAuthResult>, support: CommanderSupport = commanderSupport) {
    const handler = (mode: 'read' | 'save' | 'activate' | 'result') => async (c: Context<{
        Bindings: Env;
    }>) => {
        c.header('Cache-Control', 'no-store');
        try {
            const auth = await authenticate(c);
            if ('errorCode' in auth)
                return c.json({ error: auth.errorCode }, auth.status);
            if (new URL(c.req.url).searchParams.size)
                throw new CommanderError(400, 'contract_violation');
            const store = new CommanderPresetStore(c.env.DB, support), raw = await readBoundedBody(c);
            if (mode === 'read' || mode === 'result') {
                if (raw !== '')
                    throw new CommanderError(400, 'contract_violation');
                if (mode === 'read')
                    return c.json(await store.read(auth.accountId));
                const receipt = await store.readResult(auth.accountId, c.req.param('key') ?? '');
                if (!receipt)
                    throw new CommanderError(404, 'commander_result_not_found');
                return c.json(receipt);
            }
            const slot = c.req.param('slot');
            if (!slot || !/^[123]$/u.test(slot))
                throw new CommanderError(400, 'contract_violation');
            let input: unknown;
            try {
                input = parseCommanderBody(raw);
            }
            catch {
                throw new CommanderError(400, 'contract_violation');
            }
            const schema = mode === 'save' ? commanderSaveSchema.omit({ slotId: true }) : commanderActivateSchema.omit({ slotId: true });
            const parsed = schema.safeParse(input);
            if (!parsed.success)
                throw new CommanderError(400, 'contract_violation');
            const command = { ...parsed.data, slotId: Number(slot) }, key = c.req.header('Idempotency-Key') ?? '';
            return c.json(mode === 'save' ? await store.save(auth.accountId, commanderSaveSchema.parse(command), key) : await store.activate(auth.accountId, commanderActivateSchema.parse(command), key));
        }
        catch (error) {
            if (error instanceof CommanderError || error instanceof ActivityError)
                return c.json({ error: error.code }, error.status);
            return c.json({ error: 'commander_state_unavailable' }, 503);
        }
    };
    app.get('/v1/commander/traits', handler('read'));
    app.post('/v1/commander/traits/presets/:slot/save', handler('save'));
    app.post('/v1/commander/traits/presets/:slot/activate', handler('activate'));
    app.get('/v1/commander/traits/commands/:key', handler('result'));
}
