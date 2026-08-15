import {
  Badge,
  Button,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Subtitle1,
  Subtitle2,
  Text,
  tokens,
} from "@fluentui/react-components";
import { Dismiss20Regular } from "@fluentui/react-icons";
import { useMemo, useState } from "react";
import { VolutaFlow } from "@/components/VolutaFlow";
import { LoadingSkeleton } from "@/components/LoadingSkeleton";
import { useTopology } from "@/hooks/useTopology";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  panel: {
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  headerRow: {
    display: "flex",
    alignItems: "baseline",
    gap: tokens.spacingHorizontalS,
    flexWrap: "wrap",
  },
  muted: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  grid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: tokens.spacingHorizontalL,
    alignItems: "start",
    "@media (max-width: 900px)": {
      gridTemplateColumns: "1fr",
    },
  },
  graph: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    maxHeight: "420px",
    overflow: "auto",
  },
  edgeRow: {
    display: "flex",
    alignItems: "baseline",
    flexWrap: "wrap",
    columnGap: tokens.spacingHorizontalXS,
    rowGap: tokens.spacingVerticalXXS,
    paddingBlock: tokens.spacingVerticalXXS,
  },
  edgeSource: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  edgeSourceCond: {
    color: tokens.colorPaletteMarigoldForeground2,
  },
  arrow: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  targetChip: {
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorNeutralForeground2,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
  channelRow: {
    display: "grid",
    gridTemplateColumns: "180px 1fr",
    gap: tokens.spacingHorizontalS,
    paddingBlock: tokens.spacingVerticalXXS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  channelName: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  channelKind: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  channelList: {
    maxHeight: "420px",
    overflow: "auto",
  },
  graphRow: {
    display: "grid",
    gridTemplateColumns: "1fr",
    gap: tokens.spacingHorizontalL,
    alignItems: "start",
  },
  graphRowWithInspector: {
    gridTemplateColumns: "1fr 320px",
    "@media (max-width: 1200px)": {
      gridTemplateColumns: "1fr",
    },
  },
  inspector: {
    position: "sticky",
    top: 0,
  },
  inspectorHeader: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    gap: tokens.spacingHorizontalS,
  },
  inspectorTitle: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    minWidth: 0,
  },
  inspectorName: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground1,
    fontWeight: tokens.fontWeightSemibold,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  inspectorSection: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
  },
  inspectorList: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXXS,
  },
  inspectorItem: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
});

