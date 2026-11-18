# WAFIF Mandeville

Projects in this solution folder:

- **Mandeville.Database** - SSDT project (everything lives in the `mandeville` schema so it can share a database with Portal): 17 versioned `tbl*` tables and the `sp_Add/Update/Select/Delete*` procedures (plus the workflow procedures: connect, reconciliation, QR check-in).
- **Mandeville.Library** - Dapper repositories (one per table), models and the shared toast/helper classes. References `Data`.
- **Mandeville** - Blazor Web App (InteractiveServer). Menu: Members (+ Attendance), Visitors (3 tabs), Connect, Evangelism, Collections, Expenditure, Reconciliation, Setup. Public phone page: `/checkin/{token}`.

## First run
1. Publish `Mandeville.Database` to a SQL Server database (default connection string expects `(localdb)\MSSQLLocalDB`, database `Mandeville`) and edit `ConnectionStrings:DefaultConnection` in `appsettings.json` if needed.
2. In Setup, add member types, service assignments, departments, payment types and expense categories. Name one department "Connection Team" so the Connect page limits connectors to that team.
3. Set `Church:GroupName` and `Church:Ministers` in `appsettings.json`.
4. QR check-in: for phones to scan the code the app must be reachable over HTTPS (for example Azure App Service). On localhost only the same machine can open the link.

## Notes
- No sign-in yet: every change is stamped `Admin` (see `AppConstants.Who`).
- Delete keeps history: the last state is saved as a version, a tombstone version is added, and the live row (flngVer = 0) is removed.
- Collections and expenses on a finalised reconciliation are locked; reopen the reconciliation to change them.
