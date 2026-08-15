import type { Edge, Node } from "@xyflow/react";
import dagre from "dagre";
import type { TopologyDescription } from "@/api/types";

export const NODE_WIDTH = 148;
export const NODE_HEIGHT = 36;
export const FLOW_DIRECTION = "LR";

const TERMINALS = new Set(["START", "__START__", "END", "__END__"]);

export function isTerminal(name: string): boolean {
  return TERMINALS.has(name.toUpperCase());
}

export type FlowNodeKind = "normal" | "conditional" | "terminal";
export type FlowNodeHighlight = "current" | "next" | null;

export interface FlowNodeData {
  label: string;
  kind: FlowNodeKind;
  highlight: FlowNodeHighlight;
}

export interface FlowHighlight {
  /** Node currently executing (brand border + pulse). */
  current?: string | null;
  /** Candidate next nodes (dashed brand border). */
  next?: string[] | null;
}

export interface FlowGraph {
  nodes: Node[];
  edges: Edge[];
}

export function layoutGraph(
  topology: TopologyDescription,
  conditional: Set<string>,
  highlight?: FlowHighlight,
): FlowGraph {
  const graph = new dagre.graphlib.Graph();
  graph.setDefaultEdgeLabel(() => ({}));
  graph.setGraph({
    rankdir: FLOW_DIRECTION,
    nodesep: 56,
    ranksep: 96,
    marginx: 40,
    marginy: 40,
  });

  const nodeNames = new Set(topology.nodes);
  for (const edge of topology.staticEdges) {
    nodeNames.add(edge.source);
    nodeNames.add(edge.target);
  }

  for (const name of nodeNames) {
    graph.setNode(name, { width: NODE_WIDTH, height: NODE_HEIGHT });
  }
  for (const edge of topology.staticEdges) {
    graph.setEdge(edge.source, edge.target);
  }

  dagre.layout(graph);

  const nextSet = new Set(highlight?.next ?? []);

  const nodes: Node[] = Array.from(nodeNames).map((name) => {
    const positioned = graph.node(name);
    const nodeHighlight: FlowNodeHighlight =
      highlight?.current === name
        ? "current"
        : nextSet.has(name)
          ? "next"
          : null;
    return {
      id: name,
      type: "voluta",
      position: {
        x: positioned.x - NODE_WIDTH / 2,
        y: positioned.y - NODE_HEIGHT / 2,
      },
      data: {
        label: isTerminal(name) ? name.toUpperCase() : name,
        kind: isTerminal(name)
          ? "terminal"
          : conditional.has(name)
            ? "conditional"
            : "normal",
        highlight: nodeHighlight,
      } satisfies FlowNodeData as unknown as Record<string, unknown>,
      draggable: false,
      selectable: false,
      connectable: false,
    };
  });

  const edges: Edge[] = topology.staticEdges.map((edge, index) => ({
    id: `e-${edge.source}-${edge.target}-${index}`,
    source: edge.source,
    target: edge.target,
    type: "smoothstep",
  }));

  return { nodes, edges };
}
