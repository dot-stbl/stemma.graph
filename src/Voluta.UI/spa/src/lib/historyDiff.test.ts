import { describe, expect, it } from "vitest";
import { diffChannelValues, isDiffEmpty, truncateValue } from "./historyDiff";

describe("diffChannelValues", () => {
  it("classifies added, changed and removed keys", () => {
    const diff = diffChannelValues(
      { goal: "same", plan: "old-plan", stale: "gone" },
      { goal: "same", plan: "new-plan", fresh: "added" },
    );

    expect(diff.added).toEqual([{ key: "fresh", value: "added" }]);
    expect(diff.changed).toEqual([
      { key: "plan", before: "old-plan", after: "new-plan" },
    ]);
    expect(diff.removed).toEqual([{ key: "stale", value: "gone" }]);
  });

  it("handles null maps", () => {
    const fromNull = diffChannelValues(null, { a: "1" });
    expect(fromNull.added).toEqual([{ key: "a", value: "1" }]);

    const toNull = diffChannelValues({ a: "1" }, null);
    expect(toNull.removed).toEqual([{ key: "a", value: "1" }]);
  });

  it("returns empty sections for identical maps", () => {
    const diff = diffChannelValues({ a: "1" }, { a: "1" });

    expect(isDiffEmpty(diff)).toBe(true);
  });

  it("sorts keys deterministically", () => {
    const diff = diffChannelValues({}, { c: "3", a: "1", b: "2" });

    expect(diff.added.map((entry) => entry.key)).toEqual(["a", "b", "c"]);
  });
});

describe("truncateValue", () => {
  it("keeps short values as-is", () => {
    expect(truncateValue("short")).toBe("short");
  });

  it("truncates long values with an ellipsis", () => {
    const long = "x".repeat(100);
    const truncated = truncateValue(long);

    expect(truncated.length).toBe(64);
    expect(truncated.endsWith("…")).toBe(true);
  });
});
