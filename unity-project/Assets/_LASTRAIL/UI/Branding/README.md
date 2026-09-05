# Android Branding Candidate

Status: production candidate, NOT an approved LAST RAIL brand/logo.
No existing approved logo or launcher image was found. Existing title text and
StartupScreen are unchanged. One new textless locomotive source is reused for
all technical exports. The existing flat splash background (#21272A) is retained.

## Provenance

Source: Sources/rail_symbol_candidate_generated.png, built-in imagegen,
2026-09-05. Original RGBA PNG: 1254 x 1254, kept unchanged.
Prompt intent: front-facing armored locomotive over short rails, pale steel,
two cyan headlamps and amber center, bold small-size silhouette, no badge/text,
transparent background. This is generated candidate art, not a trademark claim.
An attempted padding edit produced an RGB checkerboard and was rejected.

## Rebuild

Unity 6000.3.22f1, Android Build Support installed:
`LAST RAIL > Resources > Build Android Branding Candidate`, or batchmode
`-executeMethod LastRail.Editor.AndroidBrandingBuilder.Build`.

The builder preserves source alpha, centers its nonzero-alpha bounds, fits all
alpha pixels within a radius of 0.27 of the adaptive canvas, and exports native
slot dimensions. Adaptive layers are Background (index 0), Foreground (index 1),
confirmed against the installed Android module's customLayerLabels.
Legacy/round exports use radius 0.43. Round corners are transparent; legacy is
opaque. Textures use uncompressed RGBA, no mipmaps, no NPOT resizing.

24 icon files: legacy and round each 192/144/96/72/48/36; adaptive background and
foreground each 432/324/216/162/108/81. One 512px transparent splash sprite.
The builder preserves other splash entries, background and Unity branding flags,
and replaces its own entry without duplicating it on subsequent runs.
Legacy/Round API deprecation warnings are expected for compatibility exports.

## Verification And Limits

AndroidBrandingTests checks persisted slot references, exact native dimensions,
adaptive layer ordering, opaque backgrounds, true foreground transparency and
66/108 safe-circle containment, plus splash sprite and retained Unity branding.
QA/Act1Launch/AndroidBranding contains circle/rounded-square mask previews at
48/72/96/256px. These simulate a centered 72/108 adaptive viewport (0.27 * 1.5
symbol radius), NOT OS launcher screenshots. Small sizes retain the train and
lights but lose fine grille detail. OEM motion/masks and real splash placement
still require device review. No APK build, signing, install or store upload.
Engine splash configuration is distinct from runtime StartupScreen UI.

References:
- https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html
- https://docs.unity3d.com/cn/6000.0/ScriptReference/PlayerSettings.SetPlatformIcons.html
