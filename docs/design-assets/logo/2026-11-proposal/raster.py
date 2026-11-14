"""Build the three raster brand assets with headless Chrome.

Nothing here can be drawn by hand: the OG card wants a real 1200x630 at the ratio
crawlers crop to, iOS wants its own 180px square (it composites transparency onto
black, so the icon carries its own ground), and jsPDF loads a PNG for the report
header. Chrome renders the same SVG the site serves, so they cannot drift.
"""
import os, subprocess, sys, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import brand as B

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
WWW = r"C:\Users\hakan\Documents\Mekanika\wwwroot"
HERE = os.path.dirname(os.path.abspath(__file__))

INK = "#2b3550"
ACC = "#e07a30"

PAGE = """<!doctype html><meta charset="utf-8">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@500;600&display=swap">
<style>
  html,body{{margin:0;padding:0}}
  /* Subpixel (LCD) antialiasing fringes coloured edges onto light text over a
     dark ground, which a share card shows off at full size. Greyscale AA instead. */
  body{{width:{w}px;height:{h}px;overflow:hidden;background:{bg};
        display:flex;flex-direction:column;align-items:center;justify-content:center;
        font-family:Inter,system-ui,sans-serif;
        -webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}}
  svg{{display:block}}
  {extra}
</style>
{body}"""


def shot(name, w, h, bg, body, extra=""):
    src = os.path.join(HERE, "_r_%s.html" % name)
    out = os.path.join(HERE, "_r_%s.png" % name)
    with open(src, "w", encoding="utf-8") as f:
        f.write(PAGE.format(w=w, h=h, bg=bg, body=body, extra=extra))
    # --disable-lcd-text is the flag that actually switches off subpixel
    # antialiasing in headless Chrome on Windows; the CSS property above is
    # ignored there. Without it the share card's caption carries red/blue
    # fringes on every stem, which is very visible on the dark ground.
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                    "--disable-lcd-text", "--force-color-profile=srgb",
                    "--force-device-scale-factor=1",
                    "--virtual-time-budget=4000",
                    "--window-size=%d,%d" % (w, h),
                    "--screenshot=" + out,
                    "file:///" + src.replace("\\", "/")],
                   check=True, capture_output=True)
    return out


# ── 1. OG / Twitter card, 1200x630 ───────────────────────────────────────
# Dark, because a share card competes in a feed and the ink ground is the site's
# own surface. The rule under the subline is the one warm colour, same as the nav.
og_body = (
    '<div class="stack">'
    + B.lockup("blueprint", "hex", "dark", 660)
    + '<p class="sub">ENGINEERING CALCULATIONS &middot; DIN &middot; ISO &middot; EN &middot; VDI</p>'
    + '</div>'
)
og_extra = """
  .stack{display:flex;flex-direction:column;align-items:center;gap:46px}
  .sub{margin:0;font-size:21px;font-weight:600;letter-spacing:.3em;
       color:rgba(255,255,255,.72);text-indent:.3em}
"""
shot("og", 1200, 630, INK, og_body, og_extra)

# ── 2. apple-touch-icon, 180x180 ─────────────────────────────────────────
# iOS draws its own rounded mask over whatever it is given and composites any
# transparency onto black, so the square ships with its own ground.
shot("touch", 180, 180, INK, B.mark("hex", "dark", 124))

# ── 3. logo.png - the jsPDF report header, on white ───────────────────────
# 32mm wide in the PDF with the height taken from this file's aspect, so the
# margin here is the margin in the report.
shot("logo", 1200, 400, "#ffffff",
     B.lockup("blueprint", "hex", "light", 1060))

import shutil
shutil.copy(os.path.join(HERE, "_r_og.png"), os.path.join(WWW, "brand", "og.png"))
shutil.copy(os.path.join(HERE, "_r_touch.png"), os.path.join(WWW, "brand", "apple-touch-icon.png"))
shutil.copy(os.path.join(HERE, "_r_logo.png"), os.path.join(WWW, "logo.png"))
for p in ("brand/og.png", "brand/apple-touch-icon.png", "logo.png"):
    print(p, os.path.getsize(os.path.join(WWW, p.replace("/", os.sep))), "bytes")
