# 04-ReviewBot

CLI harness: **plan → tools (sandbox search) → review**, optional HITL interrupt.

## Run

```bash
# offline (ScriptedChatClient)
dotnet run --project samples/04-ReviewBot -- --offline --root .

# live OpenAI-compatible chat
export STEMMA_CHAT_ENDPOINT=https://api.openai.com/v1
export STEMMA_CHAT_API_KEY=...
export STEMMA_CHAT_MODEL=gpt-4o-mini
dotnet run --project samples/04-ReviewBot -- --root . --query StateGraph

# HITL approve gate before review LLM call
dotnet run --project samples/04-ReviewBot -- --offline --hitl
```

## Flags

| Flag | Meaning |
|------|---------|
| `--offline` | Scripted replies (no API) |
| `--root <path>` | Sandbox root (default `.`) |
| `--query <text>` | Search / review focus |
| `--thread <id>` | Checkpoint thread id |
| `--hitl` | Interrupt before final review |
