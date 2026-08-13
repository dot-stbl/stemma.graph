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
          cy.fit(undefined, 48);
        }, 40);
      }
    }
    if (name === "hitl") {
      document.getElementById("refreshHitl").click();
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

  function clock() {
    const d = new Date();
    return d.toLocaleTimeString(undefined, { hour12: false });
  }

  function renderCheckpointMeta(data) {
    const channels = Object.entries(data.channelValues || {})
      .map(([k, v]) => `<dt>${escapeHtml(k)}</dt><dd>${escapeHtml(v)}</dd>`)
      .join("") || `<dt>—</dt><dd class="dim">empty</dd>`;
    const versions = Object.entries(data.channelVersions || {})
      .map(([k, v]) => `${escapeHtml(k)}@${escapeHtml(v)}`)
      .join(", ") || "—";

    document.getElementById("inspectorMeta").innerHTML = `
      <div class="meta-head">
        ${statusBadge(data.status)}
        <span class="meta-step">step ${escapeHtml(data.step)}</span>
      </div>
      <dl class="kv">
        <dt>thread</dt><dd>${escapeHtml(data.threadId)}</dd>
        <dt>last</dt><dd>${escapeHtml(data.lastNode ?? "—")}</dd>
        <dt>next</dt><dd>${escapeHtml((data.nextNodes || []).join(", ") || "—")}</dd>
        <dt>interrupt</dt><dd>${escapeHtml(data.interruptPayload ?? "—")}</dd>
        <dt>versions</dt><dd>${versions}</dd>
      </dl>
      <div class="subheader">channels</div>
      <dl class="kv">${channels}</dl>
    `;
  }

  function appendStreamLine(text, kind) {
    const log = document.getElementById("streamLog");
    const line = document.createElement("div");
    line.className = "line" + (kind ? " kind-" + kind : "");
    line.innerHTML =
      `<span class="ts">${escapeHtml(clock())}</span>` +
      `<span class="msg">${escapeHtml(text)}</span>`;
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
          `step=${data.step}  ${data.kind}  [${(data.nodeNames || []).join(",")}]  ${data.payload ?? ""}`,
          lineKind
        );
      } catch {
        appendStreamLine(ev.data);
      }
    });
    eventSource.addEventListener("done", () => {
      appendStreamLine("done", "done");
      stopStream();
      if (onDone) {
        onDone();
      }
    });
    eventSource.addEventListener("error", (ev) => {
      if (ev.data) {
        appendStreamLine("error: " + ev.data, "meta");
      } else {
        appendStreamLine("connection closed", "meta");
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
        `<div class="placeholder"><p>Not found (${res.status}).</p></div>`;
      return;
    }
    const data = await res.json();
    renderCheckpointMeta(data);
    out.textContent = JSON.stringify(data, null, 2);
  };

  document.getElementById("startStream").onclick = () => {
    const id = document.getElementById("threadId").value.trim();
    if (!id) {
      appendStreamLine("thread id required", "meta");
      return;
    }
    bindStream(api("/api/threads/" + encodeURIComponent(id) + "/stream?mode=checkpoint"));
  };

  document.getElementById("stopStream").onclick = stopStream;

  function setHitlCount(n) {
    const el = document.getElementById("hitlCount");
    if (n > 0) {
      el.hidden = false;
      el.textContent = String(n);
    } else {
      el.hidden = true;
    }
  }

  document.getElementById("refreshHitl").onclick = async () => {
    const res = await fetch(api("/api/hitl"));
    const list = document.getElementById("hitlList");
    list.innerHTML = "";
    if (!res.ok) {
      list.innerHTML = `<div class="alert alert-danger">${escapeHtml(await res.text())}</div>`;
      setHitlCount(0);
      return;
    }
    const items = await res.json();
    setHitlCount(items.length);
    if (!items.length) {
      list.innerHTML = `
        <div class="empty">
          <p class="empty-title">No interrupts</p>
          <p class="empty-sub">Sample seeds <code>ui-host-hitl-1</code> on startup.</p>
        </div>`;
      return;
    }

    const table = document.createElement("table");
    table.className = "hitl-table";
    table.innerHTML = `
      <thead>
        <tr>
          <th>Thread</th>
          <th>Status</th>
          <th>Step</th>
          <th>Node</th>
          <th>Payload</th>
          <th></th>
        </tr>
      </thead>
      <tbody></tbody>`;
    const tbody = table.querySelector("tbody");

    for (const item of items) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td class="thread">${escapeHtml(item.threadId)}</td>
        <td>${statusBadge("Interrupted")}</td>
        <td class="mono dim">${escapeHtml(item.step)}</td>
        <td class="mono">${escapeHtml(item.lastNode ?? "—")}</td>
        <td class="payload" title="${escapeHtml(item.interruptPayload ?? "")}">${escapeHtml(item.interruptPayload ?? "")}</td>
        <td>
          <div class="acts">
            <button type="button" class="btn btn-ok btn-sm" data-action="approve">Approve</button>
            <button type="button" class="btn btn-bad btn-sm" data-action="reject">Reject</button>
            <button type="button" class="btn btn-sm" data-action="open">Open</button>
          </div>
        </td>`;
      tr.querySelector('[data-action="approve"]').onclick = async () => {
        await resumeThread(item.threadId, "approve", "ok");
        document.getElementById("refreshHitl").click();
      };
      tr.querySelector('[data-action="reject"]').onclick = async () => {
        await resumeThread(item.threadId, "reject", "no");
        document.getElementById("refreshHitl").click();
      };
      tr.querySelector('[data-action="open"]').onclick = () => {
        document.getElementById("threadId").value = item.threadId;
        setTab("inspector");
        document.getElementById("loadThread").click();
      };
      tbody.appendChild(tr);
    }
    list.appendChild(table);
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
    // Architecture-diagram style: monochrome boxes, orthogonal edges (not “toy” bubbles).
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
            "font-family": "ui-monospace, Cascadia Code, Consolas, monospace",
            "font-weight": 500,
            color: "#e4e4e7",
            "background-color": "#111113",
            "background-opacity": 1,
            "border-width": 1,
            "border-color": "#52525b",
            "border-opacity": 1,
            width: "label",
            height: 36,
            padding: "10px 16px",
            shape: "rectangle",
            "text-max-width": 160,
            "text-wrap": "none",
          },
        },
        {
          selector: "node.start, node.terminal",
          style: {
            "background-color": "#09090b",
            "border-color": "#71717a",
            "border-style": "dashed",
            color: "#a1a1aa",
            shape: "rectangle",
            height: 30,
            "font-size": 11,
          },
        },
        {
          selector: "node:selected",
          style: {
            "border-width": 1.5,
            "border-color": "#a9c9ff",
            "border-style": "solid",
            color: "#fafafa",
          },
        },
        {
          selector: "edge",
          style: {
            width: 1,
            "line-color": "#52525b",
            "target-arrow-color": "#52525b",
            "target-arrow-shape": "triangle",
            "arrow-scale": 0.75,
            "curve-style": "taxi",
            "taxi-direction": "auto",
            "taxi-turn": 16,
            label: "data(label)",
            "font-size": 10,
            "font-family": "ui-monospace, Consolas, monospace",
            color: "#71717a",
            "text-background-color": "#09090b",
            "text-background-opacity": 1,
            "text-background-padding": 2,
            "text-margin-y": -6,
          },
        },
        {
          selector: "edge:selected",
          style: {
            "line-color": "#a1a1aa",
            "target-arrow-color": "#a1a1aa",
            width: 1.5,
          },
        },
      ],
      layout: { name: "preset" },
      wheelSensitivity: 0.2,
      minZoom: 0.25,
      maxZoom: 2.5,
    });

    cy.on("tap", "node", (event) => {
      const node = event.target;
      document.getElementById("topoSelection").innerHTML = `
        <div class="meta-head">${statusBadge("Node")}</div>
        <dl class="kv">
          <dt>id</dt><dd>${escapeHtml(node.id())}</dd>
          <dt>kind</dt><dd>${escapeHtml(node.data("kind") || "node")}</dd>
        </dl>`;
    });
    cy.on("tap", "edge", (event) => {
      const edge = event.target;
      document.getElementById("topoSelection").innerHTML = `
        <div class="meta-head"><span class="badge badge-edge">edge</span></div>
        <dl class="kv">
          <dt>from</dt><dd>${escapeHtml(edge.data("source"))}</dd>
          <dt>to</dt><dd>${escapeHtml(edge.data("target"))}</dd>
          <dt>label</dt><dd>${escapeHtml(edge.data("label") || "—")}</dd>
        </dl>`;
    });
    cy.on("tap", (event) => {
      if (event.target === cy) {
        document.getElementById("topoSelection").innerHTML =
          `<p class="dim">Select a node or edge.</p>`;
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
      document.getElementById("topoChannels").innerHTML = `<p class="dim">No channels.</p>`;
      return;
    }
    document.getElementById("topoChannels").innerHTML =
      `<div class="chip-row">${channels
        .map(([k, v]) => `<span class="chip">${escapeHtml(k)} · ${escapeHtml(v)}</span>`)
        .join("")}</div>
       <p class="dim mono small" style="margin-top:10px">recursionLimit=${escapeHtml(data.recursionLimit)}</p>`;
  }

  function paintTopology(data) {
    lastTopology = data;
    const host = ensureCy();
    if (!host) {
      document.getElementById("topoSelection").innerHTML =
        `<div class="alert alert-warning">Cytoscape CDN failed to load.</div>`;
      return;
    }
    const elements = topologyToElements(data);
    host.elements().remove();
    host.add(elements);
    const layoutName = window.cytoscapeDagre ? "dagre" : "breadthfirst";
    host.layout({
      name: layoutName,
      rankDir: "TB",
      nodeSep: 28,
      edgeSep: 12,
      rankSep: 48,
      animate: false,
      padding: 48,
      directed: true,
      spacingFactor: 1.05,
    }).run();
    host.fit(undefined, 56);
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
      cy.fit(undefined, 48);
    } else if (lastTopology) {
      paintTopology(lastTopology);
    }
  };

  // Prefetch interrupt count for rail badge
  fetch(api("/api/hitl"))
    .then((r) => (r.ok ? r.json() : []))
    .then((items) => setHitlCount(Array.isArray(items) ? items.length : 0))
    .catch(() => {});
})();
