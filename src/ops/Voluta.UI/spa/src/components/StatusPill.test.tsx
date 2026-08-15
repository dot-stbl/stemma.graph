import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusPill } from "./StatusPill";

describe("StatusPill", () => {
  it("renders the status label", () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <StatusPill status="Running" />
      </FluentProvider>,
    );

    expect(screen.getByText("Running")).toBeTruthy();
  });

  it("renders a dash for missing status", () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <StatusPill status={null} />
      </FluentProvider>,
    );

    expect(screen.getByText("—")).toBeTruthy();
  });
});
