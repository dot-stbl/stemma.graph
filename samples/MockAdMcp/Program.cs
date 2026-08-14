// MockAdMcp — demo MCP-shaped tools mirroring Hybrid.ai console.platform domain.
//
// Model (simplified trading desk):
//   Agency → Advertiser → CampaignFolder → Campaign → Banner → AdLibrary creative
//   Campaign also: LinkedSystems (SSP allow-list + bet), DirectDeals (PMP),
//   PriceLimitation caps, flight dates, BetOptimizationType.
//
// Not a full MCP server and not a full Hybrid API — just enough tools for
// MarketingAgent to exercise multi-step campaign setup offline.
//
// Run:
//   dotnet run --project samples/MockAdMcp
//   → http://localhost:5190

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

var store = HybridDeskStore.CreateSeed();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Text(
    """
    MockAdMcp — Hybrid-shaped trading desk tools (MCP HTTP demo)

    Domain: Agency → Advertiser → Campaign → Banner / AdLibrary
            + SSP allow-list + DirectDeal (PMP) + multi-unit limits

    GET  /health
    GET  /mcp/tools
    POST /mcp/tools/call   { "name": "...", "arguments": { ... } }
    GET  /mcp/campaigns
    GET  /mcp/ssps
    GET  /mcp/direct-deals
    """,
    "text/plain"));

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "MockAdMcp",
    product = "Hybrid.ai Console Platform (mock)",
}));

app.MapGet("/mcp/tools", () => Results.Json(new
{
    tools = ToolCatalog.All.Select(static tool => new
    {
        name = tool.Name,
        description = tool.Description,
        inputSchema = tool.InputSchema,
    }),
}));

app.MapGet("/mcp/campaigns", () =>
    Results.Json(store.Campaigns.Values.OrderBy(static campaign => campaign.Name)));

app.MapGet("/mcp/ssps", () => Results.Json(store.Ssps));

app.MapGet("/mcp/direct-deals", () => Results.Json(store.DirectDeals));

