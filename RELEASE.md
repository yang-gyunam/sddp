# v0.7.0 ( Free Edition)

Project Search and Service Health are included by default in Free and Pro.

## Project Search

- Search all 12 menus in a project. Results and menu counts reflect your read permissions.
- Use **AND** or **OR**, quoted phrases and parentheses. Narrow results by menu or field and sort by relevance or last update.
- Load 10 matches at a time and open them in their read-only views. File content matches open the corresponding Git revision and line.

## Service Health

- Administrators can open **Settings → System → Health** or the system dashboard. **Overview** shows the latest status for each configured service and highlights failures.
- **Timeline** shows up to 12 checks collected while the page is open. Filter by service or status change to see when a failure or recovery was observed.

## Installation and upgrade

- **New installations** create search storage during database provisioning. [Docker quick start](docs/public-guide.md#quick-start-docker-only)
- **Upgrading from Free 0.6.1** requires V027. Stop writers, back up the database and API files together, and rehearse on a restored copy. Apply V027 with the migration role before starting the updated API and web app. [Project search migration](scripts/db/README.md#project-search)
- **Earlier versions** must complete outstanding migrations before V027. [Upgrade guide](docs/public-guide.md#preserve-files-when-upgrading)
- **Restoring a backup** requires replaying recorded deletions and clearing restored statistics consent before reopening the installation. [Safe restore procedure](docs/deploy/api-data.md#restore-a-backup-safely)

See the [changelog](CHANGELOG.md) for details and the [Free edition guide](docs/public-guide.md#free-edition-support-scope) for edition scope.
