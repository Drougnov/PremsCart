# PremsCart

Latest update: see [WORKFLOW_UPDATE.md](WORKFLOW_UPDATE.md) for combined buying/renting, profile and avatar fixes, wanted responses, request states, reviews, and safe upgrade instructions. The animated loader is documented in [LOADER_SETUP.md](LOADER_SETUP.md).

A Premier University campus marketplace built with React, TypeScript, ASP.NET Core 10, PostgreSQL and SignalR. This version completes the main workflows described in the supplied audit while keeping a small, understandable university-project structure.

Start with **[COMPLETE_PROJECT_UPGRADE.md](COMPLETE_PROJECT_UPGRADE.md)** for the latest changes, verification rules, installation, and test results. The earlier redesign notes are retained as history.

## Start with Docker Desktop

1. For a new installation, copy `.env.example` to `.env` (for an existing installation, keep your current `.env` and database volumes) and set the four password/key/email values. The JWT key must contain at least 32 bytes and the admin password at least 12 characters. Keep the JWT key unchanged between restarts.
2. Run from this directory:

   ```sh
   docker compose up --build
   ```

3. Open **http://localhost:5173**. The database migrations run automatically. The first administrator is created from `ADMIN_EMAIL` and `ADMIN_PASSWORD` only when no Admin exists. Sign in with those values through the normal login page.
4. Register students using addresses such as `ayesha_44009@bscse.puc.ac.bd`. Open **http://localhost:8025** to read their verification emails in Mailpit. Mailpit captures email locally; it does not deliver to university inboxes.
5. Use two browser profiles (or a normal and private window) to demonstrate buyer and seller accounts.

The Compose frontend uses Vite's development server for a simple local demonstration. Database and image uploads persist in named volumes. `docker compose down` stops the app and keeps that data. Do not use `down -v` unless you intend to erase it.

## Start without Docker for the application

Requirements: .NET 10 SDK, PostgreSQL, Node.js 22, and optionally Mailpit.

1. Create `premscart_db` in PostgreSQL. Alternatively start just the support services using `docker compose up -d db mailpit` after preparing `.env`.
2. Copy `backend/PremsCart.Api/appsettings.Development.example.json` to `appsettings.Development.json` in the same directory. Set the PostgreSQL username/password.
3. In `backend/PremsCart.Api`, configure secrets:

   ```sh
   dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_KEY_AT_LEAST_32_BYTES_LONG"
   dotnet user-secrets set "Bootstrap:AdminEmail" "admin@premscart.local"
   dotnet user-secrets set "Bootstrap:AdminPassword" "YOUR_ADMIN_PASSWORD_AT_LEAST_12_CHARACTERS"
   dotnet run
   ```

   `Database:AutoMigrate` in the example applies all migrations, including the shopping/rentals and marketplace experience migrations. If you disable it, run `dotnet ef database update` using dotnet-ef 10 first. Do not execute `InitialSchema.sql` manually. Without Mailpit, remove the `Smtp` section to print verification/reset codes in the Development terminal instead. Configure real SMTP for actual university delivery.
4. In `frontend`, run `npm ci`, then `npm run dev`. The API listens on port 5000; the frontend on 5173.

## Add demo data

With the Docker stack running, seed predictable local demo accounts, listings, a shop, stock, wanted posts, wishlist activity, an offer, a giveaway request, a completed sale, and a review:

```sh
python tests/seed_demo.py
```

The script uses the application API and Mailpit rather than inserting password hashes directly, so it is safe to run against the local demo database and can be re-run without duplicating the main demo records. Demo accounts use password `CampusDemo!123`: `demo.buyer_44001@bscse.puc.ac.bd`, `demo.seller_44002@bscse.puc.ac.bd`, and `demo.shop_44003@bscse.puc.ac.bd`.

To start from a completely clean database, run `docker compose down -v`, then `docker compose up --build` and seed again. **`down -v` deletes all local PremsCart database and upload data.**

## What's included

