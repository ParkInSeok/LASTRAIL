const zero = "0".repeat(64);
export function presentationFixture() {
  const bindings: Record<string, unknown>[] = [];
  const make = (name: string, assetType = "Texture2D") => {
    const bindingId = `binding.render.${name}`;
    bindings.push({ bindingId, actorOrRoleId: name, assetType, locator: { kind: "UNITY_ASSET_GUID", guid: (bindings.length + 1).toString(16).padStart(32, "0"), localFileId: "2800000" },
      assetRawSha256: zero, metaRawSha256: zero, dependencies: [], importValidation: { unityVersion: "test", importerVersion: "test", loadVerified: true } });
    return bindingId;
  };
  const roles = { configuration: make("configuration", "CombatPresentationAssets"), font: make("font", "TMP_FontAsset"),
    console: make("console"), icons: make("icons"), map: make("map"), trainExterior: make("trainExterior"), interior: make("interior"),
    defaultTurret: make("defaultTurret"), turretRig: make("turretRig"), buggy: make("buggy"), bike: make("bike"), hit: make("hit"), skill: make("skill"), shield: make("shield"),
    frame: make("frame", "Sprite"), primary: make("primary", "Sprite"), secondary: make("secondary", "Sprite"),
    actorAtlases: ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "enemy_raider_scout"].sort().map(actorId => ({ actorId, bindingId: make(actorId) })),
    frames: ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "enemy_raider_scout", "enemy_spike_cart_gunner", "enemy_railbike_marauder"].flatMap(actor =>
      ["idle", "attack", "hit", "down_or_defeated", "skill_or_special"].flatMap(state => [1, 2, 3, 4].map(i => ({ key: `${actor}/${state}/${i}`, bindingId: make(`${actor}.${state}.${i}`) })))).sort((a, b) => a.key < b.key ? -1 : 1) };
  bindings.sort((a, b) => String(a.bindingId) < String(b.bindingId) ? -1 : 1);
  return { schemaVersion: "ce03-presentation-manifest-v1", rendererVersion: "approved-combat-view-v1", roles, bindings,
    sourceFiles: ["approvedCombatView", "combatHudWidgets", "combatPresentationAssets", "combatWindowView"].map(logicalName => ({ logicalName, rawSha256: zero })) };
}
