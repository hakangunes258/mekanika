-- ============================================================================
-- Mekanika — allow user-defined GEAR materials in library_items
-- ============================================================================
--
-- Run this once in the Supabase SQL editor, AFTER library_items.sql.
-- It only widens a constraint; no data is touched and nothing is dropped.
--
-- Why a migration at all: library_items.sql pinned the kind CHECK to
-- ('material', 'bearing', 'bolt'). Gear grades are a fourth kind, because a gear
-- material carries an ISO 6336-5 classification (material group, ML/MQ/ME quality
-- grade, surface hardness) that Models/Material.cs has no fields for, and its
-- allowable stress numbers sigma_Flim / sigma_Hlim are DERIVED from that
-- classification rather than entered. Squeezing it into 'material' would mean a
-- model that is half-empty for every consumer.
--
-- The `name` column carries the "Name - HeatTreatment" label for this kind, not the
-- bare name: C45 appears twice among the built-ins with different heat treatments,
-- and every stored reference (share links, saved calculations) resolves by the pair.
-- The existing case-insensitive unique index therefore already does the right thing.
-- ============================================================================

alter table public.library_items
    drop constraint if exists library_items_kind_check;

alter table public.library_items
    add constraint library_items_kind_check
    check (kind in ('material', 'bearing', 'bolt', 'gear-material'));

comment on column public.library_items.kind is
    'Which library the row belongs to: material | bearing | bolt | gear-material. '
    'For gear-material the name column holds the "Name - HeatTreatment" label.';

-- The RLS policies are written against the table, not against a kind, so the four
-- existing policies already cover this kind. Nothing else to add.
