# LASTRAIL local patch — 2026-10-22

Base: installed Unity 2D Animation 13.0.0, copied unchanged before the patch
from com.unity.2d.animation@cecbff528030. Upstream file hashes are preserved in
QA/OperatorInformation-20261019/BattleBuffer513/embedded-upstream-files.json
at the LASTRAIL project root. All upstream licenses and samples are preserved.

GpuDeformationSystem creates its static fallback after scene load even when
the lazy DeformationManager never exists. That path previously had no shutdown
release. CreateFallbackBuffer now idempotently registers its own existing
ClearFallbackBuffer on Application.quitting; clearing removes that subscription.
Instance cleanup remains valid and can clear first without a second release.

The official 13.0.6 tarball still contains this same unmatched lifetime path;
it was inspected, not installed. Remove this embedded override only after an
upstream owner-lifetime fix passes the no-manager Player shutdown reproduction
and Editor play/stop regression. Do not replace it with consumer-side reflection
release, a dummy SpriteSkin, warning suppression, or forced GC.

Baseline evidence: BattleBuffer515 (manager absent, buffer valid at quitting,
ComputeBuffer finalizer warning). Validation results belong in the existing
Unity handoff and QA verification record; this file does not claim completion.
