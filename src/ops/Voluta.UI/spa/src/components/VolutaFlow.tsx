import { makeStyles, tokens } from "@fluentui/react-components";
import {
  Background,
  Controls,
  Handle,
  Position,
  ReactFlow,
  type Edge,
  type Node,
  type NodeProps,
} from "@xyflow/react";
import { useMemo } from "react";
import type { TopologyDescription } from "@/api/types";
import {
  layoutGraph,
  type FlowHighlight,
  type FlowNodeData,
  type FlowNodeKind,
} from "@/lib/flowLayout";
import "@xyflow/react/dist/style.css";

const useStyles = makeStyles({
  canvas: {
    height: "560px",
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  node: {
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground1,
  },
  nodeConditional: {
    border: `1px solid ${tokens.colorPaletteMarigoldForeground2}`,
    color: tokens.colorPaletteMarigoldForeground2,
  },
  nodeTerminal: {
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    letterSpacing: "0.06em",
  },
  nodeCurrent: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
    animationName: "voluta-node-pulse",
    animationDuration: "1.6s",
    animationIterationCount: "infinite",
  },
  nodeNext: {
    border: `1px dashed ${tokens.colorBrandStroke1}`,
    color: tokens.colorBrandForeground2,
  },
  handle: {
    width: "5px",
    height: "5px",
    backgroundColor: tokens.colorNeutralStroke2,
    border: "none",
  },
  legend: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
});

function variantClass(
  styles: ReturnType<typeof useStyles>,
  kind: FlowNodeKind,
  highlight: "current" | "next" | null,
): string {
  if (highlight === "current") {
    return styles.nodeCurrent;
  }
  if (highlight === "next") {
    return styles.nodeNext;
  }
  if (kind === "terminal") {
    return styles.nodeTerminal;
  }
  if (kind === "conditional") {
    return styles.nodeConditional;
  }
  return "";
}

function TopologyFlowNode({ data }: NodeProps) {
  const styles = useStyles();
  const nodeData = data as unknown as FlowNodeData;
  const className = `${styles.node} ${variantClass(styles, nodeData.kind, nodeData.highlight ?? null)}`;

  return (
    <div className={className}>
      <Handle
        type="target"
        position={Position.Left}
        className={styles.handle}
        isConnectable={false}
      />
      {nodeData.label}
      <Handle
        type="source"
        position={Position.Right}
        className={styles.handle}
        isConnectable={false}
      />
    </div>
  );
}

const NODE_TYPES = { voluta: TopologyFlowNode };

export interface VolutaFlowProps {
  topology: TopologyDescription;
  /** Checkpoint overlay: current node (pulse) + candidate next nodes (dashed). */
  highlight?: FlowHighlight;
  /** When set, nodes become selectable and clicks are reported. */
  onNodeClick?: (nodeId: string) => void;
  /** Canvas height in px (default 560). */
  height?: number;
}

export function VolutaFlow({
  topology,
  highlight,
  onNodeClick,
  height,
}: VolutaFlowProps) {
  const styles = useStyles();

  const conditional = useMemo(
    () => new Set(topology.conditionalSources ?? []),
    [topology.conditionalSources],
  );

  const { nodes, edges } = useMemo(
    () => layoutGraph(topology, conditional, highlight),
    [topology, conditional, highlight],
  );

  const interactive = Boolean(onNodeClick);

  const selectableNodes = useMemo(
    () =>
      interactive
        ? nodes.map((node) => ({ ...node, selectable: true }))
        : nodes,
    [nodes, interactive],
  );

  const edgeStyle = useMemo(
    () => ({
      stroke: tokens.colorNeutralStroke2,
      strokeWidth: 1.4,
    }),
    [],
  );

  const handleNodeClick = useMemo(
    () =>
      onNodeClick
        ? (_event: React.MouseEvent, node: Node) => onNodeClick(node.id)
        : undefined,
    [onNodeClick],
  );

  return (
    <div
      className={styles.canvas}
      style={height !== undefined ? { height: `${height}px` } : undefined}
    >
      <ReactFlow
        nodes={selectableNodes}
        edges={edges as Edge[]}
        nodeTypes={NODE_TYPES}
        defaultEdgeOptions={{ style: edgeStyle }}
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable={interactive}
        panOnScroll
        zoomOnScroll={false}
        fitView
        fitViewOptions={{ padding: 0.15, maxZoom: 1.2 }}
        proOptions={{ hideAttribution: true }}
        onNodeClick={handleNodeClick}
      >
        <Background
          gap={22}
          size={1}
          color="var(--voluta-grid, rgba(0,0,0,0.06))"
        />
        <Controls
          showInteractive={false}
          position="bottom-right"
        />
      </ReactFlow>
    </div>
  );
}
