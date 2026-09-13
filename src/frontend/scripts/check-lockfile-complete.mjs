#!/usr/bin/env node
/**
 * Fails when the lockfile omits a dependency some installed package declares.
 *
 * npm 11.7 drops entries when it rebuilds package-lock.json from scratch: a full
 * `rm -rf node_modules package-lock.json && npm install` produced a tree missing
 * wsl-utils, @webcontainer/env and a dozen more, all of them plain (non-optional)
 * dependencies. `npm install` and `npm ci` both stayed silent; the first thing to notice
 * was `storybook build` dying on ERR_MODULE_NOT_FOUND in CI.
 *
 * Optional dependencies are excluded — the per-platform esbuild and rollup binaries are
 * meant to be absent.
 */
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

const UNMET = /UNMET DEPENDENCY (\S+)/g;

/**
 * `npm ls` prints a tree, so a real run always contains branch characters or the deduped
 * marker. Without this check an npm that failed for an unrelated reason — wrong directory,
 * missing binary, a flag removed in a future major — yields output the regex finds nothing
 * in, and the gate reports success while having examined nothing. That is the same failure
 * the eslint 10 upgrade produced when a removed CLI flag left it linting zero files.
 */
function looksLikeADependencyTree(output) {
  return /[├└]─|\bdeduped\b|@\d+\.\d+\.\d+/.test(output);
}

/**
 * @param {string} output raw `npm ls --all` output (stdout and stderr combined)
 * @returns {{ ok: boolean, missing: string[], reason?: string }}
 */
export function findMissingDependencies(output) {
  if (!looksLikeADependencyTree(output)) {
    return {
      ok: false,
      missing: [],
      reason:
        'npm ls produced no dependency tree, so nothing was checked. Run it by hand in ' +
        'src/frontend to see why before trusting this gate.',
    };
  }

  const missing = [...new Set([...output.matchAll(UNMET)].map((match) => match[1]))].sort();
  return { ok: missing.length === 0, missing };
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : '';
if (invokedPath === fileURLToPath(import.meta.url)) {
  let output;
  try {
    output = execFileSync('npm', ['ls', '--all'], {
      encoding: 'utf-8',
      stdio: ['ignore', 'pipe', 'pipe'],
      maxBuffer: 64 * 1024 * 1024,
    });
  } catch (error) {
    // npm ls exits non-zero whenever the tree has any problem, including the optional
    // dependencies we tolerate, so the output matters rather than the status.
    output = `${error.stdout ?? ''}${error.stderr ?? ''}`;
  }

  const result = findMissingDependencies(output);

  if (!result.ok && result.reason) {
    process.stderr.write(`lockfile-complete: ${result.reason}\n`);
    process.exit(1);
  }

  if (!result.ok) {
    const { missing } = result;
    process.stderr.write(
      `lockfile-complete: ${missing.length} declared ${
        missing.length === 1 ? 'dependency is' : 'dependencies are'
      } missing from the lockfile:\n`
    );
    for (const name of missing) process.stderr.write(`  - ${name}\n`);
    process.stderr.write(
      '\nDo not fix this by regenerating package-lock.json from scratch — that is what\n' +
        'causes it. Restore the lockfile from git and let `npm install` add only what the\n' +
        'package.json change needs.\n'
    );
    process.exit(1);
  }

  process.stdout.write('lockfile-complete: every declared dependency is present.\n');
}
