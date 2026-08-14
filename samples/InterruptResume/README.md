# InterruptResume — HITL pause / resume

Console sample of a **human-in-the-loop** interrupt. The `gate` node interrupts
on first visit with a transfer payload; the host resumes with
`Command { Kind = "approve" }`.

## Run

```bash
dotnet run --project samples/InterruptResume
```

## Graph

```
START → gate → END
```

Flow:

1. `StreamAsync` / invoke until `StreamEventKind.Interrupt`
2. Checkpoint status is `GraphRunStatus.Interrupted`
3. `ResumeAsync(threadId, new Command { Kind = "approve", Payload = "ok" })`
4. Gate continues, writes an approval message, run ends with `Done`