export function TopologyPage() {
  const styles = useStyles();
  const { data, isLoading, isError, error } = useTopology();
  const [selectedNode, setSelectedNode] = useState<string | null>(null);

  const conditional = useMemo(
    () => new Set(data?.conditionalSources ?? []),
    [data],
  );

  const edgeGroups = useMemo(() => {
    const map = new Map<string, string[]>();
    for (const edge of data?.staticEdges ?? []) {
      const existing = map.get(edge.source);
      if (existing) {
        existing.push(edge.target);
      } else {
        map.set(edge.source, [edge.target]);
      }
    }
    return Array.from(map.entries());
  }, [data]);

  const nodeDetails = useMemo(() => {
    if (!selectedNode || !data) {
      return null;
    }
    const incoming = data.staticEdges
      .filter((edge) => edge.target === selectedNode)
      .map((edge) => edge.source);
    const outgoing = data.staticEdges
      .filter((edge) => edge.source === selectedNode)
      .map((edge) => edge.target);
    return {
      name: selectedNode,
      incoming: Array.from(new Set(incoming)).sort(),
      outgoing: Array.from(new Set(outgoing)).sort(),
      isConditional: conditional.has(selectedNode),
    };
  }, [selectedNode, data, conditional]);

  return (
    <div className={styles.root}>
      <div className={styles.headerRow}>
        <Subtitle1 as="h1">Topology</Subtitle1>
        {data && (
          <Text className={styles.muted}>
            {data.nodes.length} nodes · {data.staticEdges.length} edges
            {data.recursionLimit != null
              ? ` · recursionLimit ${data.recursionLimit}`
              : ""}
            {conditional.size > 0 ? ` · ${conditional.size} conditional` : ""}
          </Text>
        )}
      </div>

      {isLoading && (
        <LoadingSkeleton variant="panel" rows={5} aria-label="Loading topology" />
      )}
      {isError && (
        <MessageBar intent="error">
          <MessageBarBody>
            {(error as Error)?.message ?? "Failed to load topology"}
          </MessageBarBody>
        </MessageBar>
      )}

      {data && (
        <>
          <div
            className={
              nodeDetails
                ? `${styles.graphRow} ${styles.graphRowWithInspector}`
                : styles.graphRow
            }
          >
            <VolutaFlow topology={data} onNodeClick={setSelectedNode} />

            {nodeDetails && (
              <div className={`${styles.panel} ${styles.inspector}`}>
                <div className={styles.inspectorHeader}>
                  <div className={styles.inspectorTitle}>
                    <span className={styles.inspectorName} title={nodeDetails.name}>
                      {nodeDetails.name}
                    </span>
                    {nodeDetails.isConditional && (
                      <Badge appearance="tint" color="warning" size="small">
                        conditional
                      </Badge>
                    )}
                  </div>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<Dismiss20Regular />}
                    aria-label="Close node details"
                    onClick={() => setSelectedNode(null)}
                  />
                </div>

                <div className={styles.inspectorSection}>
                  <Subtitle2 as="h3">
                    In edges ({nodeDetails.incoming.length})
                  </Subtitle2>
                  <div className={styles.inspectorList}>
                    {nodeDetails.incoming.length === 0 ? (
                      <Text className={styles.muted}>No incoming edges.</Text>
                    ) : (
                      nodeDetails.incoming.map((source) => (
                        <span
                          key={`in-${source}`}
                          className={styles.inspectorItem}
                          title={source}
                        >
                          {source}
                        </span>
                      ))
                    )}
                  </div>
                </div>

                <div className={styles.inspectorSection}>
                  <Subtitle2 as="h3">
                    Out edges ({nodeDetails.outgoing.length})
                  </Subtitle2>
                  <div className={styles.inspectorList}>
                    {nodeDetails.outgoing.length === 0 ? (
                      <Text className={styles.muted}>No outgoing edges.</Text>
                    ) : (
                      nodeDetails.outgoing.map((target) => (
                        <span
                          key={`out-${target}`}
                          className={styles.inspectorItem}
                          title={target}
                        >
                          {target}
                        </span>
                      ))
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
          <Text className={styles.muted}>
            {`click a node for details${
              conditional.size > 0
                ? " · amber border = conditional source (routing depends on state)"
                : ""
            }`}
          </Text>

          <div className={styles.grid}>
            <div className={styles.panel}>
              <Subtitle2 as="h2">Edges</Subtitle2>
              <div className={styles.graph}>
                {edgeGroups.length === 0 && (
                  <Text className={styles.muted}>No static edges.</Text>
                )}
                {edgeGroups.map(([source, targets]) => (
                  <div key={source} className={styles.edgeRow}>
                    <span
                      className={
                        conditional.has(source)
                          ? `${styles.edgeSource} ${styles.edgeSourceCond}`
                          : styles.edgeSource
                      }
                    >
                      {source}
                    </span>
                    <span className={styles.arrow} aria-hidden="true">
                      →
                    </span>
                    {targets.map((target, index) => (
                      <span
                        key={`${source}-${target}-${index}`}
                        className={styles.targetChip}
                      >
                        {target}
                      </span>
                    ))}
                  </div>
                ))}
              </div>
            </div>

            <div className={styles.panel}>
              <Subtitle2 as="h2">
                Channels ({Object.keys(data.channels).length})
              </Subtitle2>
              <div className={styles.channelList}>
                {Object.entries(data.channels).map(([name, kind]) => (
                  <div key={name} className={styles.channelRow}>
                    <span className={styles.channelName} title={name}>
                      {name}
                    </span>
                    <span className={styles.channelKind}>{kind}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
