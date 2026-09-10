import { log } from "@lastrail/common";
import { runRecurringJobs } from "../../game-api/src/recurring-jobs";

export default {
  scheduled(_event, env, ctx): void {
    ctx.waitUntil(runRecurringJobs(env.DB).then(result => {
      log({ level: result.failures ? "warn" : "info", message: "recurring reward jobs completed", fields: result });
    }));
  },
  queue(batch, env, ctx): void {
    ctx.waitUntil(
      Promise.resolve().then(() => {
        log({ level: "info", message: "queue batch received", fields: { queue: env.ADMIN_JOBS_QUEUE_NAME, size: batch.messages.length } });
      })
    );
  }
} satisfies ExportedHandler<Env>;
