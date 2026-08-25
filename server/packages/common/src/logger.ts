export type LogLevel = "debug" | "info" | "warn" | "error";

export type LogRecord = {
  readonly level: LogLevel;
  readonly message: string;
  readonly requestId?: string;
  readonly fields?: Readonly<Record<string, unknown>>;
};

export function log(record: LogRecord): void {
  const method = record.level === "error" ? console.error : record.level === "warn" ? console.warn : console.log;
  method(JSON.stringify({ timestamp: Date.now(), ...record }));
}
