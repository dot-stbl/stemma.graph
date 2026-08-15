import { describe, expect, it } from "vitest";
import type { TopologyDescription } from "@/api/types";
import { isTerminal, layoutGraph } from "@/lib/flowLayout";

const topology: TopologyDescription = {
  nodes: ["START", "plan", "execute", "report", "END"],
  channels: { messages: "addMessages" },
  staticEdges: [
    { source: "START", target: "plan" },
    { source: "plan", target: "execute" },
    { source: "execute", target: "report" },
    { source: "report", target: "END" },
  ],
  conditionalSources: ["execute"],
};

describe("isTerminal", () => {
  it("recognizes terminal node names", () => {
    expect(isTerminal("START")).toBe(true);
    expect(isTerminal("__start__")).toBe(true);
    expect(isTerminal("end")).toBe(true);
    expect(isTerminal("plan")).toBe(false);
  });
});

describe("layoutGraph", () => {
  it("creates a node per topology node with numeric positions", () => {
    const { nodes } = layoutGraph(topology, new Set(["execute"]));

    expect(nodes.map((node) => node.id).sort()).toEqual(
      ["END", "START", "execute", "plan", "report"].sort(),
    );
    for (const node of nodes) {
      expect(typeof node.position.x).toBe("number");
      expect(typeof node.position.y).toBe("number");
      expect(Number.isFinite(node.position.x)).toBe(true);
      expect(Number.isFinite(node.position.y)).toBe(true);
    }
  });

  it("maps every static edge", () => {
    const { edges } = layoutGraph(topology, new Set());

    expect(edges).toHaveLength(topology.staticEdges.length);
    for (const edge of edges) {
      expect(edge.source).toBeTruthy();
      expect(edge.target).toBeTruthy();
    }
    expect(edges[0]).toMatchObject({
      source: "START",
      target: "plan",
    });
  });

  it("marks terminal and conditional nodes via data.kind", () => {
    const { nodes } = layoutGraph(topology, new Set(["execute"]));
    const byId = new Map(nodes.map((node) => [node.id, node]));

    expect(byId.get("START")?.data).toMatchObject({ kind: "terminal" });
    expect(byId.get("execute")?.data).toMatchObject({ kind: "conditional" });
    expect(byId.get("plan")?.data).toMatchObject({ kind: "normal" });
  });

  it("applies checkpoint highlight to current and next nodes", () => {
    const { nodes } = layoutGraph(topology, new Set(), {
      current: "execute",
      next: ["report"],
    });
    const byId = new Map(nodes.map((node) => [node.id, node]));

    expect(byId.get("execute")?.data).toMatchObject({ highlight: "current" });
    expect(byId.get("report")?.data).toMatchObject({ highlight: "next" });
    expect(byId.get("plan")?.data).toMatchObject({ highlight: null });
    expect(byId.get("START")?.data).toMatchObject({ highlight: null });
  });

  it("includes edge-only nodes missing from the nodes list", () => {
    const partial: TopologyDescription = {
      nodes: ["a"],
      channels: {},
      staticEdges: [{ source: "a", target: "b" }],
    };
    const { nodes } = layoutGraph(partial, new Set());

    expect(nodes.map((node) => node.id).sort()).toEqual(["a", "b"]);
  });
});
