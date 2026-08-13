(() => {
  const base = location.pathname.replace(/\/?(index\.html)?$/, "") || "/voluta";
  const api = (path) => base + path;
  let eventSource = null;

  document.querySelectorAll(".tabs button").forEach((btn) => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".tabs button").forEach((b) => b.classList.remove("active"));
      document.querySelectorAll(".panel").forEach((p) => p.classList.remove("active"));
      btn.classList.add("active");
      document.getElementById("panel-" + btn.dataset.tab).classList.add("active");
    });
  });

  function statusPill(status) {
    const s = (status || "").toString();
    const cls =
      s === "Done" ? "ok" :
      s === "Interrupted" ? "warn" :
      s === "Failed" || s === "Cancelled" ? "err" : "idle";
    return `<span class="pill ${cls}">${escapeHtml(s || "—")}</span>`;
  }

  function escapeHtml(text) {
    return String(text)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;");
  }

  function renderCheckpointMeta(data) {
    const channels = Object.entries(data.channelValues || {})
      .map(([k, v]) => `<dt>${escapeHtml(k)}</dt><dd>${escapeHtml(v)}</dd>`)
      .join("") || `<dt>—</dt><dd class="muted">no channels</dd>`;
    const versions = Object.entries(data.channelVersions || {})
      .map(([k, v]) => `${escapeHtml(k)}@${escapeHtml(v)}`)
      .join(", ") || "—";

    document.getElementById("inspectorMeta").innerHTML = `
      <div style="display:flex;gap:0.5rem;align-items:center;margin-bottom:0.75rem">
        ${statusPill(data.status)}
        <span class="mono muted">step ${escapeHtml(data.step)}</span>
      </div>
      <dl class="kv">
        <dt>thread</dt><dd>${escapeHtml(data.threadId)}</dd>
        <dt>last node</dt><dd>${escapeHtml(data.lastNode ?? "—")}</dd>
        <dt>next</dt><dd>${escapeHtml((data.nextNodes || []).join(", ") || "—")}</dd>
        <dt>interrupt</dt><dd>${escapeHtml(data.interruptPayload ?? "—")}</dd>
        <dt>versions</dt><dd>${versions}</dd>
      </dl>
      <div style="margin-top:0.75rem"><strong>Channels</strong></div>
      <dl class="kv" style="margin-top:0.35rem">${channels}</dl>
    `;
  }

  function appendStreamLine(text) {
    const log = document.getElementById("streamLog");
    const line = document.createElement("div");
    line.className = "line";
    line.textContent = text;
    log.appendChild(line);
    log.scrollTop = log.scrollHeight;
  }

  function stopStream() {
    if (eventSource) {
      eventSource.close();
      eventSource = null;
    }
    document.getElementById("stopStream").disabled = true;
    document.getElementById("startStream").disabled = false;
  }

  document.getElementById("loadThread").onclick = async () => {
    const id = document.getElementById("threadId").value.trim();
    const out = document.getElementById("inspectorOut");
    if (!id) {
      out.textContent = "Enter a thread id.";
      return;
    }
    const res = await fetch(api("/api/threads/" + encodeURIComponent(id)));
    if (!res.ok) {
      out.textContent = await res.text();
      document.getElementById("inspectorMeta").innerHTML =
        `<div class="muted">Not found or error (${res.status}).</div>`;
      return;
    }
    const data = await res.json();
    renderCheckpointMeta(data);
    out.textContent = JSON.stringify(data, null, 2);
  };

  document.getElementById("startStream").onclick = () => {
    const id = document.getElementById("threadId").value.trim();
    if (!id) {
      appendStreamLine("Enter a thread id first.");
      return;
    }
    stopStream();
    document.getElementById("streamLog").innerHTML = "";
    appendStreamLine("connecting…");
    const url = api("/api/threads/" + encodeURIComponent(id) + "/stream?mode=checkpoint");
    eventSource = new EventSource(url);
    document.getElementById("stopStream").disabled = false;
    document.getElementById("startStream").disabled = true;

    eventSource.addEventListener("stream", (ev) => {
      try {
        const data = JSON.parse(ev.data);
        appendStreamLine(
          `step=${data.step} kind=${data.kind} nodes=[${(data.nodeNames || []).join(",")}] payload=${data.payload ?? ""}`
        );
      } catch {
        appendStreamLine(ev.data);
      }
    });
    eventSource.addEventListener("done", () => {
      appendStreamLine("— done —");
      stopStream();
    });
    eventSource.addEventListener("error", (ev) => {
      if (ev.data) {
        appendStreamLine("error: " + ev.data);
      } else {
        appendStreamLine("connection closed / error");
      }
      stopStream();
    });
  };

  document.getElementById("stopStream").onclick = stopStream;

  document.getElementById("refreshHitl").onclick = async () => {
    const res = await fetch(api("/api/hitl"));
    const list = document.getElementById("hitlList");
    list.innerHTML = "";
    if (!res.ok) {
      list.textContent = await res.text();
      return;
    }
    const items = await res.json();
    if (!items.length) {
      list.innerHTML =
        '<div class="card muted">No interrupted threads tracked in this process. Run the sample seed or invoke a thread.</div>';
      return;
    }
    for (const item of items) {
      const card = document.createElement("div");
      card.className = "card hitl";
      card.innerHTML = `
        <div style="display:flex;gap:0.5rem;align-items:center;flex-wrap:wrap">
          <strong class="mono">${escapeHtml(item.threadId)}</strong>
          ${statusPill("Interrupted")}
          <span class="muted">step ${escapeHtml(item.step)} · ${escapeHtml(item.lastNode ?? "-")}</span>
        </div>
        <div class="muted mono">${escapeHtml(item.interruptPayload ?? "")}</div>
        <div class="hitl-actions">
          <button type="button" class="primary" data-action="approve">Approve</button>
          <button type="button" class="danger" data-action="reject">Reject</button>
          <button type="button" class="ghost" data-action="stream">SSE resume</button>
        </div>`;

      card.querySelector('[data-action="approve"]').onclick = async () => {
        await resumeThread(item.threadId, "approve", "ok");
        document.getElementById("refreshHitl").click();
      };
      card.querySelector('[data-action="reject"]').onclick = async () => {
        await resumeThread(item.threadId, "reject", "no");
        document.getElementById("refreshHitl").click();
      };
      card.querySelector('[data-action="stream"]').onclick = () => {
        document.getElementById("threadId").value = item.threadId;
        document.querySelector('[data-tab="inspector"]').click();
        stopStream();
        document.getElementById("streamLog").innerHTML = "";
        appendStreamLine("SSE resume…");
        const url = api(
          "/api/threads/" +
            encodeURIComponent(item.threadId) +
            "/stream?mode=resume&kind=approve&payload=ok"
        );
        eventSource = new EventSource(url);
        document.getElementById("stopStream").disabled = false;
        document.getElementById("startStream").disabled = true;
        eventSource.addEventListener("stream", (ev) => {
          try {
            const data = JSON.parse(ev.data);
            appendStreamLine(
              `step=${data.step} kind=${data.kind} nodes=[${(data.nodeNames || []).join(",")}]`
            );
          } catch {
            appendStreamLine(ev.data);
          }
        });
        eventSource.addEventListener("done", () => {
          appendStreamLine("— done —");
          stopStream();
          document.getElementById("refreshHitl").click();
        });
        eventSource.onerror = () => stopStream();
      };
      list.appendChild(card);
    }
  };

  async function resumeThread(threadId, kind, payload) {
    const res = await fetch(api("/api/threads/" + encodeURIComponent(threadId) + "/resume"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ kind, payload }),
    });
    const text = res.ok ? JSON.stringify(await res.json(), null, 2) : await res.text();
    window.alert(text);
  }

  document.getElementById("loadTopology").onclick = async () => {
    const res = await fetch(api("/api/topology"));
    const view = document.getElementById("topologyView");
    const out = document.getElementById("topologyOut");
    if (!res.ok) {
      view.innerHTML = `<div class="muted">${escapeHtml(await res.text())}</div>`;
      return;
    }
    const data = await res.json();
    const nodes = (data.nodes || [])
      .map((n) => `<span class="chip">${escapeHtml(n)}</span>`)
      .join("");
    const edges = (data.staticEdges || [])
      .map((e) => `<span class="chip edge">${escapeHtml(e.source)} → ${escapeHtml(e.target)}</span>`)
      .join("");
    const channels = Object.entries(data.channels || {})
      .map(([k, v]) => `<span class="chip">${escapeHtml(k)} · ${escapeHtml(v)}</span>`)
      .join("");
    view.innerHTML = `
      <div><strong>Nodes</strong><div class="topo-nodes">${nodes || "—"}</div></div>
      <div style="margin-top:1rem"><strong>Edges</strong><div class="topo-edges">${edges || "—"}</div></div>
      <div style="margin-top:1rem"><strong>Channels</strong><div class="topo-nodes">${channels || "—"}</div></div>
      <div class="muted" style="margin-top:1rem">recursionLimit=${escapeHtml(data.recursionLimit)}</div>
    `;
    out.style.display = "block";
    out.textContent = JSON.stringify(data, null, 2);
  };
})();
