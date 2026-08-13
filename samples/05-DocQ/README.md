# 05-DocQ

Documentation Q&A: **search sandbox** → **answer** (scripted or live chat).

## Run

```bash
dotnet run --project samples/05-DocQ -- --offline --root . --question "What is Voluta?"
```

## Flags

| Flag | Meaning |
|------|---------|
| `--offline` | Scripted replies |
| `--root <path>` | Sandbox root |
| `--question` / `-q` | User question |
| `--thread <id>` | Checkpoint thread id |
