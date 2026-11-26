# Homepage — implementation and handoff

Owner: this homepage task. Root: `D:/CodexData/Home/worktrees/0982/LASTRAIL`.
Git common directory: `D:/Project/LASTRAIL/.git`. No shared source changes.
Outputs: `homepage/`, `QA/Homepage/`. Process TEMP/TMP/TMPDIR: `D:/Project/LASTRAIL/LocalTemp`.

## Brief and decisions

Implement the explicitly authorized first reviewable local homepage, not public deployment. Authoritative specification: current `D:/Project/LASTRAIL/docs/superpowers/plans_categories/11_WEBSITE_COMMUNITY.md`, 12 service maintenance, 33 C01/C12/C17/C18/C20; index 00. Read current source, never copy old checkout planning over it.

No existing homepage found in checkout or active root; Admin remains separate and unchanged. Use native HTML/CSS/ES modules and Node's built-in local HTTP server/test runner: no package installation or production infrastructure. This is a frontend preview server, not a game/API server. MVC boundaries: presentation templates; controller owns browser navigation/listeners; domain owns status validation and display policy; read adapter owns data availability. No credentials or user data collected. Real contracts pending from Server_2.

Local default contains no released catalog, fake notices, probabilities, posts or service health. Unknown is visible. Routes: home, notices, wiki, probabilities, community, support, account, policies, development, status. QA-only `/__qa/` scenarios are local loopback-only fixtures. Full maintenance must render static HTML with JS disabled, 503 on replaced URLs, independent status 200. Game maintenance keeps reading; read-only marks blocked writes; stale normal never means currently healthy.

Design: dark industrial shell, warm paper reading panels, amber signals, original CSS rail diagram; no borrowed art, unverified characters, game screenshots or licensed assets. All text, state, controls and decoration remain independent. New design is reviewable, not a final approved artwork claim.

## Execution plan (inline)

- [ ] Domain: failing tests for independent maintenance surfaces, expiry, invalid payload and unknown state; implement pure validation/display policy.
- [ ] View/controller: responsive routes, working local navigation/search/filter, safe text escaping, unavailable write surfaces, semantic keyboard access.
- [ ] Preview: loopback-only serving allowlist, isolated QA scenarios, static fallback and real HTTP assertions.
- [ ] Review: desktop/mobile browser captures, route/interactions/overflow, tests, exact remaining contracts and coordinator report.

## Contract requests

Server_2 asked for actual Public Read routes/schema, MaintenanceStatus, browser session/CSRF, existing support contract. No guessed endpoint wired to production. Needs approved WEB+PUBLIC notice projection, public release index/catalog and policy-linked disclosure; community moderation/report/block and secure session before writes. Admin must own all operations. Direction art/license review required before hero/10-person art integration.

## Resource inventory

CSS rail diagram: original decorative local geometry, not gameplay or approved keyvisual. Wordmark: live text. Background, borders, labels, state badge, links and reading panels: independent HTML/CSS. Character art/screenshots/fonts/external media: not included pending approval. No reference composite shipped.

## Verification

Pending. This document tracks the sole current homepage handoff.
