/**
 * .regentrc.ts — voluta
 * Global C# house rules load from ~/.agents/rules/csharp/regent-rules/
 * via regent's globalRulesPath fallback.
 */
import { defineConfig } from '@dot-stbl/regent';

export default defineConfig({
  rules: {
    extends: [],
  },
  excludePaths: [
    '**/obj/**',
    '**/bin/**',
    '**/.agents/worktree/**',
    '**/node_modules/**',
  ],
});
