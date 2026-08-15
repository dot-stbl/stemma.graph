import { Button, makeStyles, Subtitle1, Text, tokens } from "@fluentui/react-components";
import { Component, type ErrorInfo, type ReactNode } from "react";

const useStyles = makeStyles({
  root: {
    height: "100vh",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    gap: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    color: tokens.colorNeutralForeground1,
    padding: tokens.spacingVerticalL,
    textAlign: "center",
  },
  details: {
    maxWidth: "640px",
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    textAlign: "left",
    whiteSpace: "pre-wrap",
    overflow: "auto",
    maxHeight: "240px",
  },
});

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("Studio crashed:", error, info.componentStack);
  }

  private handleReset = () => {
    this.setState({ error: null });
  };

  render() {
    if (this.state.error) {
      return <ErrorBoundaryFallback error={this.state.error} onReset={this.handleReset} />;
    }
    return this.props.children;
  }
}

function ErrorBoundaryFallback({ error, onReset }: { error: Error; onReset: () => void }) {
  const styles = useStyles();
  return (
    <div className={styles.root} role="alert">
      <Subtitle1 as="h1">Something broke</Subtitle1>
      <Text>{error.message || "Unexpected client error"}</Text>
      <pre className={styles.details}>{error.stack ?? error.message}</pre>
      <Button
        appearance="primary"
        onClick={() => {
          onReset();
          window.location.assign("/voluta/studio/");
        }}
      >
        Reload Studio
      </Button>
    </div>
  );
}
