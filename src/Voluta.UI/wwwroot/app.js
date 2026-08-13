(() => {
  const baseEl = document.querySelector("base");
  const base = (baseEl && baseEl.getAttribute("href")
    ? baseEl.getAttribute("href").replace(/\/$/, "")
    : location.pathname.replace(/\/?(index\.html)?$/, "")) || "/voluta";

  const api = (path) => base + path;
  let eventSource = null;
  let cy = null;
  let lastTopology = null;
  let topologyLoaded = false;

  const pathHint = document.getElementById("pathHint");
  if (pathHint) {
    pathHint.textContent = base;
  }

  if (window.cytoscape && window.cytoscapeDagre) {
    cytoscape.use(cytoscapeDagre);
  }

  function setTab(name) {
    document.querySelectorAll("[data-tab]").forEach((btn) => {
      const active = btn.dataset.tab === name;
      btn.classList.toggle("active", active);
      btn.setAttribute("aria-selected", active ? "true" : "false");
    });
    document.querySelectorAll(".panel").forEach((panel) => {
      const active = panel.id === "panel-" + name;
      panel.classList.toggle("active", active);
      if (active) {
        panel.removeAttribute("hidden");
      } else {
        panel.setAttribute("hidden", "");
      }
    });
    if (name === "topology") {
      if (!topologyLoaded) {
        topologyLoaded = true;
        document.getElementById("loadTopology").click();
      } else if (cy) {
        setTimeout(() => {
          cy.resize();
          cy.fit(undefined, 40);
        }, 50);
      }
    }
  }

  document.querySelectorAll("[data-tab]").forEach((btn) => {
    btn.addEventListener("click", () => setTab(btn.dataset.tab));
  });

  function statusBadge(status) {
    const s = (status || "").toString();
    const cls =
      s === "Done" ? "badge-done" :
      s === "Interrupted" ? "badge-interrupted" :
      s === "Failed" || s === "Cancelled" ? "badge-failed" :
      s === "Node" ? "badge-node" :
      "badge-muted";
    return `<span class="badge ${cls}">${escapeHtml(s || "—")}</span>`;
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
        ${statusBadge(data.status)}
        <span class="meta-step">step ${escapeHtml(data.step)}</span>
      </div>
      <dl class="kv">
        <dt>Thread</dt><dd>${escapeHtml(data.threadId)}</dd>
        <dt>Last node</dt><dd>${escapeHtml(data.lastNode ?? "—")}</dd>
        <dt>Next</dt><dd>${escapeHtml((data.nextNodes || []).join(", ") || "—")}</dd>
        <dt>Interrupt</dt><dd>${escapeHtml(data.interruptPayload ?? "—")}</dd>
        <dt>Versions</dt><dd>${versions}</dd>
      </dl>
      <div class="subheader">Channels</div>
      <dl class="kv">${channels}</dl>
    `;
  }

  function appendStreamLine(text, kind) {
    const log = document.getElementById("streamLog");
    const line = document.createElement("div");
    line.className = "line" + (kind ? " kind-" + kind : "");
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
    appendStreamLine("connecting…", "meta");
    eventSource = new EventSource(url);
    document.getElementById("stopStream").disabled = false;
    document.getElementById("startStream").disabled = true;

    eventSource.addEventListener("stream", (ev) => {
      try {
        const data = JSON.parse(ev.data);
        const kind = (data.kind || "").toString().toLowerCase();
        const lineKind = kind.includes("interrupt") ? "interrupt" : "";
        appendStreamLine(
          `step=${data.step} kind=${data.kind} nodes=[${(data.nodeNames || []).join(",")}] payload=${data.payload ?? ""}`,
          lineKind
        );
      } catch {
        appendStreamLine(ev.data);
      }
    });
    eventSource.addEventListener("done", () => {
      appendStreamLine("— done —", "done");
      stopStream();
      if (onDone) {
        onDone();
      }
    });
    eventSource.addEventListener("error", (ev) => {
      if (ev.data) {
        appendStreamLine("error: " + ev.data);
      } else {
        appendStreamLine("connection closed / error", "meta");
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
      appendStreamLine("Enter a thread id first.", "meta");
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
      list.innerHTML = `<div class="alert alert-danger">${escapeHtml(await res.text())}</div>`;
      return;
    }
    const items = await res.json();
    if (!items.length) {
      list.innerHTML = `
        <div class="empty">
          <p class="empty-title">No interrupted threads</p>
          <p class="empty-sub">
            Sample seeds <code>ui-host-hitl-1</code> on startup. Resume clears the queue.
          </p>
        </div>`;
      return;
    }
    for (const item of items) {
      const card = document.createElement("article");
      card.className = "card hitl-card";
      card.innerHTML = `
        <div class="hitl-card-head">
          <strong>${escapeHtml(item.threadId)}</strong>
          ${statusBadge("Interrupted")}
        </div>
        <div class="hitl-meta">
          step ${escapeHtml(item.step)} · node ${escapeHtml(item.lastNode ?? "-")}
        </div>
        <div class="hitl-payload">${escapeHtml(item.interruptPayload ?? "")}</div>
        <div class="btn-row">
          <button type="button" class="btn btn-success btn-sm" data-action="approve">Approve</button>
          <button type="button" class="btn btn-danger-outline btn-sm" data-action="reject">Reject</button>
          <button type="button" class="btn btn-ghost-brand btn-sm" data-action="stream">SSE resume</button>
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

  function ensureCy() {
    if (cy || !window.cytoscape) {
      return cy;
    }
    cy = cytoscape({
      container: document.getElementById("cy"),
      style: [
        {
          selector: "node",
          style: {
            label: "data(label)",
            "text-valign": "center",
            "text-halign": "center",
            "font-size": 12,
            "font-family": "IBM Plex Mono, ui-monospace, Consolas, monospace",
            color: "#e8eef8",
            "background-color": "#1c3a7a",
            "background-opacity": 0.92,
            "border-width": 1.5,
            "border-color": "#a9c9ff",
            width: "label",
            height: "label",
            padding: "14px",
            shape: "round-rectangle",
          },
        },
        {
          selector: "node.terminal",
          style: {
            "background-color": "#1a4a24",
            "border-color": "#a6ff86",
            shape: "ellipse",
          },
        },
        {
          selector: "node.start",
          style: {
            "background-color": "#6b1444",
            "border-color": "#ff9bd8",
            shape: "ellipse",
          },
        },
        {
          selector: "node:selected",
          style: {
            "border-width": 3,
            "border-color": "#ffd68a",
          },
        },
        {
          selector: "edge",
          style: {
            width: 2,
            "line-color": "#4a5d7a",
            "target-arrow-color": "#4a5d7a",
            "target-arrow-shape": "triangle",
            "curve-style": "bezier",
            label: "data(label)",
            "font-size": 10,
            "font-family": "IBM Plex Mono, ui-monospace, Consolas, monospace",
            color: "#8b9bb8",
            "text-rotation": "autorotate",
            "text-margin-y": -8,
          },
        },
        {
          selector: "edge:selected",
          style: {
            "line-color": "#ffd68a",
            "target-arrow-color": "#ffd68a",
            width: 3,
          },
        },
      ],
      layout: { name: "preset" },
      wheelSensitivity: 0.25,
      minZoom: 0.2,
      maxZoom: 2.5,
    });

    cy.on("tap", "node", (event) => {
      const node = event.target;
      document.getElementById("topoSelection").innerHTML = `
        <div class="meta-head">${statusBadge("Node")}</div>
        <dl class="kv">
          <dt>Id</dt><dd>${escapeHtml(node.id())}</dd>
          <dt>Label</dt><dd>${escapeHtml(node.data("label"))}</dd>
          <dt>Kind</dt><dd>${escapeHtml(node.data("kind") || "node")}</dd>
        </dl>`;
    });
    cy.on("tap", "edge", (event) => {
      const edge = event.target;
      document.getElementById("topoSelection").innerHTML = `
        <div class="meta-head"><span class="badge badge-edge">Edge</span></div>
        <dl class="kv">
          <dt>From</dt><dd>${escapeHtml(edge.data("source"))}</dd>
          <dt>To</dt><dd>${escapeHtml(edge.data("target"))}</dd>
          <dt>Label</dt><dd>${escapeHtml(edge.data("label") || "—")}</dd>
        </dl>`;
    });
    cy.on("tap", (event) => {
      if (event.target === cy) {
        document.getElementById("topoSelection").innerHTML =
          `<p class="muted">Click a node or edge on the graph.</p>`;
      }
    });
    return cy;
  }

  function topologyToElements(data) {
    const nodeIds = new Set();
    const elements = [];

    function addNode(id, kind) {
      if (!id || nodeIds.has(id)) {
        return;
      }
      nodeIds.add(id);
      const isStart = id === "__start__" || id === "START" || kind === "start";
      const isEnd = id === "__end__" || id === "END" || kind === "end";
      elements.push({
        data: {
          id,
          label: id,
          kind: isStart ? "start" : isEnd ? "end" : "node",
        },
        classes: isStart ? "start" : isEnd ? "terminal" : "",
      });
    }

    for (const n of data.nodes || []) {
      addNode(n, "node");
    }
    for (const e of data.staticEdges || []) {
      addNode(e.source);
      addNode(e.target);
      elements.push({
        data: {
          id: `e:${e.source}->${e.target}`,
          source: e.source,
          target: e.target,
          label: "",
        },
      });
    }
    for (const c of data.conditionalSources || []) {
      const source = c.source || c.from;
      if (!source) {
        continue;
      }
      addNode(source);
      for (const branch of c.targets || c.branches || []) {
        const target = typeof branch === "string" ? branch : branch.target || branch.to;
        const label = typeof branch === "string" ? "" : branch.label || branch.when || "";
        if (!target) {
          continue;
        }
        addNode(target);
        elements.push({
          data: {
            id: `c:${source}->${target}:${label}`,
            source,
            target,
            label: label || "?",
          },
        });
      }
    }
    return elements;
  }

  function renderChannels(data) {
    const channels = Object.entries(data.channels || {});
    if (!channels.length) {
      document.getElementById("topoChannels").innerHTML =
        `<p class="muted">No channels.</p>`;
      return;
    }
    document.getElementById("topoChannels").innerHTML =
      `<div class="chip-row">${channels
        .map(([k, v]) => `<span class="chip">${escapeHtml(k)} · ${escapeHtml(v)}</span>`)
        .join("")}</div>
       <p class="muted" style="margin-top:0.65rem;font-size:0.75rem;font-family:var(--mono)">recursionLimit=${escapeHtml(data.recursionLimit)}</p>`;
  }

  function paintTopology(data) {
    lastTopology = data;
    const host = ensureCy();
    if (!host) {
      document.getElementById("topoSelection").innerHTML =
        `<div class="alert alert-warning">Cytoscape failed to load (CDN). Check network.</div>`;
      return;
    }
    const elements = topologyToElements(data);
    host.elements().remove();
    host.add(elements);
    const layoutName = window.cytoscapeDagre ? "dagre" : "breadthfirst";
    host.layout({
      name: layoutName,
      rankDir: "LR",
      nodeSep: 40,
      edgeSep: 20,
      rankSep: 60,
      animate: false,
      padding: 30,
      directed: true,
    }).run();
    host.fit(undefined, 40);
    renderChannels(data);
    document.getElementById("topologyOut").textContent = JSON.stringify(data, null, 2);
  }

  document.getElementById("loadTopology").onclick = async () => {
    const res = await fetch(api("/api/topology"));
    if (!res.ok) {
      document.getElementById("topoSelection").innerHTML =
        `<div class="alert alert-danger">${escapeHtml(await res.text())}</div>`;
      return;
    }
    paintTopology(await res.json());
  };

  document.getElementById("fitTopology").onclick = () => {
    if (cy) {
      cy.resize();
      cy.fit(undefined, 40);
    } else if (lastTopology) {
      paintTopology(lastTopology);
    }
  };
})();
