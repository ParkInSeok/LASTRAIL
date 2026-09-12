# Support route isolation verification - 2026-09-12

Production baseline b504132. No production component/client behavior changes in
this unit. Local harness mounts actual SupportAccountPage under MemoryRouter
Routes and QueryClientProvider, importing actual production CSS and API client.
It is a routed-page check, not only SupportPolicyReads component rendering.
It is NOT the complete App/bootstrap/Cloudflare Access chain.

Transport fixture accepts only the two expected GET path patterns, rejects all
other destinations/methods, and never falls back to network. Synthetic account
IDs and server-derived synthetic response fixture are not real account data.
QA buttons live outside main in this standalone script, not in the shipped UI.

Browser CUA execution at local scripts/support-route-qa.html:

1. Initial pending route renders account-selection state without requests.
2. A selected, both support/activity responses held: main header account_a,
   activity panel loading. Request trace records both A requests.
3. B selected before A completion: SYNTHETIC_account_b + B_SHOP visible, A_SHOP absent.
4. Release delayed A: trace records BOTH A responses after B; B header/B_SHOP
   unchanged, A_SHOP absent. Main action controls count0.
5. Select denied account (both GET403): account_denied header, activity panel
   Access/admin credential message; A_SHOP and B_SHOP absent. Screenshot inspected.
6. Partial unavailable account: shop displays source_read_failed / unavailable;
   pass actual0 XP/level, quest/mail records and independent availability preserved.
7. Revisit A then B: A only / B only matches selected account after cache population.

Legacy support fallback cards still say integration pending when overall support
read fails; top summary and new activity area show the classified error. No prior
account data appears. That older wording was not expanded/refactored in this QA unit.

RED: missing local fixture helper. GREEN: actual API client+QueryClient late-A
isolation and denied/partial/non-network test cases. Full npm test at14:00KST:
57files285PASS. npm run build: tsc+Vite PASS; existing >500kB warning unchanged.
Production artifacts remain index-CBMEwe49.js / index-M02gnw3l.css, same as baseline.
CUA tab closed; local Vite session stopped. No deployment/seed/auth/registry writes.

Remaining: actual authenticated deployed route smoke requires approved matching
Server/Admin rollout and existing authorized credentials. These mocked403 results
do not prove Cloudflare Access configuration or remote identity enforcement.
