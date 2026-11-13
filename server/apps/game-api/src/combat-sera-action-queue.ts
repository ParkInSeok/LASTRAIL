import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { commandSchema, SeraHealConsumer, type SeraHealInput } from "./combat-sera-heal-consumer";
import { LogicalEventQueue } from "./combat-logical-event-queue";

type Command = z.infer<typeof commandSchema>;
const sequence = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const requestSchema = z.strictObject({ command: commandSchema, sequence, effectSequence: sequence.nullable().default(null) })
  .refine(r => r.command.kind !== "RESOLVE" && (r.command.kind === "CAST" ? r.effectSequence !== null : r.effectSequence === null));
type Request = z.infer<typeof requestSchema>;
type Operation = { kind: "SCHEDULE"; request: Request } | { kind: "ADVANCE"; atMs: number };
type Receipt = ReturnType<SeraHealConsumer["dispatch"]>;
type Emitted = { eventId: string; dueMs: number; sequence: number; receipt: Receipt };

/** Internal owner adapter for one supported ability. No automatic repeated cast,
 * external clock conversion, queue expiry guess, or new simultaneous-event policy.
 * All sequences (including the reserved effect sequence) are supplied by the
 * trusted logical authority, following the original (due,sequence) execution order. */
export class SeraHealActionQueue {
  private queue = new LogicalEventQueue<Command>();
  private readonly operations: Operation[] = [];
  private readonly scheduled: Request[] = [];
  private readonly receipts: Emitted[] = [];
  private constructor(private consumer: SeraHealConsumer,
    private readonly initial: ReturnType<SeraHealConsumer["snapshot"]>) {}
  static async prepare(db: D1Database, accountId: string, request: unknown, scope: unknown) {
    const consumer = await SeraHealConsumer.prepare(db, accountId, request, scope);
    return new SeraHealActionQueue(consumer, consumer.snapshot());
  }
  inputSnapshot() { return this.consumer.inputSnapshot(); }
  health(entityId: string) { return this.consumer.health(entityId); }
  pending() { return this.queue.snapshot().pending; }
  schedule(raw: unknown) {
    const parsed = requestSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, "combat_sera_queue_command_invalid");
    const old = this.scheduled.find(r => r.command.eventId === parsed.data.command.eventId);
    if (old) {
      if (canonicalJson(old) !== canonicalJson(parsed.data)) throw new ActivityError(409, "combat_sera_queue_event_conflict");
      return { eventId: old.command.eventId, replayed: true };
    }
    this.checkLimit();
    this.applySchedule(parsed.data); this.operations.push({ kind: "SCHEDULE", request: structuredClone(parsed.data) });
    return { eventId: parsed.data.command.eventId, replayed: false };
  }
  private applySchedule(request: Request) {
    // This boundary is shared by live scheduling and checkpoint replay. Reserve
    // both command and generated effect identity before either enters the queue.
    const identities = (r: Request) => [r.command.eventId, ...(r.command.kind === "CAST"
      ? [canonicalJson([this.consumer.inputSnapshot().scope.simulationId, r.command.actionId, "op_sera.active", "HEAL_ONE"])] : [])];
    const claimed = this.scheduled.flatMap(identities), incoming = identities(request);
    if (new Set(incoming).size !== incoming.length || incoming.some(id => claimed.includes(id)))
      throw new ActivityError(409, "combat_sera_queue_event_conflict");
    const orders = this.scheduled.flatMap(r => [r.sequence, ...(r.effectSequence === null ? [] : [r.effectSequence])]);
    if (orders.includes(request.sequence) || (request.effectSequence !== null
      && (request.effectSequence === request.sequence || orders.includes(request.effectSequence))))
      throw new ActivityError(409, "combat_logical_queue_sequence_conflict");
    if (request.command.kind === "CAST" && this.scheduled.some(r => r.command.kind === "CAST" && r.command.actionId === request.command.actionId))
      throw new ActivityError(409, "combat_sera_action_conflict");
    this.queue.enqueue({ eventId: request.command.eventId, dueMs: request.command.atMs, sequence: request.sequence, payload: request.command });
    this.scheduled.push(structuredClone(request));
  }
  advanceTo(atMs: number): Emitted[] {
    const clock = this.queue.snapshot();
    if (clock.closed && atMs === clock.clockMs) return [];
    this.checkLimit();
    // A failed drain publishes neither early HP nor a partial queue/receipt state.
    const candidate = SeraHealActionQueue.restore(this.inputSnapshot(), this.snapshot());
    const result = candidate.applyAdvance(atMs);
    candidate.operations.push({ kind: "ADVANCE", atMs });
    this.consumer = candidate.consumer; this.queue = candidate.queue;
    this.operations.splice(0, this.operations.length, ...candidate.operations);
    this.scheduled.splice(0, this.scheduled.length, ...candidate.scheduled);
    this.receipts.splice(0, this.receipts.length, ...candidate.receipts);
    return structuredClone(result);
  }
  private applyAdvance(atMs: number): Emitted[] {
    const emitted: Emitted[] = [];
    for (let event = this.queue.takeDue(atMs); event; event = this.queue.takeDue(atMs)) {
      const receipt = this.consumer.dispatch(event.payload);
      const output = { eventId: event.eventId, dueMs: event.dueMs, sequence: event.sequence, receipt };
      emitted.push(output); this.receipts.push(output);
      if (event.payload.kind === "CAST" && receipt.status === "ACCEPTED") {
        const request = this.scheduled.find(r => r.command.eventId === event!.eventId)!;
        this.queue.enqueue({ eventId: receipt.effectEventId!, dueMs: receipt.effectAtMs!, sequence: request.effectSequence!,
          payload: { kind: "RESOLVE", eventId: receipt.effectEventId!, actionId: receipt.actionId, atMs: receipt.effectAtMs! } });
      }
      if (event.payload.kind === "CANCEL") {
        const action = this.consumer.snapshot().state.actions.find(a => a.actionId === receipt.actionId)!;
        this.queue.cancel(action.effectEventId);
      }
    }
    this.queue.finishAdvance(atMs); return emitted;
  }
  private checkLimit() { if (this.operations.length >= 10000) throw new ActivityError(409, "combat_sera_queue_event_limit"); }
  snapshot() { return { version: "combat.sera-action-queue.v1", initial: structuredClone(this.initial),
    input: this.inputSnapshot(), queue: this.queue.snapshot(), consumer: this.consumer.snapshot(),
    operations: this.operations.map(o => structuredClone(o)), receipts: this.receipts.map(r => structuredClone(r)) }; }
  static restore(originalInput: SeraHealInput, raw: unknown) {
    try {
      const schema = z.strictObject({ version: z.literal("combat.sera-action-queue.v1"), initial: z.unknown(), input: z.unknown(),
        queue: z.unknown(), consumer: z.unknown(), receipts: z.unknown(), operations: z.array(z.discriminatedUnion("kind", [
          z.strictObject({ kind: z.literal("SCHEDULE"), request: requestSchema }),
          z.strictObject({ kind: z.literal("ADVANCE"), atMs: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER) }),
        ])).max(10000) });
      const checkpoint = schema.parse(raw);
      if (canonicalJson(originalInput) !== canonicalJson(checkpoint.input as SeraHealInput)) throw new Error("original input mismatch");
      const initial = SeraHealConsumer.restore(originalInput, checkpoint.initial);
      if (initial.snapshot().events.length !== 0) throw new Error("not initial state");
      const restored = new SeraHealActionQueue(initial, initial.snapshot());
      for (const op of checkpoint.operations) {
        if (op.kind === "SCHEDULE") restored.applySchedule(op.request); else restored.applyAdvance(op.atMs);
        restored.operations.push(structuredClone(op));
      }
      if (canonicalJson(restored.snapshot()) !== canonicalJson(checkpoint as Parameters<typeof canonicalJson>[0])) throw new Error("queue mismatch");
      return restored;
    } catch { throw new ActivityError(409, "combat_sera_queue_state_invalid"); }
  }
}
