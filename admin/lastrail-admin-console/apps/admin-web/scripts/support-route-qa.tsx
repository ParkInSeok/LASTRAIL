import { useState } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { SupportAccountPage } from "../src/pages/SupportAccountPage";
import { createSupportRouteFixture } from "./support-route-fixtures";
import "../src/styles.css";

const client = new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } } });
function Harness() {
  const navigate = useNavigate();
  const [events, setEvents] = useState<string[]>([]);
  const [fixture] = useState(() => {
    const fixture = createSupportRouteFixture(event => setEvents(previous => [...previous, event]));
    window.fetch = fixture.fetch;
    return fixture;
  });
  return <>
    <aside aria-label="Local QA controls">
      <h1>Local synthetic route QA</h1>
      <button onClick={() => navigate("/support/accounts/account_a")}>A 요청 지연</button>
      <button onClick={() => navigate("/support/accounts/account_b")}>B 계정</button>
      <button onClick={() => fixture.releaseA()}>늦은 A 응답 완료</button>
      <button onClick={() => navigate("/support/accounts/account_denied")}>권한 거부 계정</button>
      <button onClick={() => navigate("/support/accounts/account_partial")}>부분 실패 계정</button>
      <output aria-label="Local request trace">{events.join(" · ")}</output>
    </aside>
    <Routes><Route path="/support/accounts/:accountId" element={<SupportAccountPage />} /></Routes>
  </>;
}
createRoot(document.getElementById("root")!).render(<QueryClientProvider client={client}><MemoryRouter initialEntries={["/support/accounts/pending"]}><Harness /></MemoryRouter></QueryClientProvider>);
