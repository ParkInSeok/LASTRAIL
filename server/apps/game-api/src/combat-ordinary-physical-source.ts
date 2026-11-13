/** Curated from measured EngineConsist796 and FiniteRail795; asset references, not runtime readiness. */
export function captureOrdinaryTrialPhysicalDefinition() {
  return {
  "version": "ordinary.trial-consist-authoring.v1.20261027",
  "definitionHash": "c2e5e813adfc3fb74755124c7aed8d17de0b79002a6ae82ec7509cb7cb83f497",
  "selectedLoadout": {
    "carriages": [
      {
        "carriageId": "car_combat",
        "slot": 0
      },
      {
        "carriageId": "car_gun",
        "slot": 1
      }
    ],
    "rulesVersion": "train-loadout-v1",
    "trainId": "train_a"
  },
  "totalLengthMm": 38000,
  "initialTailArcMm": 20000,
  "initialFrontArcMm": 58000,
  "vehicles": [
    {
      "role": "Engine",
      "selectionId": "train_a",
      "sourceSlot": null,
      "assetPath": "Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/Engine.prefab",
      "assetHash": "27de5aef302ca4bcb96c9848ff0fb8464295769b45d6a03b15211b8839714b5a",
      "bodyLengthMm": 12000,
      "gapAfterMm": 1000
    },
    {
      "role": "Combat",
      "selectionId": "car_combat",
      "sourceSlot": 0,
      "assetPath": "Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/Combat.prefab",
      "assetHash": "789916a42cb9411bb1df18fcfa9acd4c4af532a454b55febba43e80894f27f05",
      "bodyLengthMm": 12000,
      "gapAfterMm": 1000
    },
    {
      "role": "Gun",
      "selectionId": "car_gun",
      "sourceSlot": 1,
      "assetPath": "Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/Gun.prefab",
      "assetHash": "e666a32fa6da06bc65914d188aba18948e64898eb96df988a706beeac28312e8",
      "bodyLengthMm": 12000,
      "gapAfterMm": 0
    }
  ],
  "rail": {
    "version": "ordinary.finite-rail-authoring.v1.20261027",
    "definitionHash": "e63bbc228c1f8c505e6dee6c3ef6dd5c054e1ef2034603bc8e23221f65da47cf",
    "railSegmentId": "ordinary-first-straight-v1",
    "lengthMm": 500000.0,
    "millimetersPerUnit": 1000,
    "axis": "X_FORWARD_Y_UP_Z_SIDE",
    "startMm": [
      -25000.0,
      0.0,
      0.0
    ],
    "endMm": [
      475000.0,
      0.0,
      0.0
    ]
  }
} as const;
}
