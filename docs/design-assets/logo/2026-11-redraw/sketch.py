"""Mekanika logo - a bold redraw of the original technical-drawing sketch.

What changed and why: the original letters are an OUTLINE with fine 45 degree
hatching inside, on thin pen strokes. That is why it goes faint - at sidebar size
the hatch fills in to grey and the letters have no mass left. Here the figure and
ground of the hatch are swapped: the letters are SOLID and the hatch lines are cut
OUT of them in the background colour. Large, you still read a hatched section;
small, the hatch closes up and you are left with a solid bold wordmark instead of
a grey smudge. Everything else (dimension lines, gears, ruler, compass, centre
lines) is kept but drawn on consistent, heavier weights.
"""

INK = "#2b3550"
ACC = "#e07a30"
PAPER = "#ffffff"

CAP = 100.0
S = 20.0          # stem width
GAP = 9.0

# Every letter of MEKANIKA is straight-sided, so each one is a polygon (plus, for
# A, one counter). Coordinates are on a 0..100 cap height, y down.
LETTERS = {
    "M": (98, [[(0,100),(0,0),(20,0),(49,46),(78,0),(98,0),(98,100),(78,100),
                (78,34),(49,78),(20,34),(20,100)]]),
    "E": (60, [[(0,0),(60,0),(60,19),(20,19),(20,40),(52,40),(52,59),(20,59),
                (20,81),(60,81),(60,100),(0,100)]]),
    "K": (72, [[(0,0),(20,0),(20,38),(47,0),(72,0),(34,50),(72,100),(47,100),
                (20,62),(20,100),(0,100)]]),
    "A": (84, [[(0,100),(30,0),(54,0),(84,100),(62,100),(56,80),(28,80),(22,100)],
               [(33.4,62),(42,33.3),(50.6,62)]]),
    "N": (76, [[(0,100),(0,0),(20,0),(56,64),(56,0),(76,0),(76,100),(56,100),
                (20,36),(20,100)]]),
    "I": (20, [[(0,0),(20,0),(20,100),(0,100)]]),
}

TEXT = "MEKANIKA"


def _poly(pts, dx=0.0):
    return "M" + " L".join(f"{x+dx:g} {y:g}" for x, y in pts) + " Z"


def word_path():
    """One path for the whole word; the A counters are reversed-wound holes."""
    d, x = [], 0.0
    for i, ch in enumerate(TEXT):
        w, rings = LETTERS[ch]
        for j, ring in enumerate(rings):
            d.append(_poly(ring if j == 0 else ring[::-1], x))
        x += w + GAP
    return " ".join(d), x - GAP


WORD, WORD_W = word_path()


# ── drawing helpers ──────────────────────────────────────────────────────
def arrow(x, y, dx, dy, L=13.0, W=5.0):
    """Filled arrowhead at (x,y) pointing along (dx,dy). Solid, not the original's
    open V - an open arrowhead at 200px wide is three stray pixels."""
    import math
    n = math.hypot(dx, dy) or 1.0
    ux, uy = dx / n, dy / n
    px, py = -uy, ux
    bx, by = x - ux * L, y - uy * L
    return (f'M{x:g} {y:g} L{bx + px*W:g} {by + py*W:g} '
            f'L{bx - px*W:g} {by - py*W:g} Z')


def dim_h(x0, x1, y, ext_from=None, colour=ACC, sw=3.0, ext=18.0):
    """Horizontal dimension line with extension lines and filled arrowheads."""
    e = ""
    if ext_from is not None:
        e = (f'<path d="M{x0:g} {ext_from:g} V{y + (ext if y > ext_from else -ext):g} '
             f'M{x1:g} {ext_from:g} V{y + (ext if y > ext_from else -ext):g}" '
             f'stroke="{colour}" stroke-width="{sw*0.7:g}" fill="none"/>')
    return (f'{e}<path d="M{x0:g} {y:g} H{x1:g}" stroke="{colour}" stroke-width="{sw:g}" fill="none"/>'
            f'<path d="{arrow(x0, y, -1, 0)}{arrow(x1, y, 1, 0)}" fill="{colour}"/>')


def dim_v(y0, y1, x, colour=ACC, sw=3.0):
    return (f'<path d="M{x:g} {y0:g} V{y1:g}" stroke="{colour}" stroke-width="{sw:g}" fill="none"/>'
            f'<path d="{arrow(x, y0, 0, -1)}{arrow(x, y1, 0, 1)}" fill="{colour}"/>')


