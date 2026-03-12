# Database Provisioning Scripts

This directory contains the tracked public database provisioning files for SDDP.

## Entry Points

```bash
make db-provision
make db-seed-test
make demo-onbrick
```

## Files

- `provision-base.sql`: schema, indexes, storage setup, and base data
- `provision-test.sql`: shared public test accounts, tenants, and project membership
- `provision.sql`: runs `provision-base.sql` and `provision-test.sql` in order
- `scripts/demo/onbrick-seed.sql`: optional OnBrick showcase seed

These files are copied into the generated public output as the release DB entrypoints.

## Notes

- Do not run shared test data in production environments.
- Scenario-specific story content is not part of the default provisioning path.
- The only public showcase scenario is the OnBrick brownfield story.

## Related Files

- [../../../docs/public-guide.md](../../../docs/public-guide.md)
- [../../../README.md](../../../README.md)
