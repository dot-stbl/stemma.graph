# MarketingAgent — Hybrid desk harness

CLI harness that sets up a **Hybrid.ai-style campaign** via
[`MockAdMcp`](../MockAdMcp/) (console.platform nouns).

## Graph

```
START → brief → creative → setup → review → END
```

| Node | Role |
|------|------|
| `brief` | Trading-desk campaign brief |
| `creative` | AdLibrary creative meta |
| `setup` | create RK → SSP → DirectDeal → banner → Activate |
| `review` | go / no-go (+ optional HITL) |

`setup` tool sequence (live MCP):

1. `list_advertisers` / `list_ssps` / `list_ad_library`
2. `suggest_cpm`
3. `create_campaign` (draft, NotActive, Budget limits)
4. `set_linked_ssps` (Yandex + Between)
5. `attach_direct_deals`
6. `attach_banner` (Approved AdLibrary)
7. `set_campaign_status` Active
8. `get_delivery_status`

## Run

```bash
# terminal 1
dotnet run --project samples/MockAdMcp

# terminal 2
dotnet run --project samples/MarketingAgent -- --offline

# no MCP
dotnet run --project samples/MarketingAgent -- --offline --dry-run

# HITL before review
dotnet run --project samples/MarketingAgent -- --offline --hitl
```

## Flags

| Flag | Meaning |
|------|---------|
| `--offline` | Scripted chat |
| `--dry-run` | Skip MCP HTTP |
| `--mcp-url` | Default `http://localhost:5190` |
| `--product` | Product name in brief (default Hybrid Console Platform) |
| `--advertiser` | Default `adv_demo_brand` |
| `--type` | CampaignType (default `HybridExtended`) |
| `--bet` | Campaign bet RUB |
| `--daily-budget` | Daily Budget PriceLimitation |
| `--thread` | Checkpoint thread id |
| `--hitl` | Interrupt at review |

Live chat: `VOLUTA_CHAT_ENDPOINT` / `VOLUTA_CHAT_API_KEY` / `VOLUTA_CHAT_MODEL`.
