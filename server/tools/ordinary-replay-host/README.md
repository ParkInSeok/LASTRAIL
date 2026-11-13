# Shared ordinary replay host

User correction BATTLE-NODE-RESTART-v1.20261028 takes precedence: leaving battle/relaunching starts a new attempt from that **node's original entry state**, not this intermediate checkpoint. This tool remains a same-attempt verification bootstrap only. Do not carry its old operations/prefix into a new attempt. Operational attempt-generation binding is not yet connected to this local tool.

Local server-owned execution bootstrap for ORDINARY-SHARED-COMBAT-VERIFIER-v1.20261028. It calls Unity's **`LastRail.Combat.Core.dll`**, compiled from the same 82 domain source files plus the shared QA probe. It contains no copied combat formulas and does not modify Unity source. Historical SharedReplay816 used the full Player runtime; SharedReplay817 uses the engine-free shared module.

The current host restores an ordinary preparation checkpoint against the original START and initialization receipt. Without a server-owned prefix, only zero operations are allowed. With a prefix, it verifies consecutive sequence, exact tick, origin and full command equality, executes PAUSE/SPEED/ULTIMATE_AUTO through the shared model, and compares the entire checkpoint. Other commands are rejected. It returns `REPLAYED_UNVERIFIED`, never completion permission. It is **not** an operating `FirstObjectiveEvidenceOwner`, an HTTP endpoint, or a source of accepted user inputs. Do not connect this result to COMPLETE as proof.

## Build and run

Use .NET 8 or later with the .NET 8 targeting pack. Build `OrdinaryReplayHost.csproj` with `SharedCoreDirectory` pointing to Unity's frozen shared build. The current validated input is `D:/Project/LASTRAIL/QA/CommanderS1-20260922/SharedCore816/build3`. This project references no Unity engine DLL.

Use the included `NuGet.Config` (no package sources), explicit D `BaseIntermediateOutputPath` and output paths. Set process-local TEMP/TMP/TMPDIR, DOTNET_CLI_HOME, APPDATA/LOCALAPPDATA and NUGET_PACKAGES to project-local D paths; no global environment changes are required. The existing project RTK wrapper needs `rtk bypass dotnet build` with quoted `-p:` arguments.

Run `dotnet OrdinaryReplayHost.dll <original-json> <checkpoint-json> <core-sha256> <newtonsoft-sha256> [server-prefix-json]`. Original JSON contains the existing server export's `accountId`, `runId`, `originalStart`, and `initializationResponse`; it must come from the server operator, never a client-selected path or upload. Pins must come from the trusted build, not the submitted report. Exit 0 means replay only; exit 1 returns a bounded error category without dumping inputs. The local input ceiling is 1 MiB per file, not a new gameplay limit.

Optional prefix format: `version: ordinary.replay-prefix.v1.20261028`, `accountId`, `runId`, `startSnapshotHash`, `configurationSha256`, and `inputs` containing `{sequence,tick,command:{kind,value}}`. Sequence starts at 1 without gaps; tick must equal the model's current logical tick. This host is still at tick 0. Prefix is a separate **trusted server-owner input**, not client evidence. Identical complete resubmission replays from the same initial state and returns the same checkpoint; different submitted input at the accepted position fails. Missing prefix/suffix fails without overwriting source files. No input persistence or acceptance endpoint is installed; `prefixConsistencyVerified` and `matchedAcceptedSequence` report comparison only, while `acceptedInputVerified` remains false.

`verify.mjs <host-dll> <original-json> <player-checkpoint-json> <qa-directory>` runs separate processes with a 15-second timeout and hidden Windows child windows. It compares actual Player state, rejects changed HP/account/schema/core pin and unaccepted operations, then checks synthetic preparation-control prefix consistency/duplicate request/time/sequence/origin conflicts. It records process timing and memory. QA pins are computed from the supplied build and are compatibility evidence, not production attestation.

Validated source: `QA/CommanderS1-20260922/StaticReference811/source.json`; actual Player state: `QA/CommanderS1-20260922/FivePlayer815/write3/cycle-0-state.json`. These are captured synthetic-account/internal-START artifacts, not product admission proof.

## Remaining boundary

The pinned shared factory has no ARRIVE/native geometry execution and uses a fixed internal seed of 1. This host does not substitute a seed or create accepted inputs. Server-issued seed, accepted input/time bounds, ARRIVE, geometry, actual actions/motion/hazard, terminal evidence and original-row transaction fences must be connected before authorization is possible. No new input transport, journal lifetime, deployment or host infrastructure is chosen here. Loading the shared core without Unity succeeds for this preparation path only. Three process measurements are not three gameplay runs or API/memory-leak measurements.
