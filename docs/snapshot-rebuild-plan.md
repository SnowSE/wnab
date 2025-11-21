Quick plan: I’ll list the remaining work (what to implement), why it’s needed, where to change it, and recommended next steps you can pick from.

**Immediate fixes**
- **Ensure rebuild always starts from last valid snapshot**:  
  - Why: we fixed the one-step case, but the algorithm should locate the nearest previous snapshot that has `IsValid == true` (not only previous month).  
  - Where: `BudgetService.RebuildSnapshots` — change lookup to find the last valid snapshot before target; if none, fall back to `GetEarliestActivityDate()` and create first snapshot.
- **Persist `IsValid` correctly at DB layer**:  
  - Why: the `IsValid` flag must be persisted and used by the DB service; ensure `SaveSnapshotAsync` sets `IsValid = true` and the EF model has column.  
  - Where: `BudgetSnapshotDbService.SaveSnapshotAsync` (already sets `IsValid = true` in current code) and confirm model/migration (`WnabContextModelSnapshot.cs` + snapshot entity).

**Invalidation & DB performance**
- **Use batch SQL UPDATE for invalidation**:  
  - Why: invalidating snapshots by loading entities into memory is slow/inefficient for many snapshots. A single UPDATE is safer and faster.  
  - Where: `BudgetSnapshotDbService.InvalidateSnapshotsFromMonthAsync` — replace the ToList/loop with an EF Core `ExecuteSqlRawAsync` or LINQ `Update` (EFCore 7+ supports ExecuteUpdate) to set `IsValid = false` for matching rows.
- **Mark RTA and future snapshots consistently**:  
  - Why: requirement was to invalidate RTA snapshot and all later snapshots; make sure month/year X invalidates all (year > Y or (year == Y && month >= X)).  
  - Where: `BudgetSnapshotDbService.InvalidateSnapshotsFromMonthAsync`.

**Concurrency & correctness**
- **Add locking / deduping of rebuilds**:  
  - Why: prevent multiple hosts or simultaneous requests from rebuilding the same range concurrently.  
  - Options: a short-lived distributed lock (Redis / DB row lock), or a small `SnapshotRebuildQueue` in DB with "in-progress" flag.  
  - Where: the API endpoint that triggers rebuilds (or in the background worker).
- **Use optimistic concurrency on snapshot writes**:  
  - Why: detect concurrent writes and avoid lost updates.  
  - Where: add a `RowVersion` concurrency token on the snapshot entity (optional but recommended).

**Rebuild behavior & robustness**
- **Rebuild from last valid snapshot (or first interaction)**:  
  - Why: to minimize replay and ensure correctness. Implement searching backward until a valid snapshot is found.  
  - Where: `BudgetService.RebuildSnapshots` (and unit tests).
- **Replay deterministically**:  
  - Why: ensure replay order of interactions (transactions, allocation changes, income) is consistent so rebuilt snapshots match expected outputs.  
  - Where: event-query code in `RebuildSnapshots` / `CreateNextSnapshot` paths.
- **Backups / base snapshot fallback**:  
  - Why: if there is a backup chain beyond snapshots, provide a fallback restore-from-backup option (if you have offline backups).  
  - Where: snapshot restore code path (design doc noted this).

**API & UX**
- **Decide synchronous vs asynchronous rebuilds**:  
  - Why: rebuilding can be expensive. For user interactions, invalidation can be synchronous but actual rebuild should often run in background.  
  - Where: API endpoints that request snapshots (`/budget/snapshot`) and `/budget/snapshot/invalidate` — consider returning accepted/queued status when rebuilds are queued.
- **Add an endpoint to request rebuild status or metrics**.

**Testing**
- **Unit tests** (add / extend):  
  - Tests that `RebuildSnapshots`:
    - Rebuilds when snapshot missing.
    - Rebuilds when previous snapshot exists but `IsValid == false` (we added one).  
    - Finds last valid snapshot (not only previous month).  
  - Tests for `InvalidateSnapshotsFromMonthAsync` to ensure DB update toggles intended rows.
- **Integration tests**:
  - Full scenario: create DB in-memory, seed transactions/allocations, run invalidation + rebuild, assert snapshots match expected RTA and categories (use the `.feature` scenarios).
- **Race & concurrency tests**:
  - Simulate multiple concurrent invalidation + rebuild calls and assert final snapshot is valid and correct (use in-memory DB or integration test harness).

**Migrations & data**
- **Confirm schema**:  
  - Check that the snapshot table has `IsValid` column. If missing, add EF migration and run it. Files: `WnabContextModelSnapshot.cs`, snapshot entity location (search for `BudgetSnapshot` in WNAB.Data), and add migration.
- **Migration strategy**:  
  - Default existing snapshots to `IsValid = true` at migration time unless you want to force revalidation.

**Observability & operational safety**
- **Add logging & metrics**: rebuild start/finish, duration, number rebuilt, invalidation count.  
- **Add retry/backoff for failed rebuilds** and alerts when rebuilds are slow or failing frequently.
- **Add retention/compaction**: prune very old snapshots or compress chains to limit replay windows.

**Suggested prioritized implementation plan**
1. Bulk-invalidation: switch `InvalidateSnapshotsFromMonthAsync` to a single UPDATE (fast, low-risk).  
2. Improve `RebuildSnapshots` to search backward for last valid snapshot (instead of only checking prev month) — deterministic and fixes current bug.  
3. Add unit tests for (2) and for invalidation bulk update (1).  
4. Add a rebuild queue / background worker (async rebuild) to avoid slow request responses.  
5. Add concurrency locking or a small DB “in-progress” marker when a rebuild starts.  
6. Add integration tests exercising full feature scenarios (ReadyToAssignWithSnapshots.feature) and run in pipeline.  
7. Add logging/metrics and consider `RowVersion` if concurrent writes are expected.

**Files & places to change (concrete)**
- BudgetService.cs — ensure last-valid-snapshot logic and deterministic replay. (We already adjusted a bit.)
- BudgetSnapshotDbService.cs — replace memory loop with bulk SQL update for invalidation.
- BudgetEndpoints.cs or ApiProgram.cs — decide whether to queue rebuilds and return 202 Accepted for long rebuilds.
- Snapshot entity & EF model — verify `IsValid` and optionally add `RowVersion`.
- WNAB.Tests.Unit — add unit tests for invalidation and rebuild chain (I can add these).

What I can do next (pick one)
- Implement the bulk `InvalidateSnapshotsFromMonthAsync` update and add a unit test for it.
- Improve `BudgetService.RebuildSnapshots` to search backward for the last valid snapshot and add unit tests.
- Implement a simple rebuild queue (background worker + DB in-progress marker).
- Add integration tests to exercise the scenarios in ReadyToAssignWithSnapshots.feature.

Which of the above should I implement next?