app.MapPost("/mcp/tools/call", async (HttpRequest request) =>
{
    ToolCallRequest? call;
    try
    {
        call = await JsonSerializer.DeserializeAsync<ToolCallRequest>(
            request.Body,
            JsonSerializerOptions.Web);
    }
    catch (JsonException exception)
    {
        return Results.Json(
            ToolCallResponse.Error($"invalid JSON: {exception.Message}"),
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (call is null || string.IsNullOrWhiteSpace(call.Name))
    {
        return Results.Json(
            ToolCallResponse.Error("name is required"),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var args = call.Arguments ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
    try
    {
        var payload = call.Name switch
        {
            "list_advertisers" => ListAdvertisers(store, args),
            "list_campaigns" => ListCampaigns(store, args),
            "get_campaign" => GetCampaign(store, args),
            "list_ssps" => store.Ssps,
            "list_direct_deals" => ListDirectDeals(store, args),
            "list_ad_library" => ListAdLibrary(store, args),
            "create_campaign" => CreateCampaign(store, args),
            "set_campaign_status" => SetCampaignStatus(store, args),
            "set_campaign_bet" => SetCampaignBet(store, args),
            "set_price_limitations" => SetPriceLimitations(store, args),
            "set_flight" => SetFlight(store, args),
            "set_linked_ssps" => SetLinkedSsps(store, args),
            "attach_direct_deals" => AttachDirectDeals(store, args),
            "attach_banner" => AttachBanner(store, args),
            "get_delivery_status" => GetDeliveryStatus(store, args),
            "suggest_cpm" => SuggestCpm(store, args),
            _ => throw new InvalidOperationException($"unknown tool: {call.Name}"),
        };

        return Results.Json(ToolCallResponse.Ok(payload));
    }
    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
    {
        return Results.Json(ToolCallResponse.Error(exception.Message));
    }
});

app.Logger.LogInformation(
    "MockAdMcp (Hybrid desk) ready at {Url} — tools: {Tools}",
    "http://localhost:5190",
    string.Join(", ", ToolCatalog.All.Select(static tool => tool.Name)));

await app.RunAsync();

static object ListAdvertisers(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var agencyId = ReadString(args, "agencyId");
    var items = store.Advertisers
        .Where(advertiser =>
            string.IsNullOrWhiteSpace(agencyId)
            || string.Equals(advertiser.AgencyId, agencyId, StringComparison.Ordinal))
        .ToArray();
    return new { count = items.Length, advertisers = items };
}

static object ListCampaigns(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var advertiserId = ReadString(args, "advertiserId");
    var status = ReadString(args, "status");
    var campaignType = ReadString(args, "campaignType");
    var items = store.Campaigns.Values
        .Where(campaign =>
            string.IsNullOrWhiteSpace(advertiserId)
            || string.Equals(campaign.AdvertiserId, advertiserId, StringComparison.Ordinal))
        .Where(campaign =>
            string.IsNullOrWhiteSpace(status)
            || string.Equals(campaign.Status, status, StringComparison.OrdinalIgnoreCase))
        .Where(campaign =>
            string.IsNullOrWhiteSpace(campaignType)
            || string.Equals(campaign.CampaignType, campaignType, StringComparison.OrdinalIgnoreCase))
        .Select(static campaign => CampaignSummaryDto(campaign))
        .ToArray();
    return new { count = items.Length, campaigns = items };
}

static object GetCampaign(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    return CampaignDetailDto(campaign);
}

static object ListDirectDeals(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var sspId = ReadInt(args, "sspId", -1);
    var items = store.DirectDeals
        .Where(deal => sspId < 0 || deal.SspId == sspId)
        .ToArray();
    return new { count = items.Length, directDeals = items };
}

static object ListAdLibrary(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var format = ReadString(args, "format");
    var status = ReadString(args, "status") ?? "Approved";
    var items = store.AdLibrary
        .Where(item =>
            string.IsNullOrWhiteSpace(format)
            || string.Equals(item.Format, format, StringComparison.OrdinalIgnoreCase))
        .Where(item => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase))
        .ToArray();
    return new { count = items.Length, items };
}

static object CreateCampaign(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var advertiserId = RequireString(args, "advertiserId");
    if (store.Advertisers.All(advertiser => advertiser.Id != advertiserId))
    {
        throw new InvalidOperationException($"unknown advertiserId: {advertiserId}");
    }

    var name = RequireString(args, "name");
    var campaignType = ReadString(args, "campaignType") ?? "HybridExtended";
    if (!HybridDeskStore.CampaignTypes.Contains(campaignType, StringComparer.OrdinalIgnoreCase))
    {
        throw new ArgumentException(
            $"campaignType must be one of: {string.Join(", ", HybridDeskStore.CampaignTypes)}");
    }

    var bet = ReadDecimal(args, "bet", 50m);
    var betOptimization = ReadString(args, "betOptimizationType") ?? "NoOptimization";
    var id = $"cmp_{Guid.NewGuid():N}"[..16];
    var now = DateTimeOffset.UtcNow;

    var campaign = new CampaignRecord
    {
        Id = id,
        Name = name,
        CampaignType = campaignType,
        AdvertiserId = advertiserId,
        Status = "NotActive",
        SystemStatus = "Active",
        DeliveryStatus = "NotActiveCampaign",
        IsDraft = true,
        Bet = bet,
        BetOptimizationType = betOptimization,
        Currency = "RUB",
        StartDate = ReadString(args, "startDate"),
        EndDate = ReadString(args, "endDate"),
        IsDontExpire = ReadBool(args, "isDontExpire", fallback: false),
        DailyLimitations = [],
        TotalLimitations = [],
        LinkedSystems = [],
        DirectDealIds = [],
        Banners = [],
        DefaultClickUrl = ReadString(args, "defaultClickUrl"),
        CreatedAtUnixMs = now.ToUnixTimeMilliseconds(),
        UpdatedAtUnixMs = now.ToUnixTimeMilliseconds(),
    };

    if (args.TryGetValue("dailyBudget", out var dailyBudgetElement)
        && dailyBudgetElement.ValueKind == JsonValueKind.Number)
    {
        campaign.DailyLimitations.Add(new PriceLimitation("Budget", dailyBudgetElement.GetDecimal()));
    }

    if (args.TryGetValue("totalBudget", out var totalBudgetElement)
        && totalBudgetElement.ValueKind == JsonValueKind.Number)
    {
        campaign.TotalLimitations.Add(new PriceLimitation("Budget", totalBudgetElement.GetDecimal()));
    }

    store.Campaigns[id] = campaign;
    return CampaignDetailDto(campaign);
}

static object SetCampaignStatus(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    var status = RequireString(args, "status");
    if (status is not ("Active" or "NotActive" or "Archive" or "Stopped" or "Moderation"))
    {
        throw new ArgumentException("status must be Active|NotActive|Archive|Stopped|Moderation");
    }

    if (status == "Active" && campaign.Banners.Count == 0)
    {
        throw new InvalidOperationException("cannot activate campaign without banners");
    }

    campaign.Status = status;
    campaign.IsDraft = status is "NotActive" or "Moderation";
    campaign.DeliveryStatus = status == "Active"
        ? (campaign.Banners.Any(static banner => banner.Status == "Active")
            ? "Active"
            : "BannersDeactivated")
        : "NotActiveCampaign";
    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignDetailDto(campaign);
}

static object SetCampaignBet(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    campaign.Bet = ReadDecimal(args, "bet", campaign.Bet);
    if (ReadString(args, "betOptimizationType") is { Length: > 0 } optimization)
    {
        campaign.BetOptimizationType = optimization;
    }

    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignSummaryDto(campaign);
}

static object SetPriceLimitations(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    if (args.TryGetValue("daily", out var daily) && daily.ValueKind == JsonValueKind.Array)
    {
        campaign.DailyLimitations = ParseLimitations(daily);
    }

    if (args.TryGetValue("total", out var total) && total.ValueKind == JsonValueKind.Array)
    {
        campaign.TotalLimitations = ParseLimitations(total);
    }

    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignDetailDto(campaign);
}

static object SetFlight(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    if (ReadString(args, "startDate") is { } start)
    {
        campaign.StartDate = start;
    }

    if (ReadString(args, "endDate") is { } end)
    {
        campaign.EndDate = end;
    }

    if (args.ContainsKey("isDontExpire"))
    {
        campaign.IsDontExpire = ReadBool(args, "isDontExpire", campaign.IsDontExpire);
    }

    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignSummaryDto(campaign);
}

static object SetLinkedSsps(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    if (!args.TryGetValue("linkedSystems", out var systems) || systems.ValueKind != JsonValueKind.Array)
    {
        throw new ArgumentException("linkedSystems array is required");
    }

    var linked = new List<LinkedSystem>();
    foreach (var element in systems.EnumerateArray())
    {
        var sspId = element.TryGetProperty("sspId", out var idElement) && idElement.TryGetInt32(out var id)
            ? id
            : throw new ArgumentException("linkedSystems[].sspId required");
        if (store.Ssps.All(ssp => ssp.Id != sspId))
        {
            throw new InvalidOperationException($"unknown sspId: {sspId}");
        }

        decimal? bet = element.TryGetProperty("bet", out var betElement)
                       && betElement.ValueKind == JsonValueKind.Number
            ? betElement.GetDecimal()
            : null;
        linked.Add(new LinkedSystem(sspId, bet));
    }

    campaign.LinkedSystems = linked;
    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignDetailDto(campaign);
}

static object AttachDirectDeals(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    if (!args.TryGetValue("directDealIds", out var deals) || deals.ValueKind != JsonValueKind.Array)
    {
        throw new ArgumentException("directDealIds array is required");
    }

    var ids = deals.EnumerateArray()
        .Select(static element => element.GetString() ?? "")
        .Where(static id => id.Length > 0)
        .ToList();
    foreach (var dealId in ids)
    {
        if (store.DirectDeals.All(deal => deal.Id != dealId))
        {
            throw new InvalidOperationException($"unknown directDealId: {dealId}");
        }
    }

    campaign.DirectDealIds = ids;
    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return CampaignDetailDto(campaign);
}

static object AttachBanner(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    var adLibraryId = RequireString(args, "adLibraryId");
    var creative = store.AdLibrary.FirstOrDefault(item => item.Id == adLibraryId)
                   ?? throw new InvalidOperationException($"unknown adLibraryId: {adLibraryId}");
    if (!string.Equals(creative.Status, "Approved", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"creative {adLibraryId} is not Approved");
    }

    var banner = new BannerRecord
    {
        Id = $"bnr_{Guid.NewGuid():N}"[..16],
        Name = ReadString(args, "name") ?? creative.Name,
        AdLibraryId = adLibraryId,
        Status = "Active",
        DestUrl = ReadString(args, "destUrl") ?? campaign.DefaultClickUrl ?? "https://example.hybrid.ai/",
        BetKoeff = ReadDecimal(args, "betKoeff", 1m),
        Format = creative.Format,
    };
    campaign.Banners.Add(banner);
    if (campaign.Status == "Active")
    {
        campaign.DeliveryStatus = "Active";
    }

    campaign.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return new { campaignId = campaign.Id, banner, campaign = CampaignSummaryDto(campaign) };
}

static object GetDeliveryStatus(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var campaign = RequireCampaign(store, RequireString(args, "campaignId"));
    return new
    {
        campaign.Id,
        campaign.Name,
        campaign.Status,
        campaign.SystemStatus,
        campaign.DeliveryStatus,
        bannerCount = campaign.Banners.Count,
        activeBanners = campaign.Banners.Count(static banner => banner.Status == "Active"),
        linkedSspCount = campaign.LinkedSystems.Count,
        directDealCount = campaign.DirectDealIds.Count,
    };
}

static object SuggestCpm(HybridDeskStore store, IReadOnlyDictionary<string, JsonElement> args)
{
    var sspId = ReadInt(args, "sspId", 0);
    var ssp = store.Ssps.FirstOrDefault(item => item.Id == sspId)
              ?? throw new InvalidOperationException($"unknown sspId: {sspId}");
    var baseCpm = ssp.Id switch
    {
        24 => 180m, // Yandex
        7 => 95m, // Between
        3 => 220m, // Google AdX
        12 => 70m, // Buzzoola
        _ => 100m,
    };
    return new
    {
        sspId = ssp.Id,
        sspName = ssp.Name,
        suggestedCpm = baseCpm,
        currency = "RUB",
        note = "mock SuggestedBid — not live Hybrid pricing",
    };
}

static CampaignRecord RequireCampaign(HybridDeskStore store, string campaignId)
{
    return store.Campaigns.TryGetValue(campaignId, out var campaign)
        ? campaign
        : throw new InvalidOperationException($"unknown campaignId: {campaignId}");
}

static object CampaignSummaryDto(CampaignRecord campaign)
{
    return new
    {
        campaign.Id,
        campaign.Name,
        campaign.CampaignType,
        campaign.AdvertiserId,
        campaign.Status,
        campaign.DeliveryStatus,
        campaign.Bet,
        campaign.BetOptimizationType,
        campaign.Currency,
        campaign.IsDraft,
        banners = campaign.Banners.Count,
        linkedSsps = campaign.LinkedSystems.Count,
    };
}

static object CampaignDetailDto(CampaignRecord campaign)
{
    return new
    {
        campaign.Id,
        campaign.Name,
        campaign.CampaignType,
        campaign.AdvertiserId,
        campaign.Status,
        campaign.SystemStatus,
        campaign.DeliveryStatus,
        campaign.IsDraft,
        campaign.Bet,
        campaign.BetOptimizationType,
        campaign.Currency,
        flight = new
        {
            campaign.StartDate,
            campaign.EndDate,
            campaign.IsDontExpire,
        },
        limits = new
        {
            daily = campaign.DailyLimitations,
            total = campaign.TotalLimitations,
        },
        linkedSystems = campaign.LinkedSystems,
        directDealIds = campaign.DirectDealIds,
        banners = campaign.Banners,
        campaign.DefaultClickUrl,
        campaign.CreatedAtUnixMs,
        campaign.UpdatedAtUnixMs,
    };
}

static List<PriceLimitation> ParseLimitations(JsonElement array)
{
    var list = new List<PriceLimitation>();
    foreach (var element in array.EnumerateArray())
    {
        var unit = element.TryGetProperty("unit", out var unitElement)
            ? unitElement.GetString() ?? "Budget"
            : "Budget";
        var amount = element.TryGetProperty("amount", out var amountElement)
                     && amountElement.ValueKind == JsonValueKind.Number
            ? amountElement.GetDecimal()
            : 0m;
        list.Add(new PriceLimitation(unit, amount));
    }

    return list;
}

static string? ReadString(IReadOnlyDictionary<string, JsonElement> args, string key)
{
    return !args.TryGetValue(key, out var element)
        ? null
        : element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => element.GetRawText(),
        };
}

static string RequireString(IReadOnlyDictionary<string, JsonElement> args, string key)
{
    return ReadString(args, key) is { Length: > 0 } value
        ? value
        : throw new ArgumentException($"{key} is required");
}

static int ReadInt(IReadOnlyDictionary<string, JsonElement> args, string key, int fallback)
{
    return !args.TryGetValue(key, out var element)
        ? fallback
        : element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
            _ => fallback,
        };
}

static decimal ReadDecimal(IReadOnlyDictionary<string, JsonElement> args, string key, decimal fallback)
{
    return !args.TryGetValue(key, out var element)
        ? fallback
        : element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(element.GetString(), out var parsed) => parsed,
            _ => fallback,
        };
}

