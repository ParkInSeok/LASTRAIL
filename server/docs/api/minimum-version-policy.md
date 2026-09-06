# Minimum supported app version

The DEV Admin version document at `versions/current.json` stores `version`,
`versionCode`, and `minimumVersionCode`. Codes are decimal integer strings,
compared numerically, in the range 1 through 2147483647.

An app code below `minimumVersionCode` requires an update; equality is supported.
The minimum must not exceed the current deployment's `versionCode`.
Raise it only when compatibility requires dropping older clients, not on every release.

Legacy documents without the minimum remain readable and are displayed as unset.
Every new export must explicitly include a valid minimum. A stale client omitting
the field is rejected so it cannot silently remove an existing policy.
Only a DEV OWNER can export this document.

Rollback must use a compatible minimum no higher than the rollback version code.
Lowering the minimum is an explicit operator decision; do not lower it automatically.
This policy is independent of retention: it does not delete old artifacts or data.
Version history and rollback execution are separate work; current export overwrites
the current document and does not provide an archive or database rollback.

This slice stores and displays the policy only. Game API admission checks and
Unity update prompts do not yet consume it; saving it does not enforce login blocking.
