import { createRoot } from "react-dom/client";
import { SupportPolicyReads } from "../src/shared/support/SupportPolicyReads";
import { mapPlayerActivities } from "../src/shared/support/playerActivities";
import configured from "../src/shared/support/fixtures/configured-zero-and-owned-records-21dbd8f1-197f-4623-906e-b885dc205396.json";
createRoot(document.getElementById("root")!).render(<main style={{ maxWidth: 900, margin: "0 auto", fontFamily: "sans-serif", padding: 12 }}><h1>Local synthetic SQLite QA</h1><SupportPolicyReads state={{ kind: "ready", sections: mapPlayerActivities(configured, "a").sections }} /><SupportPolicyReads state={{ kind: "loading" }} /><SupportPolicyReads state={{ kind: "ready", sections: [] }} /><SupportPolicyReads state={{ kind: "ready", sections: [{ label: "Synthetic display only", fields: [{ label: "Reference", value: "local-reference-".repeat(10) }, { label: "Missing", value: null }, { label: "Count", value: 0 }] }] }} /></main>);
