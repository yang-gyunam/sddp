<script lang="ts">
  import Icon from './Icon.svelte';
  import type { IconSize } from '../types';

  interface Props {
    path: string;
    kind?: 'file' | 'directory' | 'symlink' | 'submodule';
    size?: IconSize;
  }

  let { path, kind = 'file', size = 'sm' }: Props = $props();
  type Appearance = { name: string; title: string; color?: string };
  const icon = $derived.by((): Appearance => {
    if (kind === 'directory') return { name: 'folder', title: 'Folder' };
    if (kind === 'symlink') return { name: 'link', title: 'Symbolic link' };
    if (kind === 'submodule') return { name: 'package', title: 'Git submodule' };

    const filename = path.split('/').at(-1)?.toLowerCase() ?? '';
    const extension = filename.includes('.') ? filename.split('.').at(-1)! : '';
    const colored = (name: string, title: string, token: string): Appearance => ({
      name,
      title,
      color: `var(--color-${token})`,
    });

    if (['package.json', 'package-lock.json', 'yarn.lock', 'pnpm-lock.yaml'].includes(filename))
      return colored('package', 'Package manifest', 'success-500');
    if (/^(dockerfile|containerfile)(\.|$)/.test(filename))
      return colored('box', 'Container build', 'info-500');
    if (filename === 'makefile') return { name: 'terminal', title: 'Build script' };
    if (/^\.(env|git|docker|editorconfig|npm|prettier|eslint)/.test(filename))
      return { name: 'settings', title: 'Configuration' };

    switch (extension) {
      case 'cs':
        return colored('hash', 'C#', 'primary-400');
      case 'ts':
      case 'tsx':
      case 'mts':
      case 'cts':
        return colored('file-code', 'TypeScript', 'info-500');
      case 'js':
      case 'jsx':
      case 'mjs':
      case 'cjs':
        return colored('file-code', 'JavaScript', 'warning-500');
      case 'svelte':
        return colored('code', 'Svelte', 'warning-600');
      case 'json':
      case 'jsonc':
        return colored('braces', 'JSON', 'warning-500');
      case 'md':
      case 'mdx':
        return colored('book-open', 'Markdown', 'info-500');
      case 'css':
      case 'scss':
      case 'sass':
      case 'less':
        return colored('palette', 'Stylesheet', 'primary-400');
      case 'html':
      case 'htm':
      case 'xml':
      case 'svg':
        return colored('code', 'Markup', 'warning-600');
      case 'yml':
      case 'yaml':
      case 'toml':
      case 'ini':
      case 'conf':
      case 'csproj':
      case 'sln':
      case 'slnx':
      case 'props':
      case 'targets':
        return { name: 'settings', title: 'Configuration' };
      case 'sql':
      case 'db':
      case 'sqlite':
        return colored('database', 'Database', 'info-500');
      case 'sh':
      case 'bash':
      case 'zsh':
      case 'ps1':
      case 'bat':
      case 'cmd':
        return { name: 'terminal', title: 'Script' };
      case 'png':
      case 'jpg':
      case 'jpeg':
      case 'gif':
      case 'webp':
      case 'ico':
      case 'avif':
        return colored('image', 'Image', 'success-500');
      case 'zip':
      case 'gz':
      case 'tar':
      case '7z':
      case 'rar':
        return { name: 'archive', title: 'Archive' };
      case 'txt':
      case 'pdf':
      case 'rst':
      case 'log':
        return { name: 'file-text', title: 'Document' };
      case 'py':
      case 'go':
      case 'rs':
      case 'java':
      case 'c':
      case 'cpp':
      case 'h':
      case 'rb':
        return { name: 'file-code', title: 'Source code' };
      default:
        return { name: 'file', title: 'File' };
    }
  });
</script>

<Icon {...icon} {size} />