static bool ReadBool(IReadOnlyDictionary<string, JsonElement> args, string key, bool fallback)
{
    return !args.TryGetValue(key, out var element)
        ? fallback
        : element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
            _ => fallback,
        };
}

file sealed record ToolDef(string Name, string Description, object InputSchema);

file static class ToolCatalog
{
    public static IReadOnlyList<ToolDef> All { get; } =
    [
        new("list_advertisers", "List advertisers (optional agencyId filter).", new
        {
            type = "object",
            properties = new { agencyId = new { type = "string" } },
        }),
        new("list_campaigns", "List campaigns by advertiserId / status / campaignType.", new
        {
            type = "object",
            properties = new
            {
                advertiserId = new { type = "string" },
                status = new { type = "string" },
                campaignType = new { type = "string" },
            },
        }),
        new("get_campaign", "Full campaign detail (banners, SSP, deals, limits).", new
        {
            type = "object",
            required = new[] { "campaignId" },
            properties = new { campaignId = new { type = "string" } },
        }),
        new("list_ssps", "SSP catalog visible to the trading desk.", new { type = "object" }),
        new("list_direct_deals", "PMP direct deals (optional sspId).", new
        {
            type = "object",
            properties = new { sspId = new { type = "integer" } },
        }),
        new("list_ad_library", "Approved creatives in AdLibrary.", new
        {
            type = "object",
            properties = new
            {
                format = new { type = "string", description = "Display|Video|NativeAd|…" },
                status = new { type = "string", description = "default Approved" },
            },
        }),
        new("create_campaign", "Create draft campaign (Hybrid campaign types).", new
        {
            type = "object",
            required = new[] { "advertiserId", "name" },
            properties = new
            {
                advertiserId = new { type = "string" },
                name = new { type = "string" },
                campaignType = new
                {
                    type = "string",
                    description = "HybridExtended|HybridVideo|InApp|CTV|Dooh|FeedAdsWeb|…",
                },
                bet = new { type = "number" },
                betOptimizationType = new
                {
                    type = "string",
                    description = "NoOptimization|CPC|CPM|CPI|ConversionRangePricing|…",
                },
                dailyBudget = new { type = "number", description = "PriceLimitation Budget (daily)" },
                totalBudget = new { type = "number", description = "PriceLimitation Budget (total)" },
                startDate = new { type = "string" },
                endDate = new { type = "string" },
                isDontExpire = new { type = "boolean" },
                defaultClickUrl = new { type = "string" },
            },
        }),
        new("set_campaign_status", "Activate / Deactivate / Archive / Stop / Moderation.", new
        {
            type = "object",
            required = new[] { "campaignId", "status" },
            properties = new
            {
                campaignId = new { type = "string" },
                status = new { type = "string" },
            },
        }),
        new("set_campaign_bet", "Update Bet + optional BetOptimizationType.", new
        {
            type = "object",
            required = new[] { "campaignId", "bet" },
            properties = new
            {
                campaignId = new { type = "string" },
                bet = new { type = "number" },
                betOptimizationType = new { type = "string" },
            },
        }),
        new("set_price_limitations", "Set daily/total PriceLimitation arrays.", new
        {
            type = "object",
            required = new[] { "campaignId" },
            properties = new
            {
                campaignId = new { type = "string" },
                daily = new { type = "array" },
                total = new { type = "array" },
            },
        }),
        new("set_flight", "StartDate / EndDate / IsDontExpire.", new
        {
            type = "object",
            required = new[] { "campaignId" },
            properties = new
            {
                campaignId = new { type = "string" },
                startDate = new { type = "string" },
                endDate = new { type = "string" },
                isDontExpire = new { type = "boolean" },
            },
        }),
        new("set_linked_ssps", "SSP allow-list with optional per-SSP bet.", new
        {
            type = "object",
            required = new[] { "campaignId", "linkedSystems" },
            properties = new
            {
                campaignId = new { type = "string" },
                linkedSystems = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            sspId = new { type = "integer" },
                            bet = new { type = "number" },
                        },
                    },
                },
            },
        }),
        new("attach_direct_deals", "Attach PMP DirectDeal ids to campaign.", new
        {
            type = "object",
            required = new[] { "campaignId", "directDealIds" },
            properties = new
            {
                campaignId = new { type = "string" },
                directDealIds = new { type = "array", items = new { type = "string" } },
            },
        }),
        new("attach_banner", "Bind AdLibrary creative as LinkVirtualBanner.", new
        {
            type = "object",
            required = new[] { "campaignId", "adLibraryId" },
            properties = new
            {
                campaignId = new { type = "string" },
                adLibraryId = new { type = "string" },
                name = new { type = "string" },
                destUrl = new { type = "string" },
                betKoeff = new { type = "number" },
            },
        }),
        new("get_delivery_status", "Status + DeliveryStatus + banner/SSP counts.", new
        {
            type = "object",
            required = new[] { "campaignId" },
            properties = new { campaignId = new { type = "string" } },
        }),
        new("suggest_cpm", "Mock SuggestedBid CPM for an SSP.", new
        {
            type = "object",
            required = new[] { "sspId" },
            properties = new { sspId = new { type = "integer" } },
        }),
    ];
}

