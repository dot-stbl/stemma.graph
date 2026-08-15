export interface ChangedValue {
  key: string;
  before: string;
  after: string;
}

export interface ValuesDiff {
  added: Array<{ key: string; value: string }>;
  changed: ChangedValue[];
  removed: Array<{ key: string; value: string }>;
}

export const DIFF_VALUE_MAX_LENGTH = 64;

export function truncateValue(value: string, maxLength = DIFF_VALUE_MAX_LENGTH): string {
  if (value.length <= maxLength) {
    return value;
  }
  return `${value.slice(0, Math.max(0, maxLength - 1))}…`;
}

/** Diff two channelValues maps by key: added / changed / removed. */
export function diffChannelValues(
  before: Record<string, string> | null | undefined,
  after: Record<string, string> | null | undefined,
): ValuesDiff {
  const oldValues = before ?? {};
  const newValues = after ?? {};

  const added: Array<{ key: string; value: string }> = [];
  const changed: ChangedValue[] = [];
  const removed: Array<{ key: string; value: string }> = [];

  for (const [key, value] of Object.entries(newValues)) {
    if (!(key in oldValues)) {
      added.push({ key, value });
    } else if (oldValues[key] !== value) {
      changed.push({ key, before: oldValues[key], after: value });
    }
  }
  for (const [key, value] of Object.entries(oldValues)) {
    if (!(key in newValues)) {
      removed.push({ key, value });
    }
  }

  added.sort((a, b) => a.key.localeCompare(b.key));
  changed.sort((a, b) => a.key.localeCompare(b.key));
  removed.sort((a, b) => a.key.localeCompare(b.key));

  return { added, changed, removed };
}

export function isDiffEmpty(diff: ValuesDiff): boolean {
  return diff.added.length === 0 && diff.changed.length === 0 && diff.removed.length === 0;
}
