-- ============================================================================
-- Mekanika — user-owned library items (custom materials, later bearings/bolts)
-- ============================================================================
--
-- Run this once in the Supabase SQL editor, AFTER schema.sql. It only adds; it
-- touches nothing that already exists.
--
-- Design notes:
--   * One table for every library, keyed by `kind` — NOT one table per library.
--     Materials ship first; 'bearing' and 'bolt' are already allowed by the CHECK
--     so adding them later needs no migration.
--   * `data` holds the whole item as jsonb, in the exact shape of the C# model
--     (Models/Material.cs). `name` is duplicated out of the jsonb only so it can
--     carry the uniqueness constraint.
--   * Row Level Security is the ENTIRE security boundary — same as calculations.
--     The publishable key is public by design; do not disable RLS.
-- ============================================================================

create table if not exists public.library_items (
    id          uuid primary key default gen_random_uuid(),
    user_id     uuid not null references auth.users (id) on delete cascade,
    kind        text not null check (kind in ('material', 'bearing', 'bolt')),
    name        text not null,
    data        jsonb not null default '{}'::jsonb,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

comment on table public.library_items is
    'User-defined entries added to the built-in reference libraries. One row per item, kind-tagged.';

-- The common query: "my materials".
create index if not exists library_items_user_kind_idx
    on public.library_items (user_id, kind);

-- A user cannot have two materials called the same thing. Case-insensitive,
-- because "C45" and "c45" in the same dropdown would be a usability trap.
create unique index if not exists library_items_user_kind_name_key
    on public.library_items (user_id, kind, lower(name));

-- Reuses the set_updated_at() function created by schema.sql.
drop trigger if exists library_items_set_updated_at on public.library_items;
create trigger library_items_set_updated_at
    before update on public.library_items
    for each row execute function public.set_updated_at();

-- ============================================================================
-- Row Level Security
-- ============================================================================

alter table public.library_items enable row level security;

drop policy if exists library_items_select_own on public.library_items;
create policy library_items_select_own on public.library_items
    for select using (auth.uid() = user_id);

drop policy if exists library_items_insert_own on public.library_items;
create policy library_items_insert_own on public.library_items
    for insert with check (auth.uid() = user_id);

drop policy if exists library_items_update_own on public.library_items;
create policy library_items_update_own on public.library_items
    for update using (auth.uid() = user_id) with check (auth.uid() = user_id);

drop policy if exists library_items_delete_own on public.library_items;
create policy library_items_delete_own on public.library_items
    for delete using (auth.uid() = user_id);

-- ============================================================================
-- Sanity checks after applying:
--   select count(*) from pg_policies where tablename = 'library_items';  -- 4
--   select relrowsecurity from pg_class where relname = 'library_items'; -- t
-- ============================================================================
