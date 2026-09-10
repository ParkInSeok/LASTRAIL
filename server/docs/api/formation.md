# S03 owned squad save and activation

Local code/document contract, migration0028 required, not deployed. Scope is five
operator IDs and their positions only. Train/equipment/package/ultimate selection,
coop readiness, run snapshots, preset deletion and optimal-power autofill remain
outside this implemented slice. The proposed three-slot product limit is not
silently activated. No resource cost, inventory grant or training-core replacement.

Both routes use existing session/account authentication and client admission.
Require an ASCII printable Idempotency-Key (1–128 chars), no query parameters, a
bounded JSON body (18KB maximum), strict fields. presetId is a stable client-chosen
ID matching `[A-Za-z0-9_-]{1,100}`; use distinct IDs for new presets. An ID already
owned by another account cannot be overwritten or read through these routes.

`POST /v1/formation/presets/:presetId/save`

```json
{"expectedRevision":0,"name":"Squad","operators":[{"slot":0,"operatorId":"op0"},{"slot":1,"operatorId":"op1"},{"slot":2,"operatorId":"op2"},{"slot":3,"operatorId":"op3"},{"slot":4,"operatorId":"op4"}]}
```

Revision0 creates; an update requires the exact current saved revision. Name is
trimmed,1–80 characters. Exactly five unique positions0–4 and five distinct owned
operators are required. Order in storage is canonical slot order. Server validates
ownership again in the committing transaction. Injected HP, levels, prices, account
IDs, role/partner data, training investments and unsupported selections are rejected.
Success includes accepted:true, receiptId:string, kind:"SAVE", presetId:string,
presetRevision:number, refreshRequired:true. Saving does not activate or modify an
existing active squad; preference saves are allowed during a run.

`POST /v1/formation/presets/:presetId/activate`

```json
{"expectedRevision":1,"expectedFormationRevision":0}
```

expectedRevision refers to the saved preset; expectedFormationRevision comes from
account-state.formation.revision (0 before activation). Activation validates the
versioned saved definition, current ownership, both revisions and absence of an
ACTIVE/FINISH_PENDING/QUARANTINED run, then copies the selection. The transaction
rechecks these predicates. Saving later edits cannot modify the active copy.
Success adds formationRevision:number, formationChanged:boolean, operators:array to
the common receipt fields. Identical preset/revision/selection activation leaves
formation revision unchanged and formationChanged=false; its request still receives
a durable receipt. No readiness changes or run start occur in this endpoint.

Read current state and saved revisions through GET /v1/account/state. All active
fields are nested under formation, as specified in account-state.md. Stored
definitionJson is a string containing `{schemaVersion:1,operators:[...]}`.
Legacy incompatible/malformed definitions remain readable, but cannot activate.
Explicitly save a valid owned selection with the existing revision to replace one.

## Errors and retry

400 contract_violation: malformed fields/key, duplicate/incomplete positions or IDs.
403 formation_operator_not_owned: missing actual ownership. Authentication codes
are inherited unchanged.409: preset_revision_conflict, preset_not_found,
preset_schema_unsupported, formation_revision_conflict, formation_open_run,
formation_write_conflict, idempotency_conflict. Unknown DB failures return500 and
leave the atomic transaction uncommitted.

Same account/key/action payload returns the stored receipt before stale revision
checks. Same key with changed payload or another action returns409. Concurrent
different writes cannot overwrite a newer revision. On transport uncertainty retry
the original payload/key; on409 refresh and preserve the editable draft. A new
intent after resolving a conflict needs the refreshed revision and a new key.
All selection writes, mutation sequence and receipt commit together; no partial
save/activation is reported successful. Refresh common state after accepted writes.

Eleven SQLite integration tests cover save-vs-activate separation, snapshot refresh,
invalid/foreign ownership and colliding IDs, stale and concurrent revisions, exact
replay, ownership/run-start races, unchanged activation, legacy definitions and
injected receipt-write rollback. They do not prove Unity/remote runtime behavior.
