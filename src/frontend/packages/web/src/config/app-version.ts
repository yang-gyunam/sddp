import packageMetadata from '../../package.json';

export const appVersion = packageMetadata.version;
export const appVersionLabel = `v${appVersion}`;
export const appVersionTooltip = `SDDP ${appVersionLabel}`;
