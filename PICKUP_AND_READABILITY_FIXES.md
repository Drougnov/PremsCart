# Pickup and readability fixes — September 25, 2026

- Increased homepage section copy, category/card text, navigation, and footer typography. Hero text sizes stay the same.
- Removed the hero pause button and hover translation/rotation at every breakpoint. Hover/focus still brings a card to the front. Automatic motion and system reduced-motion support remain.
- Redesigned order pages with a centered, padded layout, a separate back/refresh row, a clear pickup summary, readable progress steps, and grouped actions.
- The current proposer sees that they are waiting; the other participant gets **Agree to this pickup** and can instead suggest a different pickup. Every new proposal requires the other participant to agree again.
- Agreed pickups show both participants that they should wait until the selected date/time and meet at the shown location.
- Handoff confirmation is hidden before the agreed time and appears automatically for the buyer when that time arrives. The buyer must physically receive the item before confirming. Sellers see the appropriate buyer-completion guidance.
- Order data refreshes every 15 seconds while the page is visible and when the window regains focus. Refresh remains available manually.
- Progress now distinguishes agreeing on a pickup from awaiting the handoff. Completed orders mark every step, including the last one, as complete. Rentals include their return stages.
- Expired proposals provide guidance to choose a new time. Server-side authorization and early-handoff rejection remain enforced; API conflict messages are clearer.

## Install

Keep your existing `.env`, database and uploads. Replace the application source with this version, then run from the project folder:

```sh
docker compose up --build -d
```

No new database migration is needed for these fixes. Do not delete your Docker volumes. Refresh the browser after rebuilding.

## Checks

The frontend production build passed. Focused mocked-browser checks cover hover behavior, alternating buyer/seller proposals, agreement, waiting states, time-based button visibility, completed sale/giveaway/rental progress, and responsive pages. These are frontend checks, not a live multi-user API run.

`tests/pickup-ui.cjs` contains the focused browser checks (same Playwright setup as `ui-smoke.cjs`). `tests/shopping_smoke.py` now also checks alternating proposals, proposer self-confirmation rejection, and early handoff rejection against a running local API. The live script was not run here; .NET and Docker are unavailable in this environment.

The latest screenshots are `previews/pickup-*.png`; older screenshots illustrate the earlier design.
