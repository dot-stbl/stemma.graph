# MockAdMcp — Hybrid trading-desk tools (demo)

MCP-shaped HTTP surface that **mirrors Hybrid.ai `console.platform` nouns**,
not US-style reserved publisher slots.

Mental model:

```
Agency → Advertiser → Campaign → Banner (LinkVirtualBanner) → AdLibrary
                         ├─ LinkedSystems (SSP allow-list + bet)
                         ├─ DirectDeals (PMP)
                         └─ PriceLimitation[] (Budget / Impression / Click…)
```

## Run

```bash
dotnet run --project samples/MockAdMcp
```

`http://localhost:5190` — do not leave running in agent sessions.

## Tools

| Tool | Hybrid mirror |
|------|----------------|
| `list_advertisers` | Advertiser under agency |
| `list_campaigns` / `get_campaign` | NewCampaign polymorphic package |
| `list_ssps` | SSP catalog (Yandex, AdX, Between…) |
| `list_direct_deals` | PMP DirectDeal |
| `list_ad_library` | AdLibrary creatives |
| `create_campaign` | CampaignCreate (draft, NotActive) |
| `set_campaign_status` | Activate / Deactivate / Archive… |
| `set_campaign_bet` | Bet + BetOptimizationType |
| `set_price_limitations` | Daily/total multi-unit limits |
| `set_flight` | StartDate / EndDate / IsDontExpire |
| `set_linked_ssps` | LinkedSystems allow-list |
| `attach_direct_deals` | DirectDeals[] |
| `attach_banner` | New banner from AdLibrary |
| `get_delivery_status` | Status vs DeliveryStatus |
| `suggest_cpm` | SuggestedBid mock |

## Example

```bash
curl -s http://localhost:5190/mcp/tools | jq '.tools[].name'
curl -s -X POST http://localhost:5190/mcp/tools/call \
  -H "content-type: application/json" \
  -d '{"name":"list_ssps","arguments":{}}'
```

Used by [`MarketingAgent`](../MarketingAgent/).
