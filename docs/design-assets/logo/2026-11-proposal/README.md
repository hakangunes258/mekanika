# Logo — November 2026, geometric monoline proposal

**Rejected. Not in use.** Kept as a record of what was tried and why it was turned
down; the adopted logo is the bold redraw of the original sketch, in
`../2026-11-redraw/`.

The brief was to replace the faint hand-drawn PNG. This answered it by throwing the
drawing away and building a clean geometric monoline wordmark plus a hex monogram.
It solved the legibility problem and lost the thing that made the logo Mekanika's:
the technical-drawing character — hatched letters, dimension lines, gears, compass.
**That character was the point, and only the execution was weak.** The redraw keeps
the drawing and fixes the execution instead.

Everything below is the original proposal text, left unedited.

## What this replaces, and why

`wwwroot/logo.png` is a raster sketch. It is the site's logo in six places —
sidebar, home hero, favicon, apple-touch-icon, the OG/Twitter image of every page,
and the jsPDF report header — and each one exposes a different limit:

- It is only white because `filter: brightness(0) invert(1)` crushes it. That filter
  is why it can never carry a second colour.
- It is a wide wordmark, so as a favicon at 32px it is unreadable. There is no
  square mark.
- It is a hand-drawn sketch next to a UI built entirely from 1.6px monoline SVG
  icons. The two do not speak the same language.

## The construction

**MEKANIKA contains no curved letter.** M, E, K, A, N, I, K, A are straight strokes
only. So the wordmark is drawn on a grid in the same monoline language as
`Shared/ModuleIcon.razor` rather than set in a typeface: no webfont to load or fail,
no outlines to re-export, identical at any size, and it prints in one ink.

`brand.py` is the source. The SVGs here are its output — regenerate rather than
hand-edit, or the two will drift:

```
python -c "import brand; print(brand.lockup('clean','hex','dark'))"
```

Cap height 24, stroke 3.3, letter gap 5.5, with optical kerning on the four pairs
that need it (`EK`, `KA`, `AN`, `NI`/`IK` — the diagonals of K and A open a gap at
cap height that a uniform advance leaves looking loose).

The orange dimension line under the word is `--signal` (#E07A30, the Şafak colour
added to the palette in this same revision). It carries the one idea worth keeping
from the old logo: a drawing that dimensions itself.

## Files

| | |
|---|---|
| `wordmark-clean*` | word + one dimension line |
| `wordmark-blueprint*` | word + two-segment dimension chain above, line below |
| `logo-*` | the same two, with the hex mark locked up to the left |
| `mark.svg` / `mark-on-dark.svg` | hex plate, knocked-out M, orange base rule |
| `mark-accent.svg` | orange hex, ink M |
| `mark-gear*.svg` | gear plate — most literal, fuzziest at 16px |

`*-on-dark` is for the #2B3550 sidebar and the blue hero: the plate inverts, the
orange does not. **Do not re-introduce a CSS filter to recolour these.** Picking the
right file is the whole point of there being two.

## What shipped

1. `wwwroot/brand/` — `logo.svg`, `logo-on-dark.svg`, `wordmark*.svg`, `mark*.svg`,
   `favicon.svg`, `og.png`, `apple-touch-icon.png`.
2. `Shared/NavMenu.razor` and `Pages/Index.razor` point at `logo-on-dark.svg`;
   `.sidebar-logo` and `.hero-logo` lost `filter: brightness(0) invert(1)` and the
   negative margins that trimmed the old PNG's whitespace.
3. `wwwroot/index.html`: SVG favicon + 180px iOS icon, and the pre-boot loading
   screen's own inline copy of the hero image — it renders before Blazor and is easy
   to miss. `tools/generate-static-pages.mjs` carries a second copy of that same
   markup; the two must match.
4. OG/Twitter now point at `brand/og.png` from `index.html`, the generator, and the
   ten page components that set `OgImage`/`TwitterImage`.
5. `wwwroot/logo.png` was regenerated from the new lockup. It stays only because
   `wwwroot/pdfGenerator.js` loads it for the report header.

## Not done

- A `site.webmanifest` with the maskable icon sizes Android asks for.
- Any check of how the share card actually renders in LinkedIn's or X's own preview
  tools; the card is the right pixel size but has not been seen through a crawler.
