# Voluta Grafana dashboards

Importable Grafana JSON for **`voluta.*`** runtime metrics.

| File | UID | Purpose |
|------|-----|---------|
| [`voluta-overview.json`](./voluta-overview.json) | `voluta-overview` | Superstep/node duration, interrupts, checkpoint ops, stream dropped |

**Source of truth for names:** [`VolutaDiagnostics`](../../src/Voluta/Diagnostics/VolutaDiagnostics.cs)  
**Catalog:** [`docs/0.x/concepts/observability.mdx`](../../docs/0.x/concepts/observability.mdx)

---

## Import

### UI

1. Grafana → **Dashboards** → **New** → **Import**.
2. Upload `voluta-overview.json` (or paste JSON).
3. Pick a **Prometheus** (or Mimir / Cortex / AMP) datasource that holds Voluta metrics.
4. Import.

### API

```bash
curl -s -X POST \
  -H "Authorization: Bearer $GRAFANA_API_TOKEN" \
  -H "Content-Type: application/json" \
  "$GRAFANA_URL/api/dashboards/db" \
  -d @- <<EOF
{
  "dashboard": $(cat voluta-overview.json | jq 'del(.__inputs, .__requires)'),
  "overwrite": true,
  "message": "Voluta overview"
}
EOF
```

(Strip `__inputs` / `__requires` if you inject the datasource UID yourself.)

### Provisioning (optional)

```yaml
# grafana/provisioning/dashboards/voluta.yaml
apiVersion: 1
providers:
  - name: voluta
    folder: Voluta
    type: file
    options:
      path: /var/lib/grafana/dashboards/voluta
```

Mount this directory’s JSON files into that path.

---

## Getting metrics into Prometheus

Voluta core always creates BCL instruments on meter **`Voluta`**. Hosts export via **`Voluta.OpenTelemetry`**:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Voluta.OpenTelemetry;

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddVolutaInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddVolutaInstrumentation().AddOtlpExporter());
```

Typical pipeline:

```text
Host (AddVolutaInstrumentation)
        │ OTLP (gRPC/HTTP)
        ▼
OTel Collector  ──►  Prometheus remote_write / exporter
        │
        ▼
Grafana (Prometheus datasource)
```

Alternatives that also work with this dashboard:

- `AddPrometheusExporter()` on the host (scrape the app directly).
- Azure Monitor / other backends that expose PromQL-compatible metrics (adjust series names if they differ).

Without `AddVolutaInstrumentation()`, nothing is registered on the OTel meter provider — panels stay empty even if the app runs graphs.

---

## OTel name → Prometheus series

OpenTelemetry **dot.case** names become Prometheus **snake_case**. Units and instrument kinds add suffixes (OTel Prometheus compatibility):

| Constant (`VolutaDiagnostics`) | OTel metric name | Typical Prometheus series |
|--------------------------------|------------------|---------------------------|
| `SuperstepDurationMetricName` | `voluta.superstep.duration` | `voluta_superstep_duration_milliseconds_{bucket,sum,count}` |
| `NodeDurationMetricName` | `voluta.node.duration` | `voluta_node_duration_milliseconds_{bucket,sum,count}` |
| `InterruptCountMetricName` | `voluta.interrupt.count` | `voluta_interrupt_count_total` |
| `CheckpointPutCountMetricName` | `voluta.checkpoint.put.count` | `voluta_checkpoint_put_count_total` |
| `CheckpointGetCountMetricName` | `voluta.checkpoint.get.count` | `voluta_checkpoint_get_count_total` |
| `CheckpointListCountMetricName` | `voluta.checkpoint.list.count` | `voluta_checkpoint_list_count_total` |
| `StreamDroppedMetricName` | `voluta.stream.dropped` | `voluta_stream_dropped_total` |

### Tags → labels

| Tag constant | OTel tag | Prometheus label |
|--------------|----------|------------------|
| `TagNodeName` | `node.name` | `node_name` |
| `TagStreamKind` | `stream.kind` | `stream_kind` |
| `TagProviderName` | `provider.name` | `provider_name` |
| `TagRunStatus` | `run.status` | `run_status` |
| `TagErrorType` | `error.type` | `error_type` |

### If panels are empty

Exporters differ slightly. Check what landed:

```promql
{__name__=~"voluta_.*"}
```

Common mismatches and fixes:

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| No series | OTel not wired / wrong job | Confirm `AddVolutaInstrumentation` + scrape job |
| `voluta_superstep_duration_ms_*` instead of `*_milliseconds_*` | Unit suffix style | Edit panel expr to match |
| Counter without `_total` | Older / non-OTel Prometheus naming | Drop `_total` in expr |
| Labels keep dots (`node.name`) | Some OTLP bridges | Change `node_name` → `node_name` or `"node.name"` per backend |
| Histogram unitless name | Exporter omitted unit | Use `voluta_superstep_duration_bucket` etc. |

Dashboard variables:

- **Datasource** — Prometheus-compatible DS.
- **Job** — scrape `job` label (All = `.*`).
- **Node** — `node_name` from node duration series.

---

## Panels map

| Panel | Metric(s) | Notes |
|-------|-----------|--------|
| Superstep duration | `voluta.superstep.duration` | p50 / p95 / p99 |
| Node duration by node | `voluta.node.duration` | p50 / p99, `node_name` |
| Interrupt rate | `voluta.interrupt.count` | rate by node |
| Stream dropped rate | `voluta.stream.dropped` | by `stream_kind` |
| Stream dropped (range) | same | `increase` over dashboard range |
| Checkpoint put/get/list rate | `voluta.checkpoint.*.count` | by `provider_name` |
| Checkpoint puts / Interrupts (range) | stats | range totals |

Suggested alerts (from observability checklist): rising stream dropped, interrupt spikes, superstep p99, host `graph.run_failed` / checkpoint fail EventIds (logs — not on this dashboard).

---

## Traces (not on this dashboard)

Activity names (`voluta.superstep`, `voluta.node.execute`, `voluta.checkpoint.*`) go to a **Tempo / Jaeger** (or other) trace backend via the same OTLP path. Wire a Traces panel separately if you need span search; this pack is metrics-only.

---

## Versioning

- Dashboard `uid` is stable: **`voluta-overview`**. Re-import with overwrite to update.
- When renaming metrics in `VolutaDiagnostics`, update this JSON and the table above in the same PR.

---

## Also shipped in docs.tgz

Release assets pack `docs/` (including `docs/ops/grafana/voluta-overview.json`).
Same JSON as this folder — use either path. Product docs:
[Observability](../../docs/0.x/concepts/observability.mdx#grafana-dashboard-download).