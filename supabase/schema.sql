-- ============================================================================
-- Mekanika — Supabase schema for user accounts + cloud-saved calculations
-- ============================================================================
--
-- Run this once in the Supabase SQL editor (Dashboard → SQL Editor → New query)
-- against a fresh project. It is idempotent enough to re-run during development
-- but review before running against a project that already has user data.
--
-- Design notes:
--   * Only two tables. Every module's inputs live in one `calculations` table as
--     jsonb, keyed by `module_key` — NOT one table per calculator.
--   * We store INPUTS, never results. The client re-runs the engine on open, so a
--     months-old saved calculation reflects the current (possibly corrected)
--     engine. `results_snapshot` is a convenience copy for list previews and for
--     detecting "this result changed since you saved it", not a source of truth.
--   * Row Level Security is the ENTIRE security boundary. The anon key is public
--     (it ships in the WASM bundle by design); nothing below is safe without the
--     policies at the bottom. Do not disable RLS.
-- ============================================================================

-- gen_random_uuid() lives in pgcrypto; Supabase enables it, but be explicit.
create extension if not exists pgcrypto;

-- ----------------------------------------------------------------------------
-- profiles: one row per authenticated user, created automatically on signup.
-- ----------------------------------------------------------------------------
create table if not exists public.profiles (
    id                  uuid primary key references auth.users (id) on delete cascade,
    display_name        text,
    company             text,
    plan                text not null default 'free',    -- 'free' today; premium later
    preferred_language  text not null default 'en',      -- ready for the TR/EN i18n project
    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now()
);

comment on table public.profiles is 'Per-user profile, 1:1 with auth.users. Auto-created by handle_new_user().';

-- ----------------------------------------------------------------------------
-- calculations: a saved calculation's inputs, in the same neutral shape the
-- client uses for shareable links (Services/CalculationState.cs).
-- ----------------------------------------------------------------------------
create table if not exists public.calculations (
    id                uuid primary key default gen_random_uuid(),
    user_id           uuid not null references auth.users (id) on delete cascade,
    module_key        text not null,                     -- 'key-connection', 'interference-fit', ...
    title             text not null default '',          -- user-facing label, e.g. "Reducer shaft Ø40"
    inputs            jsonb not null default '{}'::jsonb, -- the whole input state
    results_snapshot  jsonb,                             -- optional: list preview + change detection
    engine_version    text,                              -- which engine produced results_snapshot
    created_at        timestamptz not null default now(),
    updated_at        timestamptz not null default now()
);

comment on table public.calculations is 'Saved calculation inputs (not results). Re-run client-side on open.';

create index if not exists calculations_user_id_idx
    on public.calculations (user_id);

-- Supports the common "my saved <module> calculations, newest first" query.
create index if not exists calculations_user_module_created_idx
    on public.calculations (user_id, module_key, created_at desc);

-- ----------------------------------------------------------------------------
-- Triggers
-- ----------------------------------------------------------------------------

-- Keep updated_at honest on every UPDATE.
create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

drop trigger if exists profiles_set_updated_at on public.profiles;
create trigger profiles_set_updated_at
    before update on public.profiles
    for each row execute function public.set_updated_at();

drop trigger if exists calculations_set_updated_at on public.calculations;
create trigger calculations_set_updated_at
    before update on public.calculations
    for each row execute function public.set_updated_at();

-- Create a profile row the moment a user signs up. SECURITY DEFINER so it can
-- write to public.profiles regardless of the (not-yet-existing) caller session.
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
    insert into public.profiles (id, display_name)
    values (
        new.id,
        coalesce(
            new.raw_user_meta_data ->> 'full_name',
            new.raw_user_meta_data ->> 'name',
            ''
        )
    )
    on conflict (id) do nothing;
    return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

-- ----------------------------------------------------------------------------
-- Account self-deletion (GDPR Art. 17 / KVKK). A logged-in user deletes their
-- own auth.users row; the ON DELETE CASCADE above removes their profile and all
-- their calculations. SECURITY DEFINER because deleting from auth.users needs
-- elevated rights the `authenticated` role does not have on its own.
-- ----------------------------------------------------------------------------
create or replace function public.delete_current_user()
returns void
language plpgsql
security definer
set search_path = public, auth
as $$
begin
    delete from auth.users where id = auth.uid();
end;
$$;

revoke all on function public.delete_current_user() from public;
grant execute on function public.delete_current_user() to authenticated;

-- ============================================================================
-- Row Level Security — the whole security model. Nothing below is optional.
-- ============================================================================

alter table public.profiles     enable row level security;
alter table public.calculations enable row level security;

-- profiles: a user sees and edits only their own row. No DELETE policy — profiles
-- go away via the auth.users cascade, not by direct client delete.
drop policy if exists profiles_select_own on public.profiles;
create policy profiles_select_own on public.profiles
    for select using (auth.uid() = id);

drop policy if exists profiles_insert_own on public.profiles;
create policy profiles_insert_own on public.profiles
    for insert with check (auth.uid() = id);

drop policy if exists profiles_update_own on public.profiles;
create policy profiles_update_own on public.profiles
    for update using (auth.uid() = id) with check (auth.uid() = id);

-- calculations: full CRUD, every operation scoped to the owner. The WITH CHECK on
-- insert/update stops a user from writing rows owned by someone else.
drop policy if exists calculations_select_own on public.calculations;
create policy calculations_select_own on public.calculations
    for select using (auth.uid() = user_id);

drop policy if exists calculations_insert_own on public.calculations;
create policy calculations_insert_own on public.calculations
    for insert with check (auth.uid() = user_id);

drop policy if exists calculations_update_own on public.calculations;
create policy calculations_update_own on public.calculations
    for update using (auth.uid() = user_id) with check (auth.uid() = user_id);

drop policy if exists calculations_delete_own on public.calculations;
create policy calculations_delete_own on public.calculations
    for delete using (auth.uid() = user_id);

-- ============================================================================
-- Sanity checks you can run after applying (should each return the expected):
--   select count(*) from pg_policies where schemaname = 'public';        -- 7
--   select relrowsecurity from pg_class where relname = 'calculations';  -- t
--   select relrowsecurity from pg_class where relname = 'profiles';      -- t
-- ============================================================================
