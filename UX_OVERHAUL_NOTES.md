# PremsCart UI/UX overhaul — 25 September 2026

This update focuses on making PremsCart understandable to a first-time student without requiring marketplace jargon or knowledge of where features are hidden.

## Navigation and language

- Main navigation now has clear, dedicated destinations: **Buy**, **Rent**, **Free**, **Requests**, **Stores**, and **Messages**.
- The student sidebar is grouped into **Overview**, **Orders & deals**, **Community**, **Store**, and **Account** so personal tools do not compete with public browsing pages.
- User-facing copy uses **item**, **post**, **request**, **saved item**, and **store item** instead of marketplace-specific words such as “listing” and “wishlist”. Internal route/API names remain unchanged for compatibility.
- The home hero now explains PremsCart immediately: it is a marketplace for Premier University students to buy, rent, sell, or give away items and arrange campus pickup.
- Buy, Rent, and Free pages have their own purpose text and fixed transaction type. Their filters no longer duplicate the same type switch.
- Pickup location was removed from marketplace filters.

## Readability and visual hierarchy

- Added `frontend/src/ux-overhaul.css`, loaded after the existing stylesheets, to raise text sizes, form-control sizes, button tap targets, spacing, and status readability across desktop and mobile.
- Product cards, forms, account screens, cart, help, admin tables, stores, orders, notifications, and mobile navigation received explicit readability minimums.
- Dashboard was simplified to an overview with direct links instead of duplicating full order/activity views.
- The old “Recent exchanges” dashboard block was removed; order history now lives only in the dedicated Buying / Selling & renting pages.

## Messages and notifications

- Conversation sidebar has a larger search box and a proper **Unread only** button instead of a raw checkbox.
- Unread conversations are visually distinct and display an unread count.
- Sent messages show **Sent** or **Seen** state.
- Notifications now show a category badge (Message, Price offer, Order, Rental, Saved item, Review, Safety update, Account) plus a separate Read/Unread badge.

## Pickup scheduling

- Pickup scheduling now uses a native **calendar date picker** and a separate **time dropdown** in 30-minute steps (08:00–20:00), instead of requiring a date/time string to be typed manually.
- Pickup proposals are displayed in a structured summary with place, date/time, agreement state, and next action.

## Store management

- When a seller creates a store, their existing posted items are automatically added to it.
- Any new item posted after that is automatically added to the seller's store.
- Store owners can show/hide an item in their storefront without deleting the original post.
- Store items can be reordered by drag-and-drop, plus **Up / Down** buttons for phones and keyboard users.
- Store inventory and store profile are separated into dedicated pages.
- Rental/single-item posts do not use stock quantities; normal sale items keep stock controls.

## Whole-Taka prices

- Sale price, rental price, offers, counteroffers, request budgets, admin price edits, and marketplace minimum/maximum price controls use `step=5` and whole-Taka values.
- Backend validation now rejects decimal prices/offers/budgets and decimal min/max price filters instead of relying only on the browser controls.

## Admin verification

- Admin user management now has a **Verify account** action for unverified students.
- This can verify a user without an email code after the administrator has checked their campus identity through an appropriate process.
- The action clears outstanding verification codes and notifies the user.

## Database update

A new EF Core migration was added:

`20260925220000_StoreMerchandising`

It adds `IsVisible` and `SortOrder` to `StoreProducts` and backfills existing seller items into existing stores.

Apply migrations before using the updated store features:

```bash
cd backend/PremsCart.Api
dotnet ef database update
```

If the deployment uses `Database__AutoMigrate=true`, the application will apply pending migrations during startup.
