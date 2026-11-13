import { ActivityError } from "./activity-policy";

export type LogicalEvent<T> = { eventId: string; dueMs: number; sequence: number; payload: T };
/** Ordered deadlines, not a wall clock or a priority policy. Sequence comes from
 * the authority's original event order; arrival/frame order is never inferred. */
export class LogicalEventQueue<T> {
  private clockMs = 0;
  private closed = false;
  private readonly events: LogicalEvent<T>[] = [];
  private readonly usedSequences = new Set<number>();
  enqueue(event: LogicalEvent<T>) {
    if (!Number.isSafeInteger(event.dueMs) || event.dueMs < 0 || !Number.isSafeInteger(event.sequence) || event.sequence < 1)
      throw new ActivityError(400, "combat_logical_queue_order_invalid");
    if (event.dueMs < this.clockMs || (this.closed && event.dueMs === this.clockMs))
      throw new ActivityError(409, "combat_logical_queue_closed_tick");
    if (this.usedSequences.has(event.sequence)) throw new ActivityError(409, "combat_logical_queue_sequence_conflict");
    this.events.push(structuredClone(event)); this.usedSequences.add(event.sequence);
    this.events.sort((a, b) => a.dueMs - b.dueMs || a.sequence - b.sequence);
  }
  takeDue(untilMs: number): LogicalEvent<T> | null {
    this.checkAdvance(untilMs);
    const event = this.events[0];
    if (!event || event.dueMs > untilMs) return null;
    this.events.shift(); this.clockMs = event.dueMs; this.closed = false;
    return structuredClone(event);
  }
  finishAdvance(untilMs: number) { this.checkAdvance(untilMs); this.clockMs = untilMs; this.closed = true; }
  private checkAdvance(atMs: number) {
    if (!Number.isSafeInteger(atMs) || atMs < 0) throw new ActivityError(400, "combat_logical_queue_time_invalid");
    if (atMs < this.clockMs) throw new ActivityError(409, "combat_logical_queue_clock_rewind");
  }
  cancel(eventId: string) { const i = this.events.findIndex(e => e.eventId === eventId); if (i >= 0) this.events.splice(i, 1); }
  snapshot() { return { clockMs: this.clockMs, closed: this.closed, pending: this.events.map(e => structuredClone(e)),
    usedSequences: [...this.usedSequences].sort((a, b) => a - b) }; }
}