file sealed class HybridDeskStore
{
    public static IReadOnlyList<string> CampaignTypes { get; } =
    [
        "HybridExtended", "HybridVideo", "HybridMobile", "InApp", "CTV", "Dooh",
        "FeedAdsWeb", "FeedAdsInApp", "Copilot",
    ];

    public required List<AdvertiserRecord> Advertisers { get; init; }
    public required List<SspRecord> Ssps { get; init; }
    public required List<DirectDealRecord> DirectDeals { get; init; }
    public required List<AdLibraryRecord> AdLibrary { get; init; }
    public required ConcurrentDictionary<string, CampaignRecord> Campaigns { get; init; }

    public static HybridDeskStore CreateSeed()
    {
        return new HybridDeskStore
        {
            Advertisers =
            [
                new AdvertiserRecord("adv_demo_brand", "Demo Brand RU", "agc_hybrid"),
                new AdvertiserRecord("adv_retail_feed", "Retail Feed Ads", "agc_hybrid"),
            ],
            Ssps =
            [
                new SspRecord(24, "Yandex"),
                new SspRecord(3, "Google AdX"),
                new SspRecord(7, "Between"),
                new SspRecord(12, "Buzzoola"),
                new SspRecord(15, "BidSwitch"),
                new SspRecord(31, "Videonow"),
            ],
            DirectDeals =
            [
                new DirectDealRecord(
                    "deal_yandex_home",
                    "Yandex Premium Homepage",
                    "deal-yandex-123",
                    24,
                    "Fixed",
                    "RUB",
                    250m,
                    ["Display"],
                    "Web"),
                new DirectDealRecord(
                    "deal_between_video",
                    "Between InStream Package",
                    "deal-between-vid-9",
                    7,
                    "NonFixed",
                    "RUB",
                    180m,
                    ["Video"],
                    "Web"),
            ],
            AdLibrary =
            [
                new AdLibraryRecord("adl_300x250_v1", "300x250_brand_v1", "Display", "Approved", "300x250"),
                new AdLibraryRecord("adl_video_15s", "Video_15s_pre_v2", "Video", "Approved", "15s"),
                new AdLibraryRecord("adl_native_card", "Native_card_v1", "NativeAd", "Approved", "card"),
                new AdLibraryRecord("adl_draft", "Draft_creative", "Display", "OnModeration", "300x250"),
            ],
            Campaigns = new ConcurrentDictionary<string, CampaignRecord>(StringComparer.Ordinal),
        };
    }
}