- Separate URLs for marketplace, giveaways, listing detail/create/edit, wanted posts, stores, messages, account settings and dashboards. Browser navigation and direct links are supported by Vite.
- Verified student registration, configurable department email mappings, login, reset/change password, profile editing/photo upload and public student profiles without email disclosure.
- Sell, Rent and Giveaway listings, multiple images and primary-image selection, previews, category/condition/price/location/negotiable filters, sorting, pagination, related listings and wishlist.
- Negotiation history with alternating counteroffers, accept/reject and buyer withdrawal. Acceptance reserves stock and closes competing requests when the last unit is reserved.
- Purchases and sales, cancellation with stock restoration, either-party pickup proposals, counterpart confirmation, buyer completion, Sold/GivenAway states, reviews and activity counts.
- Persistent SignalR chat, unread counts, presence and message reporting. Notifications for messages, offers, orders, pickup changes, reviews, wishlist status changes and moderation.
- Student stores, logo upload, inventory, low-stock counts, owner sales totals and restocking.
- Moderator report history, review/message hiding, listing hiding, warnings, suspension/reactivation and reasons. Suspension and password changes invalidate existing sessions.
- Dedicated admin workspace: users/roles, content editing and hide/restore for products, wanted posts, shops and reviews; transaction history, reports, categories and departments. Pickup locations are fixed to Main gate, Canteen, Library. Role checks are enforced by the API.
- Cart with server-validated requests; rental daily prices, preferred pickup/return dates, durations, pickup, return confirmation, and re-listing availability.
- Search suggestions, recent finds, consistent product cards, gallery expansion, responsive filters, mobile bottom navigation, and sticky purchase actions.
- Listing photo previews/reordering, local text drafts, progress timelines, active/history order views, actionable dashboards, searchable admin tables, and notification preferences.

## Roles and pages

| Role | Entry point |
|---|---|
| Student | `/dashboard` with listings, purchases, sales, offers, saved items, wanted posts, reviews and notifications |
| Business Seller | `/dashboard/store` and `/dashboard/store/inventory`; creating a store upgrades a Student account |
| Moderator | `/moderator/reports` and `/moderator/users` |
| Admin | `/admin` plus configuration/user/marketplace pages and moderation |

Create and verify another student account before assigning it a Moderator role under Admin → Users. The last active Admin cannot be demoted. Admin accounts cannot be suspended through the moderation API. Role changes require a fresh login for the affected account.

## University email configuration

The default supports `name_44009@bscse.puc.ac.bd`. The five digits encode a two-digit batch and three-digit student identifier. To enable a further department, add its code/name in Admin → Departments, then map its actual student email domain to that code under `University:Departments` in API configuration. Do not add an unverified domain merely to bypass student verification. For example, the existing mapping is:

```json
{ "University": { "Departments": { "bscse.puc.ac.bd": "CSE" } } }
```

Department entries and allowed email domains are intentionally separate: adding a department does not automatically trust a new email domain. Restart the API after changing configuration.

## Verify the project

```sh
# From frontend/
npm ci
npm run build

# From backend/
dotnet build PremsCart.sln

# From project root, with API and Mailpit running
python tests/smoke.py
python tests/shopping_smoke.py
```

The smoke test creates three uniquely named students and test marketplace records. It verifies registration, email codes, authorization, giveaways, competing requests, negotiation history, pickup agreement/completion, reviews, private profiles, notification ownership and password/session invalidation. Set `ADMIN_EMAIL` and `ADMIN_PASSWORD` in the test process environment to include admin lookup and suspension checks. It is intended for a local demonstration database and does not delete the created records.

For UI regression checks, see `tests/ui-smoke.cjs`. Install its optional dependency with `npm install --no-save playwright` in `frontend`, then `npx playwright install chromium`. With the frontend running, execute `node tests/ui-smoke.cjs` from the project root. These checks mock API data; they do not validate backend behavior.

## Demo walkthrough

1. Register and verify two students using Mailpit. Sign in separately.
2. Seller posts a Sell listing, chooses a campus location and uploads photos.
3. Buyer filters the feed, opens the listing, saves it and starts a chat.
4. Buyer offers a price; seller counters; buyer counters again. Seller accepts. Both see the proposal history and final price.
5. Either participant proposes a future pickup time; the other agrees. After that time, the buyer confirms completion and both may review.
6. Repeat with a Giveaway: request → seller selects a receiver → pickup agreement → completion. The listing becomes GivenAway and profile/dashboard counts update.
7. Open notifications, change a profile picture, recover a password using Mailpit and confirm old sessions no longer work.
8. Create a store, add stock and review its sales summary. Test cancellation restoring stock and restocking a sold-out item.
9. Report a listing, message or review. Moderator opens the queue, enters a reason, hides content or warns/suspends the user. Admin manages roles, products, wanted posts, shops, reviews, categories and departments.
10. Open Rent, select a duration, and check out through the cart. Accept the request as the owner, complete pickup, request a return, and confirm receipt as the owner.

## Scope and verification limits

Rent supports the simple lifecycle described in the shopping notes. Exchange remains unsupported. The project does not process payments. Optional chat attachments, blocking and coupons are not included. Wanted posts link to the requester's student profile; chat remains listing-based. Searchable admin lists and notification history are deliberately simple rather than enterprise-scale.

The frontend production build and mocked browser checks passed during this update. C# source syntax was checked. The migration SQL passed a PGlite PostgreSQL-engine check. **The .NET SDK, standalone PostgreSQL server, and Docker were unavailable, so a backend build, EF migration application, and live multi-account smoke test have not been executed here.** Run the commands above before submission. See `COMPLETION_NOTES.md` for migration and implementation details.
