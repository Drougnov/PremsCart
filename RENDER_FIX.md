# Render deployment fix

This version restores the root `Dockerfile.render` expected by the existing Render service and restores ASP.NET static-file + SPA fallback middleware so the React build in `/wwwroot` is actually served by the backend container.

## Existing Render service settings

Keep the service as Docker and use:

- Root Directory: blank (repository root)
- Dockerfile Path: `Dockerfile.render`
- Docker Build Context: repository root / default
- Branch: `main`

The Dockerfile builds both the React frontend and ASP.NET backend into one Render web service and listens on port 10000.

After pushing this version to GitHub, choose Render -> service -> Manual Deploy -> Clear build cache & deploy (or Deploy latest commit).
