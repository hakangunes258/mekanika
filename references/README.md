# Reference standards

Drop the PDFs or scans a module needs into a folder named after its URL key,
e.g. `references/key-connection/`.

**Nothing in here is ever committed.** `.gitignore` carries
`references/*` with `!references/README.md`, so only this file is tracked.
The repository is public and publishes to GitHub Pages; licensed copies of
standards must not reach it. (The ISO 6336 PDF used earlier carries a
Beuth/SNV licensee watermark, which is exactly why this rule exists.)

Reproducing a standard's *tables* is a separate legal question from citing it
or using its method. The site already embeds several — see the note on quoting
standards in CLAUDE.md — and that exposure is worth a lawyer's opinion before
adding to it.

---

## What each module is waiting for

After the Nov 2026 review, these are the findings that could not be fixed
without the source. Everything else in the review has been done.

### key-connection — DIN 6885-1, DIN 6892
- DIN 6885-1 Table 1 above 130 mm shaft diameter (36×20, 40×22, 45×25,
  50×28, 56×32 …) to extend `StandardKeys`.
- DIN 6885-1 standard length series (… 28, 32, 36, 40, 45, 50, 56 …) to
  replace the 5 mm rounding of `RequiredKeyLength`.
- DIN 6892's permissible pressure method: the support factor f_S, the
  hardness factor f_H, the equivalent torque, and how it treats cast iron
  (which has no yield point, so the 0,9·R_e stand-in is doubly wrong there).
  **`ModuleMetadataService` advertises DIN 6892 with `IsVerified = true`
  while the engine does not implement it — decide the flag and the public
  description together when this arrives.**

### compression-spring, extension-spring, torsion-spring — EN 10270
- R_m as a function of wire diameter for each class (SH, DH, VDC, VDSiCr,
  1.4310 …). All three modules currently use one value per class, so a thick
  wire is credited with more strength than the standard allows and a thin one
  with less.
- EN 13906-2's permissible initial tension band for extension springs: the
  current 0,05–0,20·R_m is a flat band where the standard reads it off a curve
  against the spring index and the winding method.

### ball-bearing — ISO 281, catalogues
- ISO 281 Table 3 for the 15° angular contact series: e and Y as functions of
  i·Fa/C0. The single catalogue pair is used over the whole range today and
  the results say so.
- Current manufacturer values for the 68xx/69xx series. 6305, 6807 and 6809
  disagree with NSK, and 6806/6807 and 6808/6809 share identical C0 values,
  which looks like a copy-paste.

### roller-bearing — catalogues
- NU314's C0 (220 kN here, 168 in NSK), and the pairs that share one C across
  different sizes: NU315/NU316, NU318E/NU319E, NU252/NU256, and
  H-E32213/H-E32214 which share both C and C0.
- NJ and NUP data if those types are to be offered; the dropdown now says
  "NU series" because that is all the database holds.

### gear-pair — ISO 1328-1:1995
- The 1995 grades alongside the 2013 classes. ISO 6336-1:2006 normatively
  references the 1995 edition, and the mismatch is the dominant remaining
  difference against the reference reports (conservative, about 4 % on S_H and
  8 % on S_F).

### internal-gear — a reference report
- Any KISSsoft-class run of an internal pair. `IsVerified = false` until one
  exists. The assumptions most likely to move: K_V with z_n2 → ∞ and the
  annulus as infinite mass, ISO 6336-3 Eq. (11) (ρ_fPv, not implemented), and
  the annulus root fillet ρ_F2.

### single-bolt — VDI 2230
- The eccentric branch, R3 to R5: I_Bers, s_sym, a, u and the gapping limit.
  Not implemented; the input field is l_A, not the eccentricity.
- R9's thread stripping calculation. A practice minimum engagement by material
  is used instead, and the results say which.
- Table A8's own α_A values, to confirm the ones now in `BoltService`.
- Table 5.4/1's embedding amounts per interface, to confirm f_Z.
