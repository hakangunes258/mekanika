# Supabase setup — the console steps only you can do

The repo has the schema; these are the steps that need your Supabase account. Do them
in order. Nothing here touches the live site — the app is not wired to Supabase yet
(that is phase 4).

## 1. Create the project

1. Sign in at <https://supabase.com> → **New project**.
2. **Region: Frankfurt (eu-central-1)** — matters for KVKK/GDPR, since EU users' data
   should stay in the EU. Do not pick a US region.
3. Set a strong database password (store it in your password manager, not here).
4. Free tier is fine for now. Note: a free project is **paused after ~1 week of no
   activity** — harmless at current traffic, but know it happens.

## 2. Apply the schema

1. Dashboard → **SQL Editor** → **New query**.
2. Paste the entire contents of [`schema.sql`](schema.sql) and run it.
3. Run the three sanity checks at the bottom of that file. You should get:
   - `7` policies, and RLS = `t` (true) on both tables.
4. **Verify RLS is really on** (Dashboard → Table Editor → each table shows an
   "RLS enabled" badge). This is the entire security boundary — if it is off, every
   user's data is world-readable through the public anon key.

## 3. Enable authentication

1. Dashboard → **Authentication → Providers**.
2. **Email**: enable, and turn ON "Confirm email" (magic-link / OTP). Turn OFF
   password sign-in if you want passwordless-only (recommended — no password storage
   or reset flow to own).
3. **Google**: enable, and paste a Google OAuth client ID/secret (create one in Google
   Cloud Console → Credentials → OAuth client → Web application).
4. Dashboard → **Authentication → URL Configuration**:
   - **Site URL:** `https://mekanika.org`
   - **Redirect URLs:** add both `https://mekanika.org/**` and, for local dev,
     `http://localhost:5199/**`. The auth callback returns to one of these, and it
     must survive the GitHub Pages `404.html` SPA rewrite — test it in phase 4.

## 4. Collect the two public values

Dashboard → **Project Settings → API**:

- **Project URL** (e.g. `https://xxxx.supabase.co`)
- **anon public key**

Both are safe to ship in the WASM bundle — that is by design, and RLS is what keeps
data private. **Never** put the `service_role` key in the client; it bypasses RLS.

Hand these two to me (or drop them into `wwwroot/appsettings.json` under a
`Supabase` section) when we start phase 4, and I will wire up auth + the storage
service.

## 5. Legal (before any real user signs up)

- Fill in the placeholders in `Pages/Privacy.razor` (legal entity, jurisdiction) and
  have someone confirm it covers KVKK + GDPR for your situation. It is a drafted
  starting point, not legal advice.
- The About page currently claims "0 Server Uploads" / "No data is sent to any
  server". That becomes false once accounts exist — it will need softening in phase 4
  to "calculations run locally; saving to your account is optional and opt-in".

## What is NOT done yet

- No app code talks to Supabase (phase 4: auth + `AuthenticationStateProvider`).
- No cloud save UI (phase 5: `SaveCalculation` + `/my-calculations`).
- The privacy page is not linked from the nav yet (added in phase 4 alongside auth).