file sealed record AdvertiserRecord(string Id, string Name, string AgencyId);

file sealed record SspRecord(int Id, string Name);

file sealed record DirectDealRecord(
    string Id,
    string Name,
    string DirectDealId,
    int SspId,
    string PricingType,
    string Currency,
    decimal Cpm,
    IReadOnlyList<string> BannerType,
    string TrafficType);

file sealed record AdLibraryRecord(
    string Id,
    string Name,
    string Format,
    string Status,
    string SizeOrDuration);

file sealed class CampaignRecord
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string CampaignType { get; set; }
    public required string AdvertiserId { get; init; }
    public required string Status { get; set; }
    public required string SystemStatus { get; set; }
    public required string DeliveryStatus { get; set; }
    public required bool IsDraft { get; set; }
    public required decimal Bet { get; set; }
    public required string BetOptimizationType { get; set; }
    public required string Currency { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public bool IsDontExpire { get; set; }
    public required List<PriceLimitation> DailyLimitations { get; set; }
    public required List<PriceLimitation> TotalLimitations { get; set; }
    public required List<LinkedSystem> LinkedSystems { get; set; }
    public required List<string> DirectDealIds { get; set; }
    public required List<BannerRecord> Banners { get; set; }
    public string? DefaultClickUrl { get; set; }
    public long CreatedAtUnixMs { get; init; }
    public long UpdatedAtUnixMs { get; set; }
}

file sealed class BannerRecord
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string AdLibraryId { get; init; }
    public required string Status { get; set; }
    public required string DestUrl { get; init; }
    public required decimal BetKoeff { get; init; }
    public required string Format { get; init; }
}

file sealed record LinkedSystem(int SspId, decimal? Bet);

file sealed record PriceLimitation(string Unit, decimal Amount);

file sealed record ToolCallRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("arguments")] Dictionary<string, JsonElement>? Arguments);

file sealed record ToolCallResponse(
    [property: JsonPropertyName("content")] IReadOnlyList<ToolContent> Content,
    [property: JsonPropertyName("isError")] bool IsError)
{
    public static ToolCallResponse Ok(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        return new ToolCallResponse([new ToolContent("text", json)], IsError: false);
    }

    public static ToolCallResponse Error(string message)
    {
        return new ToolCallResponse([new ToolContent("text", message)], IsError: true);
    }
}

file sealed record ToolContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);
