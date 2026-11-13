/** Rank inputs are pinned in the departure snapshot. Missing uninvested ranks are zero. */
export function merchantRanksFromSnapshot(snapshotJson: string): { do03Rank: number; x08Rank: number } {
  const snapshot: unknown = JSON.parse(snapshotJson);
  if (typeof snapshot !== 'object' || snapshot === null || Array.isArray(snapshot)) throw new Error('merchant_snapshot_invalid');
  const admission = (snapshot as { admission?: unknown }).admission;
  if (typeof admission !== 'object' || admission === null || Array.isArray(admission)) throw new Error('merchant_admission_invalid');
  const traits = (admission as { commanderTraits?: unknown }).commanderTraits;
  if (traits !== null && traits !== undefined && (typeof traits !== 'object' || Array.isArray(traits))) {
    throw new Error('merchant_traits_invalid');
  }
  const ranks = traits === null || traits === undefined ? undefined : (traits as { ranks?: unknown }).ranks;
  if (ranks !== undefined && (typeof ranks !== 'object' || ranks === null || Array.isArray(ranks))) {
    throw new Error('merchant_ranks_invalid');
  }
  const values = ranks as Record<string, unknown> | undefined;
  const do03Rank = values?.DO03 === undefined ? 0 : values.DO03;
  const x08Rank = values?.X08 === undefined ? 0 : values.X08;
  if (typeof do03Rank !== 'number' || !Number.isInteger(do03Rank) || do03Rank < 0 || do03Rank > 4
    || typeof x08Rank !== 'number' || !Number.isInteger(x08Rank) || x08Rank < 0 || x08Rank > 2) {
    throw new Error('merchant_rank_invalid');
  }
  return { do03Rank, x08Rank };
}
