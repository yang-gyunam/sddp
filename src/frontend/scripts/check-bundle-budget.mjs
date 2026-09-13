#!/usr/bin/env node

import { existsSync, readFileSync, statSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const bundleBudget = {
  maxInitialJsBytes: 550 * 1024,
  minAsyncChunks: 8,
};

function collectStaticGraph(manifest, roots) {
  const visited = new Set();

  function visit(key) {
    if (visited.has(key)) return;
    const chunk = manifest[key];
    if (!chunk) throw new Error(`manifest references missing chunk: ${key}`);

    visited.add(key);
    for (const importedKey of chunk.imports ?? []) visit(importedKey);
  }

  for (const root of roots) visit(root);
  return visited;
}

function collectAsyncRoots(manifest, initialGraph) {
  const asyncRoots = new Set();
  for (const key of initialGraph) {
    for (const dynamicKey of manifest[key].dynamicImports ?? []) {
      if (!manifest[dynamicKey]) {
        throw new Error(`manifest references missing dynamic chunk: ${dynamicKey}`);
      }
      if (!initialGraph.has(dynamicKey)) asyncRoots.add(dynamicKey);
    }
  }
  return asyncRoots;
}

function formatKiB(bytes) {
  return `${(bytes / 1024).toFixed(2)} KiB`;
}

export function checkBundleBudget(manifestPath) {
  if (!existsSync(manifestPath)) {
    throw new Error(`Vite manifest not found: ${manifestPath}`);
  }

  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
  const entryKeys = Object.entries(manifest)
    .filter(([, chunk]) => chunk.isEntry)
    .map(([key]) => key);
  if (entryKeys.length === 0) throw new Error('Vite manifest has no entry chunk');

  const initialGraph = collectStaticGraph(manifest, entryKeys);
  const outputRoot = resolve(dirname(manifestPath), '..');
  let initialJsBytes = 0;
  for (const key of initialGraph) {
    const file = manifest[key].file;
    if (!file?.endsWith('.js')) continue;
    const outputPath = resolve(outputRoot, file);
    if (!existsSync(outputPath)) throw new Error(`manifest output is missing: ${file}`);
    initialJsBytes += statSync(outputPath).size;
  }

  const asyncRoots = collectAsyncRoots(manifest, initialGraph);
  const errors = [];
  if (initialJsBytes > bundleBudget.maxInitialJsBytes) {
    errors.push(
      `initial JS ${formatKiB(initialJsBytes)} exceeds ${formatKiB(bundleBudget.maxInitialJsBytes)}`
    );
  }
  if (asyncRoots.size < bundleBudget.minAsyncChunks) {
    errors.push(`async root count ${asyncRoots.size} is below ${bundleBudget.minAsyncChunks}`);
  }

  return {
    entryCount: entryKeys.length,
    initialJsBytes,
    asyncRootCount: asyncRoots.size,
    errors,
  };
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : '';
if (invokedPath === fileURLToPath(import.meta.url)) {
  const manifestPath = resolve(
    process.argv[2] ??
      fileURLToPath(new URL('../packages/web/dist/.vite/manifest.json', import.meta.url))
  );

  try {
    const result = checkBundleBudget(manifestPath);
    const summary =
      `bundle-budget: ${result.entryCount} entry, ` +
      `${formatKiB(result.initialJsBytes)} initial JS, ` +
      `${result.asyncRootCount} async roots`;

    if (result.errors.length > 0) {
      process.stderr.write(`${summary}\n`);
      for (const error of result.errors) process.stderr.write(`- ${error}\n`);
      process.exit(1);
    }
    process.stdout.write(`${summary} passed.\n`);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`bundle-budget: ${message}\n`);
    process.exit(1);
  }
}
