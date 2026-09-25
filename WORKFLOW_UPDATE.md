# Buying, renting, profiles and wanted posts

This update starts from your uploaded `PremsCart-Modern-Campus(3).zip` and keeps the previous campus pickup flow, site design and your existing project structure.

## What changed

- **Sell and rent the same item:** select Sell, enable “Also offer this item for rent”, and enter a separate daily price. The product page offers Buy / Rent choices. Rental search includes these listings. Checkout calculates the selected price and duration. Accepting either request reserves that one item and closes competing requests; confirming a rental return makes it available again. Negotiated offers remain purchase offers.
- **Profile:** a single card with a soft gradient cover, prominent avatar, separate photo/name/account sections, aligned fields and mobile layouts. Choose a photo, review its preview, then select Save photo. Both the profile and header update after a successful upload. The header uses authenticated image loading. University details remain read-only.
- **Store:** the store directory checks ownership and shows “Manage my store” when you already have one.
- **Wanted:** choose “I have this”, select one of your available sale/rental/giveaway listings, and send it to the requester. An existing or new item conversation opens with an introduction. The requester opens the linked listing and uses its normal transaction choices. If you have not listed the item, “Post an item for this request” creates the listing and sends it afterward. Sending a listing does not reserve it or create an order. Mark your wanted post Fulfilled from My wanted posts when satisfied.
- **Request buttons:** listing cards and product pages show Request sent, Offer sent, View your order, On rent, or Return requested as appropriate. These states are fetched from the backend on reload, after actions, on window focus and periodically while visible. Completed/cancelled requests no longer block another request for an available item.
- **Reviews:** Review buyer / Review seller opens a dialog directly on the completed order. Submit a rating and optional comment without navigating away. Errors keep the form open. Successful submission changes the button to Review submitted, including after reload. Each participant can review once. Rentals become reviewable after the return is completed.
- **Animated loader:** branded initial page loader plus account loading state and action spinners. No artificial wait, reduced-motion support, and a retry option after slow loading. See LOADER_SETUP.md.

## Keep it simple

A combined sale/rental listing represents one physical item. Like rental-only listings, it cannot be added to quantity-based store inventory. Store owners can still post it as a normal campus listing. Remove an existing item from store inventory before enabling rent. Wanted responses reuse the existing chat and order flow; there is no second bidding or payment system.

## Apply the update

1. Keep your current `.env`, local backend configuration, uploaded images and database. Extract this ZIP to a separate folder first if you want to compare changes.
2. Copy the updated source into your existing project directory. Do not replace your actual secrets with example settings. Use the same Docker Compose project directory/name to keep using your existing named volumes.
3. From the project directory run:

   ```sh
   docker compose up -d --build
   docker compose logs api --tail=60
   ```

   The included Compose configuration enables automatic migrations. The new `20260925120000_FlexibleListings` migration adds `Products.AllowRent` and `Products.RentalPrice`, plus a rental-option constraint. Existing listings default to rental disabled; existing rental orders retain their duration and behavior. Do not run `docker compose down -v` when keeping your data.
4. Refresh the browser after the API finishes starting. Without Docker, use your existing connection settings and run `dotnet ef database update` from `backend/PremsCart.Api` if automatic migration is disabled, then restart the API and frontend as described in README.md.

No new frontend runtime dependencies or new services are required.

## Main files

Frontend: ProfileSettings.tsx, RequestState.tsx, ReviewDialog.tsx, WantedReply.tsx, Loader.tsx; integrations in App.tsx, ListingForm.tsx, ProductDetail.tsx, catalog.tsx, Cart.tsx, cart.ts, Transactions.tsx, Stores.tsx and StudentFeatures.tsx. Styling is in experience.css and public/loader.css.

Backend: Product model, product/cart/transaction/wanted/store/account/wishlist controllers, DbContext and migration snapshot, and FlexibleListings.cs.

## Verification

Completed here:
- TypeScript check and Vite production build.
- Chromium tests with mocked API responses: combined listing submission, rental choice and checkout payload, request state after reload, profile name/photo updates, store CTA, wanted response and inline review submission.
- Responsive overflow checks at 320, 390, 768 and 1440 pixels; profile screenshots visually inspected.
- Initial loader and reduced-motion behavior.
- PostgreSQL migration SQL executed with PGlite: legacy defaults, separate prices, and invalid rental-option constraints.
- C# source syntax parsing; Python smoke-test syntax.

The .NET SDK and Docker were unavailable here. A .NET build, EF migration application against your running database, and live backend integration tests were not run. Run these locally before relying on the update:

```sh
# Running local API + Mailpit required; creates unique demo users/data.
python3 tests/flexible_workflows_smoke.py
```

For the mocked browser regression test, keep the frontend running, install Playwright locally for testing, then run from the project root:

```sh
cd frontend
npm install --no-save playwright
npx playwright install chromium
cd ..
node tests/workflows-ui.cjs
```

Screenshots are written to tests/artifacts. Optional UI_URL changes the default http://127.0.0.1:5173. API_URL and MAILPIT_URL configure the live Python test.
