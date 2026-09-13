FROM node:22.23.2-bookworm-slim AS build
WORKDIR /workspace

COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/ui/package.json packages/ui/
COPY packages/shell/package.json packages/shell/
COPY packages/activities/package.json packages/activities/
COPY packages/web/package.json packages/web/

RUN npm ci
RUN node -e "const {platform, arch} = process; let suffix=''; if (platform === 'linux') { suffix = arch === 'arm' ? 'gnueabihf' : 'gnu'; } else if (platform === 'win32') { suffix = 'msvc'; } const lock = require(process.cwd() + '/package-lock.json'); const pin = (pkg) => { const e = lock.packages['node_modules/' + pkg]; if (!e || !e.version) { throw new Error('no package-lock entry for ' + pkg); } return e.version; }; const variants = [ ['lightningcss', platform, arch, suffix].filter(Boolean).join('-') + '@' + pin('lightningcss'), ['@tailwindcss/oxide', platform, arch, suffix].filter(Boolean).join('-') + '@' + pin('@tailwindcss/oxide') ]; console.log('installing', variants.join(' ')); require('child_process').execSync('npm install --no-save ' + variants.join(' '), {stdio: 'inherit'});"

COPY packages ./packages
COPY scripts ./scripts

ARG VITE_API_URL=""
ARG VITE_API_VERSION="v1"
ARG VITE_ENABLE_DEBUG="false"
ARG VITE_ENABLE_ANALYTICS="false"
ARG VITE_ENABLE_AI_FEATURES="false"
ARG VITE_ENABLE_SPEECH_TO_TEXT="true"
ARG VITE_ENABLE_ARTIFACT_MAINTENANCE="false"
ARG VITE_ENABLE_PROJECT_INTEGRATIONS="false"
ARG VITE_ENABLE_SPEC_SUPPLEMENTARY_TABS="false"
ARG VITE_ENABLE_SYSTEM_CONFIG_AUTHENTICATION="false"
ARG VITE_ENABLE_SYSTEM_CONFIG_PERFORMANCE="false"
ARG VITE_ENABLE_RUNES_TOAST_STORE="false"
ARG VITE_ENABLE_RUNES_PANEL_STORE="false"
ARG VITE_ENABLE_RUNES_TAB_STATE_STORE="false"
ARG VITE_ENABLE_RUNES_COMMAND_STORE="false"
ARG VITE_ENABLE_RUNES_MENU_STORE="false"
ARG VITE_ENABLE_RUNES_SIDE_PANEL_STORE="false"
ARG VITE_ENABLE_RUNES_PANEL_CONTENT_STORE="false"
ARG VITE_ENABLE_RUNES_TABS_STORE="false"
ARG VITE_ENABLE_RUNES_STORE_SHADOW_PARITY="true"
ARG VITE_AUTH_TOKEN_EXPIRY="900"

RUN npm run build

FROM nginx:1.31.4-alpine@sha256:db35bfc6b2951e7f8a72db5db120288c127ffaeeb4a6d4b95a26fead017d5913 AS runtime

COPY --from=build /workspace/packages/web/dist /usr/share/nginx/html
COPY packages/web/nginx.conf /etc/nginx/templates/default.conf.template

RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    touch /var/run/nginx.pid && chown nginx:nginx /var/run/nginx.pid && \
    rm -f /etc/nginx/conf.d/default.conf && \
    chown -R nginx:nginx /etc/nginx/conf.d /etc/nginx/templates

USER nginx

EXPOSE 8080

ENV API_ORIGIN=""
ENV WS_ORIGIN=""

CMD ["nginx", "-g", "daemon off;"]
