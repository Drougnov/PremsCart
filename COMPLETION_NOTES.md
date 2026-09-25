# Completion notes

> Historical notes from the earlier update. Current behavior, including rentals, the new logo/theme, admin management and fixed pickup locations, is documented in REDESIGN_AND_SHOPPING_NOTES.md.

The supplied Phase 9 code was extended rather than replaced. The existing controllers and React components remain recognizable so the project is suitable for a university presentation and viva.

## Mapping to the audit

| Audit target | Implementation |
|---|---|
| Password reset/change | AccountController; hashed, purpose-separated, expiring reset codes with attempt limits; token version invalidation |
| Profile images/public profiles | AccountController; private image delivery; activity counts; existing Community reputation endpoint |
| Notifications | Existing entity activated through DbContext save events and moderation actions; inbox/read/read-all API and UI |
| Sold/GivenAway | Transaction completion, dashboard/profile counts, migration for identifiable historical completions |
| Discovery | Condition, negotiable, structured campus-name location, sorting, category-related results |
| Offer history | OfferProposal rows and LastProposerId; both participants counter in turn; original buyer can withdraw |
| Order cleanup | Close competing pending offers/requests when the last item is reserved |
| Two-sided pickup | Proposed/Confirmed/Completed; proposer stored; other party confirms; rescheduling resets agreement |
| Moderation | Review/message report targets, hidden content, warnings, suspension and resolution reasons/history |
| Admin | Role/user controls; category/department/location CRUD; statistics and listing/transaction overviews |
| Bootstrap | Configuration-driven first Admin, only when none exists |
| Stores | Logo, inventory, low-stock and completed-sales summaries; restock after final status |
| Routed UI | History API router, distinct URLs, role-aware menus, responsive dashboard shell, cross-page feature actions |
| Local delivery | Mailpit and PostgreSQL Compose services, optional full application containers, setup guide and smoke tests |

## Migration

`20260924000000_Completion` follows the two existing migrations. Back up an existing Phase 9 database before applying it. It adds account state, session version, password-reset records, offer proposals, pickup agreement fields, report resolution fields and hidden-content flags.

Existing offer rows can preserve only their latest stored amount; the earlier counteroffer values were overwritten by the old application and cannot be reconstructed. New negotiations retain every proposal. Existing scheduled pickups become Proposed and need counterpart confirmation. Historical completed orders are marked Completed for pickup status; their exact completion timestamp is unknown and remains null. Identifiable completed unavailable Sell/Giveaway products receive Sold/GivenAway. Legacy Rent/Exchange records are retained but are not offered as supported new workflows.

The migration's Down method deliberately requires a database backup rather than deleting new history silently. The model snapshot is updated. The JSON contracts mostly retain the original property names so existing components remain usable.

## Implementation notes

- Notifications are added in the same SaveChanges transaction as domain changes; there is no queue or worker. The header polls every 30 seconds; chat itself remains SignalR.
- Store statistics count the owner's completed transactions. They are simple seller totals, not accounting reports or product-specific analytics.
- Pickup uses a campus location ID on orders. Listings use validated campus location names, keeping the existing schema small. Renaming a location changes future listing choices; old listing text is retained.
- Content hiding masks stored chat history and previews on the next fetch. Text already displayed in another participant's open browser is not remotely erased.
- All marketplace pages require a verified account; “public student profile” means visible to other signed-in campus members.
- Authentication and authorization run on the server. Suspended accounts cannot log in or use old tokens; live chat checks account/session state before sending/joining.
- Uploads check type signatures, size and ownership. They are stored on disk/in a Docker volume, with no cloud storage requirement.
- Admin/moderator lists are intentionally small-project lists with frontend filtering. Notification UI shows the newest 100 entries; the unread total covers all entries and “mark all read” covers the entire inbox.
- New writes use simple controller/EF patterns. For a public high-traffic deployment, further concurrency, abuse-prevention, logging and operational work would be needed; that is outside this university-project scope.

## Validation performed

- `npm ci` and `npm run build`: passed, strict TypeScript and Vite production bundle.
- Browser checks against mocked API responses: 26 routed screens loaded with no JavaScript errors; request/report cross-page actions, buyer counter-counter controls, pickup confirmation, student access guard, store pages, and mobile page overflow checks passed.
- Desktop dashboard and mobile marketplace screenshots inspected.
- Initial schema and completion SQL executed successfully in PGlite (embedded PostgreSQL); this does not validate EF migration execution.
- All C# files parsed without syntax errors. This is not equivalent to a .NET build or EF runtime verification.
- Python smoke script syntax checked. Live smoke tests are supplied but not run here because the required backend runtime/database were unavailable.

Recommended final local gate: start Compose, run the API smoke test, then perform the two-browser chat/store/moderation walkthrough in README. This is the remaining runtime verification, not a claim that it already passed.
