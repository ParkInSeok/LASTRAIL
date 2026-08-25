import { log } from "@lastrail/common";

export default {
  queue(batch, env, ctx): void {
    ctx.waitUntil(
      Promise.resolve().then(() => {
        log({ level: "info", message: "queue batch received", fields: { queue: env.ADMIN_JOBS_QUEUE_NAME, size: batch.messages.length } });
      })
    );
  }
} satisfies ExportedHandler<Env>;
