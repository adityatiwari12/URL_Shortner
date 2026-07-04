# Future Plan

Roadmap for extending this project beyond the current college-project scope. Nothing here is built yet — this is a reference for what to do next, in what order, and why.

---

## Phase 1 — Quick feature wins

No architecture change needed, all fit into the existing `Urls` table / layering.

| Feature | What changes |
|---|---|
| Custom alias (`/mylink` instead of random code) | Extra input field, same uniqueness check `DatabaseHelper.ShortCodeExists` already does |
| QR code per short link | Generate client-side (e.g. a QR JS lib) from the existing `shortUrl` value, no backend change |
| Link expiry | Add `ExpiresAt` column; redirect endpoint checks it, returns `410 Gone` if past |
| Edit destination URL after creation | New `PUT /api/url/{shortCode}` endpoint, one `UPDATE` query |
| Bulk shorten (paste multiple URLs) | Either loop client-side calls to the existing POST endpoint, or add a batch endpoint |

---

## Phase 2 — User accounts (the fork point)

Everything past this point depends on links belonging to a *user*, not one global table. Do this before analytics/dashboards/private-links — retrofitting auth onto an existing global table later is much more painful than building it in now.

- Add authentication (JWT or cookie-based).
- Add `UserId` FK column to `Urls`.
- Scope `GET /api/url` to the logged-in user's links; add an admin/global view separately if needed.
- Unlocks: "my links" dashboard, private links, per-user rate limits, password-protected links.

---

## Phase 3 — Analytics

- Don't just increment `ClickCount`. Add a separate `Clicks` log table: one row per click (`ShortCodeId`, `ClickedAt`, `Referrer`, `UserAgent`, `IpHash` or geo). A single counter can't produce a time-series graph or a referrer breakdown after the fact — the raw log is what makes that possible.
- Build charts (clicks over time, top referrers, device/browser split) from that log table.

---

## Phase 4 — Scaling

What breaks first, in order, as traffic grows:

1. **Redirect is the hot path, not create.** Every redirect currently does a synchronous `SELECT` + `UPDATE`. Fix: cache `ShortCode → OriginalUrl` in-memory or Redis, serve the redirect from cache, and push click-count increments to a queue/log table aggregated asynchronously instead of updating the row on every hit.
2. **Shortcode generation degrades at scale.** Current approach: random 6-char code, retry-until-unique against the DB (up to 10 attempts). Fine at low volume; collision retries increase as the table fills. Swap to base62-encoding the auto-increment `Id` — guaranteed unique, zero DB round-trip needed to check.
3. **The app is already stateless** (no in-memory session state), so it can run as N instances behind a load balancer with no code change, once the DB is externalized (already true today — it's SQL Server, not LocalDB).
4. **DB becomes the single bottleneck eventually.** Add a read replica for the list/analytics endpoints once traffic grows; keep writes (create/redirect) on the primary.
5. **Rate limit `POST /api/url`** at the API gate to stop abuse/spam shortening before any of the above matters.

---

## Suggested order

1. Phase 1 features (cheap, immediate value)
2. Phase 2 user accounts (unblocks everything else)
3. Phase 3 analytics (needs the `Clicks` log table decided up front, before Phase 4 caching changes the write path)
4. Phase 4 scaling (only once there's real traffic to justify it)
