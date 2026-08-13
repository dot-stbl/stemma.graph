(() => {
  const baseEl = document.querySelector("base");
  const base = (baseEl && baseEl.getAttribute("href")
    ? baseEl.getAttribute("href").replace(/\/$/, "")
    : location.pathname.replace(/\/?(index\.html)?$/, "")) || "/voluta";

  const api = (path) => base + path;
  let eventSource = null;
  let cy = null;
  let lastTopology = null;

  const pathHint = document.getElementById("pathHint");
  if (pathHint) {
    pathHint.textContent = base;
  }

  if (window.cytoscape && window.cytoscapeDagre) {
    cytoscape.use(cytoscapeDagre);
  }

  function setTab(name) {
    document.querySelectorAll("[data-tab]").forEach((btn) => {
      btn.classList.toggle("active", btn.dataset.tab === name);
    });
    document.querySelectorAll(".panel").forEach((panel) => {
      panel.classList.toggle("active", panel.id === "panel-" + name);
    });
    if (name === "topology" && cy) {
      setTimeout(() => {
        cy.resize();
        cy.fit(undefined, 40);
      }, 50);
    }
  }

  document.querySelectorAll("[data-tab]").forEach((btn) => {
    btn.addEventListener("click", () => setTab(btn.dataset.tab));
  });

  function statusBadge(status) {
    const s = (status || "").toString();
    const cls =
      s === "Done" ? "bg-green-lt text-green" :
      s === "Interrupted" ? "bg-yellow-lt text-yellow" :
      s === "Failed" || s === "Cancelled" ? "bg-red-lt text-red" :
      "bg-secondary-lt";
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
      .join("") || `<dt>—</dt><dd class="text-secondary">no channels</dd>`;
    const versions = Object.entries(data.channelVersions || {})
      .map(([k, v]) => `${escapeHtml(k)}@${escapeHtml(v)}`)
      .join(", ") || "—";

    document.getElementById("inspectorMeta").innerHTML = `
      <div class="meta-head">
        ${statusBadge(data.status)}
        <span class="font-monospace text-secondary">step ${escapeHtml(data.step)}</span>
      </div>
      <dl class="kv">
        <dt>Thread</dt><dd>${escapeHtml(data.threadId)}</dd>
        <dt>Last node</dt><dd>${escapeHtml(data.lastNode ?? "—")}</dd>
        <dt>Next</dt><dd>${escapeHtml((data.nextNodes || []).join(", ") || "—")}</dd>
        <dt>Interrupt</dt><dd>${escapeHtml(data.interruptPayload ?? "—")}</dd>
        <dt>Versions</dt><dd>${versions}</dd>
      </dl>
      <div class="subheader mt-3 mb-2">Channels</div>
      <dl class="kv">${channels}</dl>
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
        `<p class="text-secondary mb-0">Not found or error (${res.status}).</p>`;
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
      list.innerHTML = `<div class="col-12"><div class="alert alert-danger">${escapeHtml(await res.text())}</div></div>`;
      return;
    }
    const items = await res.json();
    if (!items.length) {
      list.innerHTML = `
        <div class="col-12">
          <div class="empty">
            <p class="empty-title">No interrupted threads</p>
            <p class="empty-subtitle text-secondary">
              Sample seeds <code>ui-host-hitl-1</code> on startup. Resume clears the queue.
            </p>
          </div>
        </div>`;
      return;
    }
    for (const item of items) {
      const col = document.createElement("div");
      col.className = "col-md-6 col-lg-4";
      col.innerHTML = `
        <div class="card">
          <div class="card-body">
            <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
              <strong class="font-monospace">${escapeHtml(item.threadId)}</strong>
              ${statusBadge("Interrupted")}
            </div>
            <div class="text-secondary small mb-1">
              step ${escapeHtml(item.step)} · node <span class="font-monospace">${escapeHtml(item.lastNode ?? "-")}</span>
            </div>
            <div class="hitl-payload">${escapeHtml(item.interruptPayload ?? "")}</div>
            <div class="btn-list">
              <button type="button" class="btn btn-success btn-sm" data-action="approve">Approve</button>
              <button type="button" class="btn btn-outline-danger btn-sm" data-action="reject">Reject</button>
              <button type="button" class="btn btn-outline-azure btn-sm" data-action="stream">SSE resume</button>
            </div>
          </div>
        </div>`;
      col.querySelector('[data-action="approve"]').onclick = async () => {
        await resumeThread(item.threadId, "approve", "ok");
        document.getElementById("refreshHitl").click();
      };
      col.querySelector('[data-action="reject"]').onclick = async () => {
        await resumeThread(item.threadId, "reject", "no");
        document.getElementById("refreshHitl").click();
      };
      col.querySelector('[data-action="stream"]').onclick = () => {
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
      list.appendChild(col);
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
            "font-family": "ui-monospace, Consolas, monospace",
            color: "#e6e7e9",
            "background-color": "#206bc4",
            "border-width": 1,
            "border-color": "#4299e1",
            width: "label",
            height: "label",
            padding: "14px",
            shape: "round-rectangle",
          },
        },
        {
          selector: "node.terminal",
          style: {
            "background-color": "#2fb344",
            "border-color": "#5ecf72",
            shape: "ellipse",
          },
        },
        {
          selector: "node.start",
          style: {
            "background-color": "#ae3ec9",
            "border-color": "#cc5de8",
            shape: "ellipse",
          },
        },
        {
          selector: "node:selected",
          style: {
            "border-width": 3,
            "border-color": "#ffd43b",
          },
        },
        {
          selector: "edge",
          style: {
            width: 2,
            "line-color": "#6c7a91",
            "target-arrow-color": "#6c7a91",
            "target-arrow-shape": "triangle",
            "curve-style": "bezier",
            label: "data(label)",
            "font-size": 10,
            color: "#9aa4b2",
            "text-rotation": "autorotate",
            "text-margin-y": -8,
          },
        },
        {
          selector: "edge:selected",
          style: {
            "line-color": "#ffd43b",
            "target-arrow-color": "#ffd43b",
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
        <div class="mb-2">${statusBadge("Node")}</div>
        <dl class="kv">
          <dt>Id</dt><dd>${escapeHtml(node.id())}</dd>
          <dt>Label</dt><dd>${escapeHtml(node.data("label"))}</dd>
          <dt>Kind</dt><dd>${escapeHtml(node.data("kind") || "node")}</dd>
        </dl>`;
    });
    cy.on("tap", "edge", (event) => {
      const edge = event.target;
      document.getElementById("topoSelection").innerHTML = `
        <div class="mb-2"><span class="badge bg-purple-lt text-purple">Edge</span></div>
        <dl class="kv">
          <dt>From</dt><dd>${escapeHtml(edge.data("source"))}</dd>
          <dt>To</dt><dd>${escapeHtml(edge.data("target"))}</dd>
          <dt>Label</dt><dd>${escapeHtml(edge.data("label") || "—")}</dd>
        </dl>`;
    });
    cy.on("tap", (event) => {
      if (event.target === cy) {
        document.getElementById("topoSelection").innerHTML =
          `<p class="text-secondary mb-0">Click a node or edge on the graph.</p>`;
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
        `<p class="text-secondary mb-0">No channels.</p>`;
      return;
    }
    document.getElementById("topoChannels").innerHTML =
      `<div class="chip-row">${channels
        .map(([k, v]) => `<span class="chip">${escapeHtml(k)} · ${escapeHtml(v)}</span>`)
        .join("")}</div>
       <div class="text-secondary small mt-2">recursionLimit=${escapeHtml(data.recursionLimit)}</div>`;
  }

  function paintTopology(data) {
    lastTopology = data;
    const host = ensureCy();
    if (!host) {
      document.getElementById("topoSelection").innerHTML =
        `<div class="alert alert-warning mb-0">Cytoscape failed to load (CDN). Check network.</div>`;
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
        `<div class="alert alert-danger mb-0">${escapeHtml(await res.text())}</div>`;
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

  // Auto-load topology graph when opening tab first time
  let topologyLoaded = false;
  const originalSetTab = setTab;
  // re-bind topology auto-load on tab buttons
  document.querySelectorAll('[data-tab="topology"]').forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!topologyLoaded) {
        topologyLoaded = true;
        document.getElementById("loadTopology").click();
      }
    });
  });
})();
