# Tutorial Guide Resources

No new bitmap was needed. The guide reuses the Common tooltip panel and the existing
back-arrow/close icons at the paths in `../Style/lastrail-tutorial-resources.json`.
The back arrow is rotated for Next and for a non-interactive target pointer.
Highlight borders are ordinary thin Unity Image geometry, not a new artwork asset.
Existing selected-card art has inset decorative bars, so it is not stretched over targets.
Original PNGs are reused in place, unchanged; no external art or new generated source.

## Owner Contract

Instantiate `Assets/_LASTRAIL/Prefabs/Screens/TutorialGuide.prefab` as the last child
of an existing screen under its Canvas. The prefab has no Canvas, scene or startup hook.
Call `TutorialGuideView.Show` with the active screen Behaviour, actual device-safe
RectTransform, live target RectTransform, external step/string id, already resolved
plain text, optional Next/Close actions, and protected art/object RectTransforms.
Existing nonempty TMP and button bounds are also protected automatically.
Use the device safe rectangle, not an additional design-margin inset: those differ in
the existing combat screen. The owner must update that rectangle when device insets change.

The view tracks actual target/candidate bounds every LateUpdate in the shared Canvas.
Panel space is searched within the supplied safe area. No safe space, too-long text,
an unavailable owner/target, invisible target Graphic/CanvasGroup, RectMask2D clipping,
or a target outside the safe rectangle clears visuals and action delegates. It does
not advance, persist or restart a step. The owner can explicitly Show again later.
The current surface is sized for the project's 1080x1920 reference and 360x640 scaled
portrait output; safe widths below 964 reference units are conservatively refused.

This is a nonmodal guide. The target and outside screen remain interactive because
highlight/pointer Images do not receive raycasts. The panel receives raycasts to stop
click-through beneath its own background/controls. No global click blocker is installed.
Next/Close icons have 144-reference-unit hit targets; absent action owners disable them.
Actions clear the guide before invoking the owner, allowing an explicit next Show.
Repeated Show calls replace callbacks, not accumulate listeners. No target click is
automatically interpreted as tutorial completion. Pointer art is omitted if its nearby
positions are unsafe, while a safe highlight/panel may still be displayed.

## Connection Limits And QA

Only narrative tutorial-adjacent lines/string-id coverage were found, not a runtime
tutorial step producer, sequence controller or approved gameplay onboarding steps.
Those narrative lines are not converted into mandatory steps. This is a reusable view
and external callback adapter only; no existing screen is replaced or automatically
overlaid, no account progress, completion save, rewards or unlock logic is introduced.

QA explicitly supplies neutral button/operator-area/skill-icon labels and external
actions against actual StartupScreen and FullyAnimatedCombatHud prefab targets.
These are fixture steps, not proof of production onboarding. Screenshots are actual
Camera.Render at 1080x1920 and 360x640, not composite images. The original left-side
3/2 ally layout, right-side enemies and rail backgrounds are unchanged.
Screenshots/log/XML stay local under QA; no video is produced.
