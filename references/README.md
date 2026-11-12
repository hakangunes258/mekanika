# references/

Standards and reference documents kept locally while a module is being verified
against them. **Nothing in here is committed except this file** — the copies are
licensed and this repository is public and publishes to GitHub Pages.

One folder per module key (the same key used in `ModuleMetadataService`), e.g.
`references/key-connection/`. Any file type is fine: PDF, scan, photo, a page
exported from a textbook.

When a document is added, say which clause or table matters; the verification
work then records in `CLAUDE.md` what was checked and against what, the way the
gear module's anchors are recorded.

## Currently needed

### key-connection
- **DIN 6885-1** — Table 1 in full: `b x h`, `t1`, `t2` for shaft diameters above
  130 mm (the engine's table stops there), and the standard key **length series**
  (… 28, 32, 36, 40, 45, 50, 56 …), which is what a required length should be
  rounded up to instead of to the nearest 5 mm.
- **DIN 6892** — the calculation method, specifically: permissible surface
  pressure (`f_S`, `f_H` support and hardness factors, and how cast iron is
  treated), the load distribution factor for more than one key, the application
  factors, and the limit on load-carrying length (`l_tr <= 1.3 d`).
