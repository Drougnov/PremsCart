# Updating the existing Render deployment + showcase demo data

## 1. Update the same Git repository Render already deploys

For a Git-backed Render service, you normally **do not upload the ZIP into Render itself**. Unzip the new project, copy it into the same local Git repository/branch that your current Render service uses, then push that branch.

1. Back up your current local project folder first.
2. Unzip `PremsCart-UX-Overhaul-With-Demo-Data.zip`.
3. Open the local folder of the GitHub/GitLab repository already connected to Render.
4. Keep its hidden `.git` folder. Replace the old project files with the new version's files.
5. Do **not** copy/commit `.env` files, passwords, JWT keys, database credentials, or other secrets.
6. From the repository root, run:

```bash
git status
git add .
git commit -m "Update PremsCart UX and add showcase demo data"
git push origin main
```

If your Render service deploys a branch other than `main`, push that branch instead.

Render normally auto-deploys when you push to the linked branch. If Auto-Deploy is off, open the service in Render and choose **Deploys -> Manual Deploy -> Deploy latest commit**.

If the frontend and backend are two separate Render services but both point to the same repository, check both services after the push. Their existing Root Directory / Dockerfile / build settings should stay as they were unless you intentionally changed your deployment architecture.

## 2. Keep the existing PostgreSQL database

Do **not** create a new database just because the source code changed. Keep your existing `ConnectionStrings__DefaultConnection` environment variable so the new version continues using the same PostgreSQL database.

If your existing deployment already uses:

```text
Database__AutoMigrate=true
```

leave it enabled. The application will apply any required EF migrations at startup. A normal source-code redeploy does not wipe your PostgreSQL records.

## 3. Enable the optional showcase dataset

This version has a startup seeder specifically so a Free Render service does not need Shell/SSH access.

In the **backend Render web service -> Environment**, add:

```text
DemoData__Enabled=true
DemoData__Password=Choose-A-Private-Demo-Password
```

Choose your own private password of **at least 10 characters**. There is no hard-coded fallback demo password.

Then redeploy the backend. On startup, PremsCart creates the showcase records **once**. It uses a marker account, so restarting or redeploying again does not keep duplicating the dataset.

After you confirm the data is visible, you can set:

```text
DemoData__Enabled=false
```

and redeploy. The already-created PostgreSQL rows remain.

## 4. Demo logins

All login-capable showcase accounts use the private password you put in `DemoData__Password`.

| Purpose | Email |
|---|---|
| Tech store owner | `showcase.owner_44901@bscse.puc.ac.bd` |
| Book store owner | `showcase.books_44902@bscse.puc.ac.bd` |
| Clothing store owner | `showcase.style_44903@bscse.puc.ac.bd` |
| Study store owner | `showcase.study_44904@bscse.puc.ac.bd` |
| Student | `showcase.ayesha_44911@bscse.puc.ac.bd` |
| Student | `showcase.nafis_44912@bscse.puc.ac.bd` |
| Student | `showcase.samira_44913@bscse.puc.ac.bd` |
| Student | `showcase.farhan_44914@bscse.puc.ac.bd` |
| Student | `showcase.mim_44915@bscse.puc.ac.bd` |
| Student | `showcase.arif_44916@bscse.puc.ac.bd` |
| Student | `showcase.tisha_44917@bscse.puc.ac.bd` |
| Admin-list example | `showcase.pending_44919@bscse.puc.ac.bd` — intentionally unverified, cannot log in |
| Admin-list example | `showcase.suspended_44920@bscse.puc.ac.bd` — intentionally suspended, cannot log in |

No shared demo Moderator/Admin credential is created. Your existing bootstrap Admin account is unchanged and continues to use your current `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` settings.

## 5. What gets created

The startup seed contains enough data to exercise nearly every major screen:

- 13 dummy users, including active students/business sellers plus unverified and suspended admin-list examples
- 4 stores with descriptions, logos and inventory
- 41 products/listings across **Books, Electronics, Academic Materials, Clothing, Food and Accessories**
- Sell, Rent and Giveaway examples
- real externally hosted CC0/public-domain product photographs
- products that can both sell **and** rent
- negotiable and non-negotiable listings
- visible and hidden store-item examples
- 8 wanted/request posts
- 12 wishlist saves
- Pending, Countered and Rejected price offers plus proposal history
- 11 orders covering Completed, Pending, Accepted, Pickup scheduled, Cancelled, Rented and Return requested states
- a completed Giveaway and a completed/returned Rental example
- 6 reviews
- 4 conversations with read/unread message examples
- notifications
- pending and resolved moderation reports

## 6. Why the demo photos use public URLs

On Render's Free web-service plan, the local filesystem is ephemeral. Files uploaded into the API's local `uploads` directory can disappear after a restart, redeploy, or idle spin-down. The demo dataset therefore saves public Wikimedia Commons image URLs in PostgreSQL instead of uploading demo photos to local disk.

The application was also updated so public `http(s)` demo images render directly, while your normal app-owned `/api/...` images continue using authenticated loading.

The image source/license list is in:

```text
backend/PremsCart.Api/DemoData/IMAGE_SOURCES.md
```

## 7. Important limitation for real user uploads on a Free Render service

This seed solves the **demo-data image** problem, but normal photos uploaded by actual users still use the backend's local upload directory. On a Free Render web service those files are not durable.

For a short university demo, the externally hosted showcase photos are sufficient. For a longer-lived/production version, move uploads to object storage such as Cloudinary, S3-compatible storage, Supabase Storage, etc., or use a deployment option with persistent file storage.

## 8. If the new deployment fails

Check the backend Render **Logs** first. The most useful things to verify are:

- `ConnectionStrings__DefaultConnection` still points to the existing Render PostgreSQL database.
- `Jwt__Key` is still present and at least 32 bytes.
- `Frontend__Origin` matches the deployed frontend URL.
- `Database__AutoMigrate=true` is present if your current deployment depends on startup migrations.
- `DemoData__Enabled=true` is spelled exactly as shown.
- `DemoData__Password` is at least 10 characters.

If you changed dependencies or Docker/build settings and Render appears to reuse stale artifacts, use **Manual Deploy -> Clear build cache & deploy**.
