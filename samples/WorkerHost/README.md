# WorkerHost — durable runner pattern

HITL and long agent turns must not live inside a single HTTP request. This sample
shows a **wake → run until interrupt/done/fail → park or complete** loop using
`BackgroundService` and an in-memory channel.

```text
producer ──ThreadWake(threadId)──► channel ──► GraphWorkerService
                                                    │
                                                    ▼
                                            Invoke / ResumeInvoke
                                                    │
                          ┌─────────────────────────┼─────────────────────────┐
                          ▼                         ▼                         ▼
                     Interrupt                  End (Done)                 Failed
                     → park                     → complete                 → dead-letter
                     (checkpoint SoT)           (no more wakes)            (last-good C)
```

## Run

```bash
dotnet run --project samples/WorkerHost
```

Expected log shape (abbrev.):

```text
Demo: enqueue start for worker-hitl-1
Thread worker-hitl-1 parked at interrupt
Demo: enqueue resume (approve) for worker-hitl-1
Thread worker-hitl-1 completed
Demo: final status=Done
```

The process exits after the demo completes (producer stops the host).

## Pieces

| Type | Role |
|------|------|
| `ThreadWake` | Start (input writes) or Resume (`Command`) for a `threadId` |
| `ThreadWakeChannel` | In-process `Channel<T>` bus — replace with NATS/SQS/etc. |
| `GraphThreadRunner` | One wake → `InvokeAsync` / `ResumeInvokeAsync` → disposition |
| `GraphWorkerService` | `BackgroundService` drain loop + park/complete/fail policy |
| `DemoProducerService` | Sample-only driver (start → park → approve → stop) |

No new NuGet package: copy these types into your host, or extract a shared
library when you have a second consumer.

## Multi-instance / k8s scale-out

- **Checkpointer is the source of truth.** Use File / EF / S3 (or another shared
  store), not `UseInMemory`, when more than one process can touch a thread.
- **Wakes are hints.** Any instance may receive “run thread X”; the durable
  snapshot decides whether the next turn is invoke, resume, or already terminal.
- **Avoid double-run.** Partition wakes by `threadId` (queue key / consumer
  group) or take a short lease before `Invoke`/`Resume`. This sample skips a
  second in-flight wake for the same id **on one instance** only.
- **Interrupt park is multi-process safe.** Process A interrupts and exits; hours
  later process B receives a resume wake and `ResumeInvokeAsync` against the same
  store.
- **Failed is not HITL-resumeable.** Last-good channel values remain on the
  Failed checkpoint; re-invoke a new thread or rebuild input from history
  (see main README failure policy).

## Production swaps

| Sample | Production |
|--------|------------|
| `ThreadWakeChannel` | Durable queue + poison/DLQ |
| `UseInMemory()` | `UseFile` / `UseEntityFrameworkCore` / `UseS3` |
| Log-only fail policy | Metrics, alert, DLQ message with `threadId` + error code |
| `DemoProducerService` | HTTP approve endpoint, bus consumer, cron redrive |

Not in scope: Hangfire/Quartz, full Agent Server PaaS.
