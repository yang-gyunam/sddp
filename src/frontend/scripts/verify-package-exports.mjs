import { access, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const frontendRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const packageNames = ['ui', 'shell', 'activities'];
const errors = [];

function collectTargets(value, keyPath, targets) {
  if (typeof value === 'string') {
    targets.push({ keyPath, value });
    return;
  }

  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    errors.push(`${keyPath} must resolve to a string or condition object`);
    return;
  }

  for (const [key, child] of Object.entries(value)) {
    collectTargets(child, `${keyPath}.${key}`, targets);
  }
}

for (const packageName of packageNames) {
  const packageRoot = path.join(frontendRoot, 'packages', packageName);
  const manifestPath = path.join(packageRoot, 'package.json');
  const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
  const targets = [];

  collectTargets(manifest.exports, `${manifest.name}.exports`, targets);

  for (const field of ['svelte', 'main', 'types']) {
    if (typeof manifest[field] === 'string') {
      targets.push({ keyPath: `${manifest.name}.${field}`, value: manifest[field] });
    }
  }

  if (!Array.isArray(manifest.files) || manifest.files.includes('src') || !manifest.files.includes('dist')) {
    errors.push(`${manifest.name}.files must expose dist and must not expose src`);
  }

  for (const target of targets) {
    if (!target.value.startsWith('./dist/')) {
      errors.push(`${target.keyPath} must target ./dist/: ${target.value}`);
      continue;
    }

    try {
      await access(path.resolve(packageRoot, target.value));
    } catch {
      errors.push(`${target.keyPath} target does not exist: ${target.value}`);
    }
  }
}

if (errors.length > 0) {
  console.error('Frontend package export contract failed:');
  for (const error of errors) {
    console.error(`- ${error}`);
  }
  process.exitCode = 1;
} else {
  console.log('Frontend package export contract: OK (dist-only targets exist)');
}
