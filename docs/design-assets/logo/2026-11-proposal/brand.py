# Mekanika brand assets. Everything is geometry - no font, no raster, no CSS
# filter. MEKANIKA happens to contain no curved letter, so the wordmark is drawn
# from straight strokes on the same grid language as the UI icon set.
INK = "#2b3550"
ACC = "#e07a30"
WHITE = "#ffffff"

CAP = 24.0
SW  = 3.3
GAP = 5.5
KERN = {"EK": -1.0, "KA": -2.6, "AN": -2.4, "NI": -0.4, "IK": -0.4}

LETTERS = {
    "M": (20, ["M0 24 V0 L10 12.5 L20 0 V24"]),
    "E": (16, ["M16 0 H0 V24 H16", "M0 12 H13"]),
    "K": (17, ["M0 0 V24", "M16.5 0 L2 12 L16.5 24"]),
    "A": (20, ["M0 24 L10 0 L20 24", "M4.4 14.4 H15.6"]),
    "N": (18, ["M0 24 V0 L18 24 V0"]),
    "I": (0,  ["M0 0 V24"]),
}

TEXT = "MEKANIKA"


def word_paths():
    x = 0.0
    out = []
    for i, ch in enumerate(TEXT):
        w, paths = LETTERS[ch]
        for p in paths:
            out.append(f'<path transform="translate({x:g} 0)" d="{p}"/>')
        x += w + GAP
        if i + 1 < len(TEXT):
            x += KERN.get(TEXT[i] + TEXT[i + 1], 0.0)
    return "".join(out), x - GAP - KERN.get(TEXT[-2] + TEXT[-1], 0.0)


WORD, WORD_W = word_paths()


def dim(total, y, colour, sw=1.5, a=3.4, ext=True):
    e = f'<path d="M0 {y-5} V{y+3.5} M{total} {y-5} V{y+3.5}"/>' if ext else ""
    return (f'<g stroke="{colour}" stroke-width="{sw}" fill="none" stroke-linecap="round" stroke-linejoin="round">'
            f'{e}<path d="M0 {y} H{total}"/>'
            f'<path d="M{a} {y-2.1} L0 {y} L{a} {y+2.1}"/>'
            f'<path d="M{total-a} {y-2.1} L{total} {y} L{total-a} {y+2.1}"/></g>')


def dim_chain(total, y, colour, sw=1.2):
    """Two-segment dimension chain above the word, with the station tick."""
    mid = total * 0.5
    a = 2.8
    arrows = "".join(
        f'<path d="M{x0+a} {y-1.8} L{x0} {y} L{x0+a} {y+1.8}"/><path d="M{x1-a} {y-1.8} L{x1} {y} L{x1-a} {y+1.8}"/>'
        for x0, x1 in ((0, mid), (mid, total)))
    return (f'<g stroke="{colour}" stroke-width="{sw}" fill="none" stroke-linecap="round" stroke-linejoin="round" opacity="0.75">'
            f'<path d="M0 {y-2.5} V{y+6} M{mid} {y-2.5} V{y+6} M{total} {y-2.5} V{y+6}"/>'
            f'<path d="M0 {y} H{total}"/>{arrows}</g>')


# ── marks ────────────────────────────────────────────────────────────────
M_IN = "M6.8 16.6 V6.4 L12 11.4 L17.2 6.4 V16.6"
HEX  = "M12 1.2 L21.4 6.6 V17.4 L12 22.8 L2.6 17.4 V6.6 Z"
# A short centred tick under the M, not a rule spanning its full width. The first
# version ran the whole width of the M at stroke 1.8 and its round caps merged into
# both stems: at 96px the M stopped being a letter and became a cup with an orange
# floor. Narrow and detached, it reads as what it is - the same dimension mark the
# wordmark carries. Defined once here; favicon.svg draws the same path.
BAR  = "M9.4 19.2 H14.6"
BAR_W = 1.5


