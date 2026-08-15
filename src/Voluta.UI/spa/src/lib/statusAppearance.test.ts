import { describe, expect, it } from "vitest";
import { appearanceFor } from "@/lib/statusAppearance";

describe("appearanceFor", () => {
  it("maps terminal and running statuses", () => {
    expect(appearanceFor("Done")).toBe("success");
    expect(appearanceFor("Interrupted")).toBe("warning");
    expect(appearanceFor("Failed")).toBe("danger");
    expect(appearanceFor("Error")).toBe("danger");
    expect(appearanceFor("Running")).toBe("informative");
    expect(appearanceFor("Cancelled")).toBe("subtle");
  });

  it("is case-insensitive", () => {
    expect(appearanceFor("done")).toBe("success");
    expect(appearanceFor("INTERRUPTED")).toBe("warning");
    expect(appearanceFor("running")).toBe("informative");
  });

  it("falls back to subtle for unknown statuses", () => {
    expect(appearanceFor("SomethingElse")).toBe("subtle");
  });
});
