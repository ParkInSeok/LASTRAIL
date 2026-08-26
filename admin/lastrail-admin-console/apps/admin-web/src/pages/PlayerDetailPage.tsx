import { Link, useParams } from "react-router-dom";
import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { hasPermission } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";
import { useAdmin } from "../shared/auth/AdminContext";

export function PlayerDetailPage() {
  const { accountId = "" } = useParams();
  const { bootstrap } = useAdmin();
  const canWriteNote = bootstrap ? hasPermission(bootstrap.admin, "ACCOUNT_NOTE_WRITE") : false;
  const queryClient = useQueryClient();
  const [note, setNote] = useState("");
  const [lastNoteRequestId, setLastNoteRequestId] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ["player", accountId],
    queryFn: () => adminApi.playerDetail(accountId),
    enabled: accountId.length > 0
  });
  const notesQuery = useQuery({
    queryKey: ["player-notes", accountId],
    queryFn: () => adminApi.playerNotes(accountId),
    enabled: accountId.length > 0
  });
  const noteMutation = useMutation({
    mutationFn: () => adminApi.createPlayerNote(accountId, note),
    onSuccess: (result) => {
      setNote("");
      setLastNoteRequestId(result.note.requestId);
      void queryClient.invalidateQueries({ queryKey: ["player-notes", accountId] });
    }
  });

  function submitNote(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLastNoteRequestId(null);
    noteMutation.mutate();
  }

  if (query.isLoading) return <main className="content">Loading player</main>;
  if (query.error) return <main className="content error">Player request failed</main>;
  if (!query.data) return <main className="content">No player selected</main>;

  const player = query.data;

  return (
    <main className="content">
      <header>
        <h1>{player.displayName}</h1>
        <p>{player.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>Status</span>
          <strong>{player.status}</strong>
        </div>
        <div>
          <span>Operator EXP</span>
          <strong>{player.progression.operatorExp}</strong>
        </div>
        <div>
          <span>Sync Level</span>
          <strong>{player.progression.syncLevel}</strong>
        </div>
        <div>
          <span>Core Level Spread</span>
          <strong>{player.progression.coreLevelSpread}</strong>
        </div>
      </section>
      <section className="admin-panel">
        <h2>Operator Notes</h2>
        {canWriteNote ? (
          <form className="note-form" onSubmit={submitNote}>
            <label htmlFor="player-note">Note</label>
            <textarea
              id="player-note"
              maxLength={1000}
              minLength={1}
              required
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
            <div className="form-row">
              <small>{note.length}/1000</small>
              <button type="submit" disabled={noteMutation.isPending || note.trim().length === 0}>
                {noteMutation.isPending ? "Saving" : "Save note"}
              </button>
            </div>
            {lastNoteRequestId ? <p className="success">Note saved. Request {lastNoteRequestId}</p> : null}
            {noteMutation.error ? <p className="error">Note save failed</p> : null}
          </form>
        ) : null}
        <div className="note-history">
          <h3>Recent Notes</h3>
          {notesQuery.isLoading ? <p>Loading notes</p> : null}
          {notesQuery.error ? <p className="error">Note history failed</p> : null}
          {notesQuery.data?.items.length === 0 ? <p>No notes yet</p> : null}
          {notesQuery.data?.items.map((item) => (
            <article key={item.id}>
              <header>
                <strong>{item.authorDisplayName}</strong>
                <span>{new Date(item.createdAt).toLocaleString()}</span>
              </header>
              <p>{item.note}</p>
            </article>
          ))}
        </div>
      </section>
      <h2>Training Core</h2>
      <table>
        <thead>
          <tr>
            <th>Slot</th>
            <th>Operator</th>
            <th>Trained Level</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.trainingCore.map((core) => (
            <tr key={core.slot}>
              <td>{core.slot}</td>
              <td>{core.name}</td>
              <td>{core.trainedLevel}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <h2>Operators</h2>
      <table>
        <thead>
          <tr>
            <th>Operator</th>
            <th>Effective Level</th>
            <th>Level Source</th>
            <th>Trained Level</th>
            <th>Skill</th>
            <th>Promotion</th>
            <th>Core Slot</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.name}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{operator.levelSource}</td>
              <td>{operator.trainedLevel}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
              <td>{operator.coreSlot ?? "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <Link to={`/runs?accountId=${player.accountId}`}>View runs</Link>
    </main>
  );
}
