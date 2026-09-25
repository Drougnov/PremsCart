# Earlier campus redesign, shopping, and rentals

Historical notes from the first redesign. **[COMPLETE_PROJECT_UPGRADE.md](COMPLETE_PROJECT_UPGRADE.md)** describes the current navy/blue theme, preferred rental dates, expanded features, and current validation results.

This update continues the existing React/ASP.NET project. It uses the uploaded logo and recreates the reference video's centered headline and fanned-card motion in HTML, SVG, and CSS. It does not embed the reference recording as a video background.

## What changed

- Consistent ivory, forest green, and pastel visual theme; self-hosted Manrope headings and DM Sans body text, with font licenses included.
- New homepage hero, four quick actions, wanted-post callout, three-step guide, campus trust cards, header, and footer. Motion can be paused and respects reduced-motion preferences.
- Visible Shop, Rent, and cart navigation. Listing cards show Buy/Rent/Request buttons; detail pages offer Buy now/Rent now and Add to cart.
- A browser-saved cart scoped to the signed-in user, up to 20 distinct listings. Rent listings include a 1–30-day selector. The API checks current availability, seller state, price, pending requests, and stock before creating orders atomically. Cart items are not reservations; seller acceptance reserves them. Each listing represents one item per request.
- Payment is arranged directly at pickup. There is no payment gateway, payment receipt, delivery service, rental deposit, late fee, or future-date rental booking calendar.
- A dedicated admin overview/sidebar for users/roles, products/rentals, wanted posts, shops, transactions, reports, reviews, categories, departments, and fixed pickup locations. Admins can edit content and hide/restore it with an owner notification and moderation record. Transaction history remains read-only for administrators; participants confirm handoffs and returns. Existing role changes, suspension, and report management remain available.
- Admin sign-in opens `/admin`; the student dashboard is no longer the administrator landing page.
- Pickup choices are exactly **Main gate**, **Canteen**, and **Library**, enforced in both the interface and backend. Location management is read-only.

## Rental lifecycle

1. Seller posts a Rent listing with a daily price.
2. Renter selects 1–30 days and places a request through the cart; total = daily price × days.
3. Seller accepts. The item becomes Reserved and competing requests close.
4. Either participant proposes a pickup location/time; the other confirms.
5. After the scheduled time, the renter confirms pickup. The order becomes Rented and the due timestamp is calculated from the actual pickup confirmation.
6. The renter selects “I’m ready to return it”. The owner confirms receipt after the physical return.
7. The order becomes Completed, the item becomes Available again (unless hidden), and reviews are enabled.

Cancellation is available before pickup and releases the reservation. Active rentals cannot be cancelled as purchases. Overdue rentals show a notice; people coordinate directly. Rental inventory is one individually managed listing at a time, outside store stock quantities. Rental terms cannot change during an active request. Once an order exists, changing listing transaction type requires a new listing.

## How verification works

Registration validates the configured university email pattern and department domain. The current default is `name_44009@bscse.puc.ac.bd`. New accounts start with `IsVerified = false`. A six-digit email code expires in 10 minutes; five incorrect attempts lock that code. A valid code sets `IsVerified = true`. Sign-in and authenticated requests require verification and an Active account.

The badge means **university email verified**. It does not independently confirm identity or current university enrollment. Real email delivery requires SMTP configuration; local demonstrations use Mailpit at `http://localhost:8025`. The configured bootstrap administrator is provisioned separately. `/verification` explains this in the interface.

## Apply to an existing installation

Keep the existing database and upload volumes and your current `.env` values. Back up your local database before applying schema changes, then run:

```sh
docker compose up --build -d
```

Automatic migrations apply `20260924120000_ShoppingAndRentals` after the existing migrations. It adds rental dates/duration and content visibility flags, retains order history, and maps obsolete pickup locations to Main gate. Existing Library and Cafeteria records map to Library and Canteen. Listing location text is normalized. Historical completed orders are retained. Pending legacy rental requests without a duration are cancelled; any previously accepted legacy rental needs to be cancelled by a participant and recreated with a duration. Do not reset database volumes to install this update.

Run `python tests/seed_demo.py` for demo accounts/products, including a calculator rental. See README for initial setup and demo credentials.

## Checks performed

- Frontend TypeScript and Vite production build passed.
- Browser checks with mocked API responses passed: hero pause, purchase controls, cart count/deduplication, rental selection/quote totals, checkout failure preservation, success clearing, active-rental controls, student/admin access and landing pages, admin edit/hide dialogs, fixed locations, mobile navigation, dark theme, and reduced motion.
- Checked home, marketplace, listing detail, cart, and admin layouts at 320, 390, 768, and 1440 pixels, with no page-level horizontal overflow.
- Parsed all 26 C# source files for syntax errors: none found. This is not a .NET compilation.
- Executed the initial, completion, and shopping/rentals migration SQL using PGlite's PostgreSQL engine; verified the new columns and three canonical locations. This does not exercise EF migration discovery/model compatibility or the running API.
- The environment had no .NET SDK, Docker, or standalone PostgreSQL server. A backend compilation and live multi-user/API test were not run here.

Before submission, run:

```sh
dotnet build backend/PremsCart.sln
# With the app, PostgreSQL, and Mailpit running:
python tests/smoke.py
python tests/shopping_smoke.py
```

`shopping_smoke.py` checks real rental pricing/authorization, acceptance, pickup, return, review eligibility, and reopening availability. Both scripts create local demo records and leave them for inspection. `tests/ui-smoke.cjs` contains the mocked browser checks; install Playwright in `frontend` and its Chromium browser to run it.

The `previews` folder contains homepage screenshots and illustrative cart/admin screenshots from the browser checks. Cart/admin data in those images is mocked test data.
