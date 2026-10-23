# @sddp/web

`@sddp/web` is the web application assembly package for SDDP. It wires together the `ui`, `shell`, and `activities` packages into the browser-facing app.

## Development

```bash
cd src/frontend
npm ci
VITE_API_URL=http://localhost:5001 npm run dev
```

Vite has no API proxy. This command sets the API origin for this local process only;
substitute your API host and port if different. Keep `VITE_API_URL` empty for the
Docker frontend, where nginx proxies `/api` and `/hubs` requests.

The default local development URL is `http://localhost:3500`. Start and provision the evaluation
API/DB/Redis first using the [public guide](../../../../docs/public-guide.md).

## Commands

```bash
cd src/frontend
npm run build
npm run test
```

Run the production build from `src/frontend`; it builds the library packages first and verifies
that the web app consumes their `dist/` exports. A workspace-local `packages/web` build is only a
source-wiring diagnostic and is not the release contract.

## Related Files

- [../../../../docs/public-guide.md](../../../../docs/public-guide.md)
- [../../../../README.md](../../../../README.md)

Only the UI-control and public error-handler unit suites are shipped. `npm test` at the frontend root runs both once; `npm test` in this web package runs only the error-handler suite. L1/L2/L3 and integration test commands are not shipped.

For OnBrick browser tests, follow [public validation](../../../../docs/public-guide.md#validation).
They need separate scenario seed data and a Playwright browser. The recommended
headless smoke has no global setup or Redis flush; it creates a uniquely named Spec,
reads it back and deactivates it afterward. The older headed showcase's global setup clears the evaluation
Redis instance and creates or updates an E2E project. Use that showcase only in a disposable
evaluation environment. Unit tests do not have these service prerequisites.
