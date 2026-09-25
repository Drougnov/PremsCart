# Complete project upgrade

Latest follow-up: see **PICKUP_AND_READABILITY_FIXES.md** for the September 25 pickup and typography corrections.

This continues the supplied PremsCart project as a manageable university project: React/TypeScript, ASP.NET Core 10, PostgreSQL, and SignalR. It includes the source, migrations, demo seed script, test scripts, and illustrative screenshots.

## What changed

| Area | Current behavior |
| --- | --- |
| Shared design | White/light gray surfaces, navy typography, blue actions, restrained accent colors, local Manrope and DM Sans fonts, uploaded logo, consistent cards, forms, dialogs, and footer. Dark mode is retained. |
| Homepage | Reference-inspired animated card fan with reduced-motion support, categories, real aggregate counts, recent available listings, rental shelf, quick actions, improved how-it-works and campus trust sections. The reference recording is recreated with HTML/SVG/CSS rather than embedded as a background video. |
| Navigation | Clear Shop, Rent, Giveaways, Wanted, Stores and Messages; visible cart count and notifications; search with recent searches and title suggestions; mobile Home/Browse/Sell/Messages/Account navigation. |
| Marketplace | Consistent Buy now/Rent now/Request for free actions, colored listing types, wishlist hearts, prices, condition, seller and pickup location. Search, sorting, chips and compact filters; a bottom filter drawer on phones. Filter URLs survive reloads. |
| Product details | Gallery thumbnails, cover identification, photo counter, enlarged gallery, description, availability, seller verification/reputation/completed exchanges, related cards, recent finds, safety advice and mobile purchase bar. |
| Cart | Browser-saved per-account cart; remove items, change rental duration/dates, check current totals and availability, send requests, and recover from price/availability conflicts. The API validates every checkout; adding to cart does not reserve stock. |
| Rentals | Daily prices, preferred pickup and return dates, 1–30-day duration and calculated total; seller acceptance, pickup, active rental, return request, owner return confirmation and restored availability. |
| Listing editor | Grouped information/pricing/pickup/photos, drag-and-drop or file selection, five-photo limit, previews, reorder/remove controls, cover selection, text drafts and a final preview dialog. Failed photo uploads can be retried without creating another listing. |
| Orders | Contextual next step, sale/rental progress timeline, active versus completed/cancelled filters, search, direct order links, pickup arrangement, return controls and participant-only order conversations. |
| Student dashboard | Compact activity counts, requests/offers/messages needing attention, upcoming pickups and recent orders. |
| Stores | Searchable directory, storefront identity/logo and reputation, product search/type filters, consistent cards and wishlist actions, owner stock controls. |
| Administration | Separate landing page/sidebar, overview and recent moderation activity, searchable/paged user and content tables, role/account dialogs, product/wanted/shop/review editing and visibility management, reports, transaction/rental monitoring, categories and departments. |
| Feedback and settings | Toast feedback, loading skeletons, helpful empty/error/retry states, native accessible dialogs, keyboard focus treatment, notification preferences, help/FAQ and campus safety guidance. |

Individual marketplace records remain available only to signed-in verified members. Guests see real aggregate statistics and category links, with a sign-in invitation for the listing shelf. No private listing data or member emails are exposed merely to fill the homepage.

## Verification: what the badge means

A new account starts unverified. Registration checks the configured university email format and department domain. A correct six-digit email code, valid for 10 minutes with at most five incorrect attempts, sets `IsVerified` to true. Login requires a verified, active account.

The badge means **university email verified**. It does not certify identity or current enrollment against university records. Development uses Mailpit at `http://localhost:8025`; configure actual SMTP to verify access to real university inboxes. The bootstrap administrator is provisioned separately from the configured credentials.

## Four demonstration journeys

