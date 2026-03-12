FROM node:22-bookworm-slim AS build
WORKDIR /workspace

COPY package.json package-lock.json tsconfig.base.json ./
COPY packages/ui/package.json packages/ui/
COPY packages/shell/package.json packages/shell/
COPY packages/activities/package.json packages/activities/
COPY packages/web/package.json packages/web/

RUN npm ci
RUN node -e "const {platform, arch} = process; let suffix=''; if (platform === 'linux') { suffix = arch === 'arm' ? 'gnueabihf' : 'gnu'; } else if (platform === 'win32') { suffix = 'msvc'; } const variants = [ ['lightningcss', platform, arch, suffix].filter(Boolean).join('-'), ['@tailwindcss/oxide', platform, arch, suffix].filter(Boolean).join('-') ]; require('child_process').execSync('npm install --no-save ' + variants.join(' '), {stdio: 'inherit'});"

COPY packages ./packages
RUN npm run build

FROM nginx:alpine AS runtime

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

ENV API_ORIGIN="http://localhost:5001"
ENV WS_ORIGIN="ws://localhost:5001"

CMD ["nginx", "-g", "daemon off;"]
