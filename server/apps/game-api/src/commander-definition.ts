import { CommanderError, parseCommanderDefinition, type CommanderPin, type CommanderSupport } from '../../../packages/common/src/commander-traits';
/** No combat handlers have supplied a verified S1 binding yet. Never infer support from authoring text. */
export const commanderSupport: CommanderSupport = Object.freeze({});
export async function readCommanderDefinition(db: D1Database): Promise<CommanderPin | null> {
    try {
        const row = await db.prepare('SELECT b.version,b.config_json FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1').first<{
            version: number;
            config_json: string;
        }>();
        if (!row)
            throw new CommanderError(503, 'commander_definition_unavailable');
        const config: unknown = JSON.parse(row.config_json);
        if (typeof config !== 'object' || config === null || Array.isArray(config))
            throw new CommanderError(503, 'commander_definition_unavailable');
        if (!Object.hasOwn(config, 'commanderTraits'))
            return null;
        return { balanceVersion: row.version, configJson: row.config_json, definition: parseCommanderDefinition((config as Record<string, unknown>).commanderTraits) };
    }
    catch (error) {
        if (error instanceof CommanderError)
            throw error;
        throw new CommanderError(503, 'commander_definition_unavailable');
    }
}
