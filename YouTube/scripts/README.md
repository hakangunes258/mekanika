# Mekanika YouTube Scripts

## Production Order

| File | Video | Duration | Priority | Status |
|------|-------|----------|----------|--------|
| `00-channel-trailer.md` | Channel Trailer | 2:30-3:00 | Launch day | ✅ Ready |
| `01-interference-fit.md` | Interference Fit - DIN 7190 | 6:30-7:00 | #1 | ✅ Ready |
| `02-single-bolt.md` | Single Bolt - VDI 2230 | 6:30-7:00 | #2 | ✅ Ready |
| `03-taper-fit.md` | Taper Fit - DIN 7190 | 6:30-7:00 | #3 | ✅ Ready |

## Workflow for Each Video

1. **Prepare demo values** — listed at the bottom of each script
2. **Record screen** — OBS Studio, 1920×1080, 30 FPS, incognito browser
3. **Generate voiceover** — paste script sections into ElevenLabs (Adam/Josh voice)
4. **Edit** — DaVinci Resolve: sync audio + screen recording, add intro/outro
5. **Create thumbnail** — Canva 1280×720 using branding-specifications.md
6. **Upload** — use title/description/tags from each script file

## ElevenLabs Tips
- Paste one section at a time (between `---` dividers)
- Settings: Stability 75%, Clarity 80%, Speed 0.95x
- Export as MP3, 192 kbps minimum
- Listen back before recording screen — timing may need adjusting

## OBS Settings
- Resolution: 1920×1080
- Frame rate: 30 FPS
- Encoder: x264, CRF 18
- Clean browser: incognito, no bookmarks bar, no extensions
- Desktop: hide personal files

## Upload Checklist (Per Video)
- [ ] Title matches script exactly
- [ ] Description includes all timestamps
- [ ] Tags added (20 tags)
- [ ] Thumbnail uploaded (1280×720)
- [ ] Category: Science & Technology
- [ ] End screen: subscribe button + related video
- [ ] Cards: link to calculator URL at relevant timestamps
- [ ] Auto-captions enabled → review for technical terms

## Related Files
- `channel-setup.md` — channel configuration, description, sections
- `branding-specifications.md` — colors, fonts, dimensions
- `phase1-checklist.md` — step-by-step setup tasks
