# @sddp/web

`@sddp/web` is the web application assembly package for SDDP. It wires together the `ui`, `shell`, and `activities` packages into the browser-facing app.

## Development

```bash
cd src/frontend
npm install
npm run dev
```

The default local development URL is `http://localhost:3500`.

## Commands

```bash
cd src/frontend
npm run build
npm run test
npm run test:e2e -w @sddp/web
```

Run the production build from `src/frontend`; it builds the library packages first and verifies
that the web app consumes their `dist/` exports. A workspace-local `packages/web` build is only a
source-wiring diagnostic and is not the release contract.

## Related Files

- [../../../../docs/public-guide.md](../../../../docs/public-guide.md)
- [../../../../README.md](../../../../README.md)
