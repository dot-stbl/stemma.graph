(() => {
  const baseEl = document.querySelector("base");
  const base = (baseEl && baseEl.getAttribute("href")
    ? baseEl.getAttribute("href").replace(/\/$/, "")
    : location.pathname.replace(/\/?(index\.html)?$/, "")) || "/voluta";

  const api = (path) => base + path;
  let eventSource = null;

  const pathHint = document.getElementById("pathHint");
  if (pathHint) {
    pathHint.textContent = base;
  }

  function setTab(name) {
    document.querySelectorAll(".tabs button").forEach((btn) => {
      const on = btn.dataset.tab === name;
      btn.classList.toggle("active", on);
      btn.classList.toggle("outline", !on);
    });
    document.querySelectorAll(".panel").forEach((panel) => {
      panel.classList.toggle("active", panel.id === "panel-" + name);
    });
  }

  document.querySelectorAll(".tabs button").forEach((btn) => {
    btn.addEventListener("click", () => setTab(btn.dataset.tab));
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
      <div class="meta-head">
        ${statusPill(data.status)}
        <span class="mono muted">step ${escapeHtml(data.step)}</span>
      </div>
      <dl class="kv">
        <dt>Thread</dt><dd>${escapeHtml(data.threadId)}</dd>
        <dt>Last node</dt><dd>${escapeHtml(data.lastNode ?? "—")}</dd>
        <dt>Next</dt><dd>${escapeHtml((data.nextNodes || []).join(", ") || "—")}</dd>
        <dt>Interrupt</dt><dd>${escapeHtml(data.interruptPayload ?? "—")}</dd>
        <dt>Versions</dt><dd>${versions}</dd>
      </dl>
      <p class="block-label">Channels</p>
      <dl class="kv" style="margin-top:0.4rem">${channels}</dl>
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

  function bindStream(url, onDone) {
    stopStream();
    document.getElementById("streamLog").innerHTML = "";
    appendStreamLine("connecting…");
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
      if (onDone) {
        onDone();
      }
    });
    eventSource.addEventListener("error", (ev) => {
      if (ev.data) {
        appendStreamLine("error: " + ev.data);
      } else {
        appendStreamLine("connection closed / error");
      }
      stopStream();
    });
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
        `<p class="muted">Not found or error (${res.status}).</p>`;
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
    bindStream(api("/api/threads/" + encodeURIComponent(id) + "/stream?mode=checkpoint"));
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
        '<article><p class="muted">No interrupted threads. Seed runs on sample startup (ui-host-hitl-1).</p></article>';
      return;
    }
    for (const item of items) {
      const card = document.createElement("article");
      card.className = "hitl-card";
      card.innerHTML = `
        <header>
          <strong class="mono">${escapeHtml(item.threadId)}</strong>
          ${statusPill("Interrupted")}
          <small class="muted mono">step ${escapeHtml(item.step)} · ${escapeHtml(item.lastNode ?? "-")}</small>
        </header>
        <p class="payload muted">${escapeHtml(item.interruptPayload ?? "")}</p>
        <div class="hitl-actions">
          <button type="button" data-action="approve">Approve</button>
          <button type="button" class="secondary" data-action="reject">Reject</button>
          <button type="button" class="secondary outline" data-action="stream">SSE resume</button>
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
        setTab("inspector");
        bindStream(
          api(
            "/api/threads/" +
              encodeURIComponent(item.threadId) +
              "/stream?mode=resume&kind=approve&payload=ok"
          ),
          () => document.getElementById("refreshHitl").click()
        );
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
    const raw = document.getElementById("topologyRaw");
    const out = document.getElementById("topologyOut");
    if (!res.ok) {
      view.innerHTML = `<p class="muted">${escapeHtml(await res.text())}</p>`;
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
      <p class="block-label">Nodes</p>
      <div class="chip-row">${nodes || "—"}</div>
      <p class="block-label">Edges</p>
      <div class="chip-row">${edges || "—"}</div>
      <p class="block-label">Channels</p>
      <div class="chip-row">${channels || "—"}</div>
      <p class="muted mono" style="margin:0.5rem 0 0">recursionLimit=${escapeHtml(data.recursionLimit)}</p>
    `;
    raw.hidden = false;
    out.textContent = JSON.stringify(data, null, 2);
  };
})();