1. **Buy:** browse → listing → Buy now/Add to cart → send request → seller accepts → agree pickup → buyer confirms physical handoff → optional review.
2. **Rent:** choose preferred dates/duration → send request → owner accepts → agree pickup → renter confirms pickup → rental due date → renter requests return → owner confirms receipt → listing becomes available again.
3. **Sell:** Post → complete grouped form → add/reorder photos → preview → publish → review requests in Sales → accept → arrange pickup → handoff.
4. **Give away:** publish Giveaway → another student requests it for free → giver accepts → agree pickup → recipient confirms handoff.

Rentals deliberately use one item per listing. Preferred dates are a request, not a calendar booking. Acceptance reserves the item immediately until cancellation before pickup or confirmed return. The due timestamp starts from actual pickup confirmation. There are no overlapping future bookings, deposits, late-fee calculations or payment processing. Payment is arranged directly at pickup. Store quantity inventory is separate from individual rental listings.

Pickup choices are **Main gate**, **Canteen**, and **Library** throughout the UI and API. Administrators can inspect them but cannot add, rename or delete them.

## Install/update

For an existing installation, keep your `.env`, upload volume, and database volume. Back up your database, replace the source files, then run from the project directory:

```sh
docker compose up --build -d
```

Do not use `docker compose down -v` to install this update: it deletes your data. For a fresh installation, follow README.md and copy `.env.example` to `.env` before setting credentials.

Automatic migrations apply the existing migrations followed by:

- `20260924120000_ShoppingAndRentals`: rental lifecycle columns, visibility flags, and normalization to the three allowed pickup locations. Historical completed orders remain. Legacy pending rental requests without a duration are cancelled; previously accepted legacy rentals should be cancelled and recreated by their participants.
- `20260924140000_MarketplaceExperience`: nullable preferred rental pickup date and five notification-preference columns. Existing accounts start with all five preferences enabled.

Notification preferences affect newly generated in-app notifications for messages, offers, purchases/giveaways, rentals, and saved-listing availability. They do not remove old notifications, block chat delivery, or silence account safety/moderation notices.

Local drafts, cart, recent searches and recently viewed item IDs are scoped to the account in this browser. They do not synchronize across devices. Photos are not stored in text drafts. Recently viewed data contains IDs; current item details are fetched through the authorized API.

## Validation and its limits

- TypeScript checking and Vite production build passed.
- Browser workflow checks use a mocked API to exercise the frontend. They cover discovery, wishlist, filter validation/URL persistence, gallery controls, rental dates and quote totals, failed/successful checkout, listing drafts/preview/publish, order history/timeline, dashboard next steps, preferences, storefront search, and administration dialogs/paging. Additional checks passed for free-request checkout and photo preview/reordering/removal/type validation.
- Responsive browser checks cover representative home, shopping, listing, cart, order, dashboard, store, settings, help and admin pages at 320, 390, 768 and 1440 pixels. The order timeline becomes vertical on small screens.
- All 28 C# source files were parsed without syntax errors. This is a syntax check, not a .NET compilation.
- Initial, completion, shopping/rentals and marketplace experience SQL ran successfully using PGlite's PostgreSQL engine. Checks covered the canonical locations, rental/visibility/date columns, and enabled notification defaults. This does not validate EF migration discovery or runtime query translation.
- A .NET SDK, Docker and a standalone PostgreSQL service were unavailable here, so backend compilation and live API integration remain to be run locally.

Before submission, run:

```sh
dotnet build backend/PremsCart.sln
# With the application, PostgreSQL and Mailpit running:
python tests/smoke.py
python tests/shopping_smoke.py
```

The live scripts create disposable demo accounts/listings and retain them for inspection. The shopping test covers rental dates/pricing, preferences, role checks, conversations after acceptance, pickup/return, review eligibility, and renewed availability.

To repeat the mocked browser checks, start the frontend on port 5173, then in `frontend` run `npm install --no-save --package-lock=false playwright` and `npx playwright install chromium`. From the project root, run `node tests/ui-smoke.cjs` and `node tests/ui-extras.cjs`. Optionally set `BASE_URL` for another local frontend port. These tests replace API responses in the browser and do not validate the running backend. Screenshots in `previews` use illustrative test data, not real student records.
