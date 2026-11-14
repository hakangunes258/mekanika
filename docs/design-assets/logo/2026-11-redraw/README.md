# Logo — November 2026, the bold redraw

**Adopted. This is what `wwwroot/brand/` holds.** It is the original hand-drawn
logo redrawn as clean vector geometry, not a new idea: same hatched letters, same
dimension lines, gears, ruler and compass. `logo-original-2025.png` is the file it
replaces, kept here for comparison.

An earlier attempt at a geometric monoline identity is in `../2026-11-proposal/`.
It was rejected — read its README before proposing anything like it again.

## The problem it solves

The original goes faint at every size the site actually uses it. The cause is
structural, not a matter of line weight: the letters are an **outline with fine
45° hatching inside**, so they are mostly background. Shrink that to a 200px
sidebar and the hatch fills in to grey while the outline thins to a hairline —
there is no mass left to read.

**The fix is to swap the figure and the ground of the hatch.** Here the letters
are solid and the hatch lines are cut *out* of them in the background colour.
Large, you still read a hatched section; small, the hatch closes up and you are
left with a solid bold wordmark instead of a grey smudge. One change, and the
drawing survives every size without losing its character.

Everything else follows from wanting it to stay legible: consistent heavier
stroke weights, filled arrowheads instead of open Vs (an open arrowhead at 200px
is three stray pixels), and clean geometry instead of sketched wobble.

## How it is built

`sketch.py` holds the geometry, `build.py` assembles the variants, `emit2.py`
writes the SVGs, `raster2.py` shoots the PNGs. **Regenerate; never hand-edit the
output in `wwwroot/brand/`.**

```
python emit2.py && python raster2.py
```

Every letter of MEKANIKA is straight-sided, so each one is a polygon on a 0–100
cap height with a 20-unit stem — no typeface, nothing to outline, no webfont to
fail. The A is the only letter with a counter, and it is derived from the leg
geometry (inner edges parallel to the outer ones, offset 22 horizontally, meeting
at y = 33.3) rather than eyeballed; the first version was a 12-unit slit that
read as a scratch.

**Hatch pitch 12 with a 3.0 cut.** Tested against 11/2.8, 13/3.2, 14/4.0 and
16/4.4: at 16 the letters go stripey and lose mass, which is the original's
failure mode returning.

**A dimension line is interrupted behind the part in front of it**, the way a real
drawing does it — without that the orange lines ran straight through the gear teeth
and read as a mistake. The draw order is dimensions → annotations → letters, and the
interruption is a `<mask>`.

**It was a painted halo first, and that was wrong.** A disc in the paper colour only
disappears on a background that happens to match: the moment the full drawing went
on the home page's blue hero it showed four navy blobs behind the gears, ruler and
compass. A mask removes the line instead of covering it. `_ANN` in `build.py` drives
both the part and the hole it punches, so the two cannot drift apart.

**The hatch cut is transparent for the same reason** — painted in the paper colour it
gave white letters with navy stripes on the blue hero. On the navy sidebar and on
white the transparent version is pixel-identical, so there was nothing to trade away.

**Nothing in the drawing may be painted in the background colour.** It sits on navy,
on white and on a blue gradient; `mark*`'s plate is the one exception, because there
the plate *is* the ground.

## The four shapes, and where each is used

| file | where | why not another one |
|---|---|---|
| `wordmark*.svg` | sidebar, 200px | below ~240px the dimension lines are two hairlines that crowd the word |
| `logo*.svg` | pre-boot loading screen, PDF header | the default lockup: word between its two dimension lines |
| `logo-full*.svg` | home hero (400px), share card, print | gears, ruler, compass, rotation arrows — needs real width to hold together |
| `mark*.svg` | square uses | the wordmark's own M on a plate, hatched the same way |

`*-on-dark` is a **separate file**, not a CSS filter. The old logo was only ever
white because `filter: brightness(0) invert(1)` crushed it, which is exactly why
it could never carry the orange. Pick the right file.

**`favicon.svg`'s M is solid while everything else is hatched.** At 16px the
knocked-out hatch lines are sub-pixel and turn the letter into grey noise. It also
carries its own `prefers-color-scheme` switch, because the tab strip is the one
surface whose background cannot be known.

## Not done

- `site.webmanifest` with Android's maskable icon sizes.
- The share card has not been put through LinkedIn's or X's own preview tools; it
  is the right pixel size but has not been seen through a crawler.
