#!/usr/bin/env node

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const sharedToolPackages = ['@sveltejs/vite-plugin-svelte', 'vite', 'vitest'];
const storybookPackages = [
  '@storybook/addon-a11y',
  '@storybook/svelte',
  '@storybook/svelte-vite',
  'storybook',
];
const removedStorybookPackages = ['@storybook/addon-essentials', '@storybook/addon-interactions'];

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'));
}

function dependencySpec(manifest, packageName) {
  return (
    manifest.dependencies?.[packageName] ??
    manifest.devDependencies?.[packageName] ??
    manifest.peerDependencies?.[packageName]
  );
}

function collectManifests(workspaceRoot) {
  const manifests = [
    {
      path: 'package.json',
      manifest: readJson(join(workspaceRoot, 'package.json')),
    },
  ];
  const packagesRoot = join(workspaceRoot, 'packages');
  for (const entry of readdirSync(packagesRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const relativePath = `packages/${entry.name}/package.json`;
    const manifestPath = join(workspaceRoot, relativePath);
    if (existsSync(manifestPath))
      manifests.push({ path: relativePath, manifest: readJson(manifestPath) });
  }
  return manifests;
}

function checkAlignedSpecs(manifests, packageNames, errors) {
  for (const packageName of packageNames) {
    const declarations = manifests
      .map(({ path, manifest }) => ({ path, spec: dependencySpec(manifest, packageName) }))
      .filter(({ spec }) => spec !== undefined);
    const specs = new Set(declarations.map(({ spec }) => spec));
    if (specs.size <= 1) continue;

    const detail = declarations.map(({ path, spec }) => `${path}=${spec}`).join(', ');
    errors.push(`${packageName} version spec drift: ${detail}`);
  }
}

export function checkWorkspaceDependencies(workspaceRoot) {
  const errors = [];
  const rootLockfile = join(workspaceRoot, 'package-lock.json');
  if (!existsSync(rootLockfile)) errors.push('root package-lock.json is missing');

  const packagesRoot = join(workspaceRoot, 'packages');
  for (const entry of readdirSync(packagesRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const lockfilePath = join(packagesRoot, entry.name, 'package-lock.json');
    if (existsSync(lockfilePath)) {
      errors.push(`workspace lockfile is forbidden: packages/${entry.name}/package-lock.json`);
    }
  }

  const manifests = collectManifests(workspaceRoot);
  checkAlignedSpecs(manifests, sharedToolPackages, errors);
  checkAlignedSpecs(manifests, storybookPackages, errors);

  const storybookDeclarations = storybookPackages.flatMap((packageName) =>
    manifests
      .map(({ path, manifest }) => ({
        packageName,
        path,
        spec: dependencySpec(manifest, packageName),
      }))
      .filter(({ spec }) => spec !== undefined)
  );
  if (new Set(storybookDeclarations.map(({ spec }) => spec)).size > 1) {
    const detail = storybookDeclarations
      .map(({ packageName, path, spec }) => `${path}:${packageName}=${spec}`)
      .join(', ');
    errors.push(`Storybook version spec drift: ${detail}`);
  }

  for (const packageName of storybookPackages) {
    if (!manifests.some(({ manifest }) => dependencySpec(manifest, packageName))) {
      errors.push(`required Storybook package is missing: ${packageName}`);
    }
  }
  for (const packageName of removedStorybookPackages) {
    const declaration = manifests.find(({ manifest }) => dependencySpec(manifest, packageName));
    if (declaration) errors.push(`removed Storybook package is forbidden: ${packageName}`);
  }

  return { manifestCount: manifests.length, errors };
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : '';
if (invokedPath === fileURLToPath(import.meta.url)) {
  const workspaceRoot = resolve(
    process.argv[2] ?? dirname(fileURLToPath(new URL('../package.json', import.meta.url)))
  );
  try {
    const result = checkWorkspaceDependencies(workspaceRoot);
    if (result.errors.length > 0) {
      process.stderr.write('workspace-deps: contract failed\n');
      for (const error of result.errors) process.stderr.write(`- ${error}\n`);
      process.exit(1);
    }
    process.stdout.write(
      `workspace-deps: root lockfile and ${result.manifestCount} manifests passed.\n`
    );
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`workspace-deps: ${message}\n`);
    process.exit(1);
  }
}
