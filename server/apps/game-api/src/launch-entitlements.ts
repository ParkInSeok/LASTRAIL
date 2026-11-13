import { readCrystalPolicy } from "./crystal-wallet";

export type LaunchLine = {kind:string; id:string; quantity:number; source:string; launchSourceKey?:string};
export type LaunchSource = {key:string; quantity:number; rewardId:string};
const ticketAmounts: Record<string,number> = {opn_03_water_stop:1,opn_05_night_boarders:1,opn_06_broken_line:2,raid_raider_king_engine:3,launch_login_bonus_act1_v1:5};
const coreAmounts: Record<string,number> = {opn_06_broken_line:1,raid_raider_king_engine:2};

export async function prepareLaunchRewards(db:D1Database, accountId:string, operation:string, lines:LaunchLine[], convert:boolean) {
  const result:LaunchLine[] = [], sources:LaunchSource[] = [];
  let conversionPolicy: Awaited<ReturnType<typeof readCrystalPolicy>> | undefined;
  for (const line of lines) {
    const isTicket = line.id === "cur_command_ticket" || line.launchSourceKey?.endsWith(":cur_command_ticket");
    const limited = (line.source === "FIRST_CLEAR" || line.source === "LAUNCH_LOGIN_BONUS" || line.launchSourceKey !== undefined)
      && (isTicket || line.id === "cur_special_core");
    if (!limited) { result.push(line); continue; }
    const rewardId = isTicket ? "cur_command_ticket" : "cur_special_core";
    const quantity = (isTicket ? ticketAmounts : coreAmounts)[operation];
    if (!quantity) throw new Error("launch_source_unrecognized");
    const key = `${operation}:${rewardId}`;
    const consumed = await db.prepare(`SELECT source_key FROM account_launch_entitlements WHERE account_id = ? AND source_key = ?
      UNION ALL SELECT ? FROM account_first_clears f JOIN reward_transactions r ON r.run_id = f.run_id AND r.account_id = f.account_id,
      json_each(r.reward_json, '$.rewardLines') line
      WHERE f.account_id = ? AND f.operation_key = ? AND json_extract(line.value,'$.id') = ?
        AND json_extract(line.value,'$.source') = 'FIRST_CLEAR' AND json_extract(line.value,'$.quantity') > 0
      UNION ALL SELECT ? FROM account_login_bonus_claims WHERE account_id = ? AND bonus_id = ? LIMIT 1`)
      .bind(accountId,key,key,accountId,operation,rewardId,key,accountId,operation).first();
    if (consumed) continue;
    sources.push({key,quantity,rewardId});
    if (isTicket && convert) {
      const policy = conversionPolicy ??= await readCrystalPolicy(db);
      result.push({kind:"CRYSTAL",id:policy.currencyId,quantity:quantity*100,source:line.source,launchSourceKey:key});
    } else result.push({...line,quantity});
  }
  return {lines:result,sources,conversionPolicyVersion:conversionPolicy?.policyVersion};
}

export function launchEntitlementStatements(db:D1Database, accountId:string, sources:LaunchSource[], claimId:string, now:number) {
  return sources.map(source => db.prepare(`INSERT INTO account_launch_entitlements(account_id,source_key,reward_id,quantity,claim_id,created_at) VALUES(?,?,?,?,?,?)`)
    .bind(accountId,source.key,source.rewardId,source.quantity,claimId,now));
}