def mark(kind="hex", on="light", size=None):
    """on: light|dark - which background the mark will sit on."""
    plate = INK if on == "light" else WHITE
    knock = WHITE if on == "light" else INK
    if kind == "hex":
        body = (f'<path d="{HEX}" fill="{plate}"/>'
                f'<path d="{M_IN}" fill="none" stroke="{knock}" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"/>'
                f'<path d="{BAR}" stroke="{ACC}" stroke-width="{BAR_W}" stroke-linecap="round"/>')
    elif kind == "hex-accent":
        body = (f'<path d="{HEX}" fill="{ACC}"/>'
                f'<path d="{M_IN}" fill="none" stroke="{INK}" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/>')
    elif kind == "gear":
        teeth = "".join(
            f'<rect x="11.05" y="0.3" width="1.9" height="3.6" rx="0.55" fill="{plate}" transform="rotate({a} 12 12)"/>'
            for a in range(0, 360, 30))
        body = (f'{teeth}<circle cx="12" cy="12" r="9.7" fill="{plate}"/>'
                f'<path d="{M_IN}" fill="none" stroke="{knock}" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"/>'
                f'<path d="M8.6 19.2 H15.4" stroke="{ACC}" stroke-width="1.7" stroke-linecap="round"/>')
    else:
        raise ValueError(kind)
    sz = f' width="{size}" height="{size}"' if size else ""
    return f'<svg{sz} viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">{body}</svg>'


def _mark_inner(kind, on):
    return mark(kind, on).split(">", 1)[1].rsplit("</svg>", 1)[0]


# ── lockups ──────────────────────────────────────────────────────────────
def wordmark(style="clean", on="light", width=None, accent=ACC):
    """style: clean (one dimension line) | blueprint (chain above + line below) | plain"""
    colour = INK if on == "light" else WHITE
    top, bottom, y0, h = "", "", 0.0, 27.0
    if style == "clean":
        bottom = dim(WORD_W, 35.0, accent)
        h = 41.0
    elif style == "blueprint":
        top = dim_chain(WORD_W, -11.0, accent)
        bottom = dim(WORD_W, 35.0, accent)
        y0 = -16.0
        h = 57.0
    vb = f'{-2} {y0-2} {WORD_W+4} {h+4}'
    wh = ""
    if width:
        wh = f' width="{width}" height="{width*(h+4)/(WORD_W+4):.2f}"'
    return (f'<svg{wh} viewBox="{vb}" fill="none" xmlns="http://www.w3.org/2000/svg">'
            f'{top}<g stroke="{colour}" stroke-width="{SW}" stroke-linecap="round" stroke-linejoin="round">{WORD}</g>'
            f'{bottom}</svg>')


def lockup(style="clean", markkind="hex", on="light", width=None):
    colour = INK if on == "light" else WHITE
    top, bottom, y0, h = "", "", 0.0, 27.0
    if style == "clean":
        bottom = dim(WORD_W, 35.0, ACC); h = 41.0
    elif style == "blueprint":
        top = dim_chain(WORD_W, -11.0, ACC); bottom = dim(WORD_W, 35.0, ACC); y0 = -16.0; h = 57.0

    # The mark is sized against the WHOLE text block, not against the cap height,
    # and centred on it. Pinned to the cap height it came out at 26 units beside a
    # 57-unit blueprint block: the hexagon looked like an afterthought floating at
    # the word's x-height. 0.70 keeps it clearly subordinate to the word while
    # still reading as part of the same object.
    mk = round(0.70 * h, 2)
    gap = round(0.35 * mk, 2)
    mk_y = y0 + (h - mk) / 2.0
    vw = mk + gap + WORD_W
    vb = f'{-2} {y0-2} {vw+4} {h+4}'
    wh = f' width="{width}" height="{width*(h+4)/(vw+4):.2f}"' if width else ""
    return (f'<svg{wh} viewBox="{vb}" fill="none" xmlns="http://www.w3.org/2000/svg">'
            f'<g transform="translate(0 {mk_y:g}) scale({mk/24.0:.5f})">{_mark_inner(markkind, on)}</g>'
            f'<g transform="translate({mk+gap:g} 0)">{top}'
            f'<g stroke="{colour}" stroke-width="{SW}" stroke-linecap="round" stroke-linejoin="round">{WORD}</g>'
            f'{bottom}</g></svg>')
