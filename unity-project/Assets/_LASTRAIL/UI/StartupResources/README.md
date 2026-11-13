# Startup Resources

`download_surface_accent.png` is the unchanged built-in image generation output,
retained again under `Sources/download_surface_generated.png` for provenance.
It is a static industrial download accent, not an animated or filled progress meter.
The existing rail background, Common button/gauge sprites and status icons are reused.
Full resource paths and connection limits are in `../Style/lastrail-startup-resources.json`.

Use `StartupScreen.prefab` as a reusable portrait view. Subscribe the primary and
secondary action events before enabling interaction; retain the authoritative auth
state in the owner to route the action. Feed `RenderAuth` the existing display model,
or call `Render` with reported download bytes, an error or an update notice.
Render Waiting while an owner operation is outstanding, then its result state.
Same-phase renders do not unlock an already requested action. Owners must unsubscribe
and cancel their own work as appropriate; this view does not own network tasks.

No startup scene registration, authentication execution, content download producer,
version policy, Addressables package or transport is supplied here. Labels, version,
byte counts and progress are separate TMP/Image layers. Unknown totals hide the bar;
100 percent never synthesizes success. Retry's separate sprite is reserved only.
Details are plain text with ellipsis for overlong owner-provided notices.

QA PNGs are local actual-render fixtures, not product execution evidence. They and
Unity logs/test result XML are excluded from this change's Git staging; no video is
generated. Regenerate only this prefab with the Build Startup Resource Prefab menu.
