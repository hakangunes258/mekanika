"""The three rasters, rendered from the same SVG geometry with headless Chrome.

--disable-lcd-text is required: the CSS -webkit-font-smoothing property is ignored
in headless Chrome on Windows, and subpixel antialiasing fringes light text on the
dark share card in red and blue.
"""
import os, subprocess, sys, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sketch as K
import build as B

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
WWW = r"C:\Users\hakan\Documents\Mekanika\wwwroot"
HERE = os.path.dirname(os.path.abspath(__file__))
INK, ACC = K.INK, K.ACC

PAGE = """<!doctype html><meta charset="utf-8">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@500;600&display=swap">
<style>
  html,body{{margin:0;padding:0}}
  body{{width:{w}px;height:{h}px;overflow:hidden;background:{bg};
        display:flex;flex-direction:column;align-items:center;justify-content:center;
        font-family:Inter,system-ui,sans-serif}}
  svg{{display:block}}
  {extra}
</style>
{body}"""


def shot(name, w, h, bg, body, extra=""):
    src = os.path.join(HERE, "_r2_%s.html" % name)
    out = os.path.join(HERE, "_r2_%s.png" % name)
    with open(src, "w", encoding="utf-8") as f:
        f.write(PAGE.format(w=w, h=h, bg=bg, body=body, extra=extra))
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                    "--disable-lcd-text", "--force-color-profile=srgb",
                    "--force-device-scale-factor=1", "--virtual-time-budget=4000",
                    "--window-size=%d,%d" % (w, h), "--screenshot=" + out,
                    "file:///" + src.replace("\\", "/")],
                   check=True, capture_output=True)
    return out


# ── share card, 1200x630: the full drawing, which is what it is for ───────
shot("og", 1200, 630, INK,
     '<div class="stack">' + B.full("og", "#ffffff", INK, ACC, 880)
     + '<p class="sub">ENGINEERING CALCULATIONS &middot; DIN &middot; ISO &middot; EN &middot; VDI</p></div>',
     """.stack{display:flex;flex-direction:column;align-items:center;gap:18px}
        .sub{margin:0;font-size:20px;font-weight:600;letter-spacing:.3em;
             color:rgba(255,255,255,.72);text-indent:.3em}""")

# ── iOS icon, 180x180: the mark without its plate, since iOS supplies the
#    ground and then masks the corners itself ───────────────────────────────
m = B._poly_m()
touch = ('<svg width="124" height="124" viewBox="0 0 100 100">'
         + '<defs>' + B.hatch_def("tch", "#ffffff", 11.0, 3.0) + '</defs>'
         + '<g transform="translate(22.56 19.00) scale(0.56)">'
         + f'<path d="{m}" fill="url(#htch)" stroke="#ffffff" stroke-width="8.93" stroke-linejoin="round"/></g>'
         + f'<path d="M34 86 H66" stroke="{ACC}" stroke-width="6" stroke-linecap="round"/></svg>')
shot("touch", 180, 180, INK, touch)

# ── logo.png: the PDF report header, on white. jsPDF places it 32mm wide and
#    takes the height from this file's aspect, so the margin here is the
#    margin in the report ────────────────────────────────────────────────────
shot("logo", 1200, 430, "#ffffff", B.compact("pdf", INK, "#ffffff", ACC, 1060))

shutil.copy(os.path.join(HERE, "_r2_og.png"), os.path.join(WWW, "brand", "og.png"))
shutil.copy(os.path.join(HERE, "_r2_touch.png"), os.path.join(WWW, "brand", "apple-touch-icon.png"))
shutil.copy(os.path.join(HERE, "_r2_logo.png"), os.path.join(WWW, "logo.png"))
for p in ("brand/og.png", "brand/apple-touch-icon.png", "logo.png"):
    print(p, os.path.getsize(os.path.join(WWW, p.replace("/", os.sep))), "bytes")