def gear(cx, cy, r_tip, teeth=16, colour=INK, sw=4.0, hub=True, centre=True, halo=None):
    """Spur gear outline: alternating tip and root arcs with flat tooth tops."""
    import math
    r_root = r_tip * 0.80
    pts = []
    step = math.pi / teeth           # half a tooth
    half = step * 0.40               # tooth top half-angle
    for k in range(teeth):
        a = 2 * step * k
        for ang, r in ((a - half, r_tip), (a + half, r_tip),
                       (a + step - half * 0.9, r_root), (a + step + half * 0.9, r_root)):
            pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    d = "M" + " L".join(f"{x:.2f} {y:.2f}" for x, y in pts) + " Z"
    out = ""
    if halo:
        out += f'<circle cx="{cx:g}" cy="{cy:g}" r="{r_tip*1.26:g}" fill="{halo}"/>'
    out += f'<path d="{d}" fill="none" stroke="{colour}" stroke-width="{sw:g}" stroke-linejoin="round"/>'
    if hub:
        out += (f'<circle cx="{cx:g}" cy="{cy:g}" r="{r_tip*0.46:g}" fill="none" '
                f'stroke="{colour}" stroke-width="{sw*0.85:g}"/>')
    if centre:
        # ISO centre line: long-dash / short-dash through the hub.
        L = r_tip * 1.22
        out += (f'<path d="M{cx-L:g} {cy:g} H{cx+L:g} M{cx:g} {cy-L:g} V{cy+L:g}" '
                f'fill="none" stroke="{colour}" stroke-width="{sw*0.55:g}" '
                f'stroke-dasharray="{r_tip*0.5:g} {r_tip*0.14:g} {r_tip*0.14:g} {r_tip*0.14:g}"/>')
    return out


def ruler(x, y, w, h=20.0, colour=INK, sw=3.0, n=14, halo=None):
    ticks = "".join(
        f'M{x + w*(k+1)/(n+1):g} {y:g} V{y + (h*0.62 if k % 2 else h*0.38):g}'
        for k in range(n))
    return ((f'<rect x="{x-6:g}" y="{y-6:g}" width="{w+12:g}" height="{h+12:g}" fill="{halo}"/>' if halo else '')
            + f'<rect x="{x:g}" y="{y:g}" width="{w:g}" height="{h:g}" rx="2" fill="{halo or "none"}" '
            f'stroke="{colour}" stroke-width="{sw:g}"/>'
            + f'<path d="{ticks}" stroke="{colour}" stroke-width="{sw*0.6:g}" fill="none"/>')


def compass(cx, cy, h=86.0, spread=30.0, colour=INK, sw=4.0, halo=None):
    """Drafting compass: head, hinge, two legs, pencil tip."""
    top = cy - h * 0.5
    bot = cy + h * 0.5
    return ((f'<rect x="{cx-spread/2-14:g}" y="{top-14:g}" width="{spread+28:g}" height="{h+24:g}" fill="{halo}"/>' if halo else '')
            + f'<circle cx="{cx:g}" cy="{top+10:g}" r="10" fill="{halo or "none"}" stroke="{colour}" stroke-width="{sw:g}"/>'
            f'<path d="M{cx:g} {top-9:g} V{top:g}" stroke="{colour}" stroke-width="{sw*1.3:g}" fill="none"/>'
            f'<path d="M{cx:g} {top+20:g} V{top+28:g}" stroke="{colour}" stroke-width="{sw:g}" fill="none"/>'
            f'<path d="M{cx:g} {top+28:g} L{cx-spread/2:g} {bot:g} M{cx:g} {top+28:g} L{cx+spread/2:g} {bot:g}" '
            f'stroke="{colour}" stroke-width="{sw:g}" fill="none" stroke-linecap="round"/>'
            f'<path d="M{cx-spread/2-3:g} {bot-12:g} L{cx-spread/2:g} {bot+6:g} L{cx-spread/2+3:g} {bot-12:g} Z" fill="{colour}"/>')


def arc_arrow(cx, cy, r, a0, a1, colour=INK, sw=3.2):
    """Rotation arrow: an arc with a filled head at the far end."""
    import math
    x0, y0 = cx + r*math.cos(a0), cy + r*math.sin(a0)
    x1, y1 = cx + r*math.cos(a1), cy + r*math.sin(a1)
    large = 1 if abs(a1 - a0) > math.pi else 0
    sweep = 1 if a1 > a0 else 0
    tx, ty = -math.sin(a1), math.cos(a1)
    if a1 < a0:
        tx, ty = -tx, -ty
    return (f'<path d="M{x0:.2f} {y0:.2f} A{r:g} {r:g} 0 {large} {sweep} {x1:.2f} {y1:.2f}" '
            f'fill="none" stroke="{colour}" stroke-width="{sw:g}"/>'
            f'<path d="{arrow(x1, y1, tx, ty, 12, 4.6)}" fill="{colour}"/>')
