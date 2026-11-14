import math, sketch as K

HATCH_PITCH = 12.0
HATCH_CUT = 3.0

def hatch_def(uid, ink, paper, pitch=None, w=None):
    pitch = HATCH_PITCH if pitch is None else pitch
    w = HATCH_CUT if w is None else w
    return (f'<pattern id="h{uid}" width="{pitch:g}" height="{pitch:g}" '
            f'patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
            f'<rect width="{pitch:g}" height="{pitch:g}" fill="{ink}"/>'
            f'<rect width="{w:g}" height="{pitch:g}" fill="{paper}"/></pattern>')

def wordmark(uid, ink, paper, hatched=True, outline=5.0):
    fill = f'url(#h{uid})' if hatched else ink
    return (f'<path d="{K.WORD}" fill="{fill}" fill-rule="evenodd" '
            f'stroke="{ink}" stroke-width="{outline:g}" stroke-linejoin="round"/>')

def full(uid, ink, paper, acc, width=None, hatched=True):
    W = K.WORD_W
    dims = []
    dims.append(K.dim_h(0, W, -126, ext_from=-8, colour=acc, sw=3.4))
    dims.append(K.dim_h(109, 290, 152, ext_from=106, colour=acc, sw=3.4))
    dims.append(K.dim_v(0, 100, W + 50, colour=acc, sw=3.4))
    dims.append(f'<path d="M{W+12:g} 0 H{W+64:g} M{W+12:g} 100 H{W+64:g}" stroke="{acc}" '
                f'stroke-width="2.4" fill="none"/>')
    ann = []
    ann.append(K.arc_arrow(16, 2, 74, math.radians(58), math.radians(152), ink, 3.4))
    ann.append(K.arc_arrow(236, 128, 66, math.radians(152), math.radians(44), ink, 3.4))
    ann.append(K.arc_arrow(566, -44, 64, math.radians(206), math.radians(-26), ink, 3.2))
    ann.append(K.gear(16, 2, 46, 16, ink, 4.2, halo=paper))
    ann.append(K.gear(236, 128, 42, 14, ink, 4.0, halo=paper))
    ann.append(K.ruler(374, -58, 108, 20, ink, 3.0, halo=paper))
    ann.append(K.compass(566, -62, 86, 32, ink, 4.0, halo=paper))
    x0, y0, w, h = -82, -156, W + 172, 356
    wh = f' width="{width}" height="{width*h/w:.2f}"' if width else ""
    return (f'<svg{wh} viewBox="{x0:g} {y0:g} {w:g} {h:g}" xmlns="http://www.w3.org/2000/svg">'
            f'<defs>{hatch_def(uid, ink, paper)}</defs>'
            f'<g>{"".join(dims)}</g><g>{"".join(ann)}</g>'
            f'{wordmark(uid, ink, paper, hatched)}</svg>')

def compact(uid, ink, paper, acc, width=None, hatched=True):
    W = K.WORD_W
    dims = K.dim_h(0, W, -40, ext_from=-6, colour=acc, sw=3.6, ext=12) + \
           K.dim_h(0, W, 140, ext_from=106, colour=acc, sw=3.6, ext=12)
    x0, y0, w, h = -10, -62, W + 20, 224
    wh = f' width="{width}" height="{width*h/w:.2f}"' if width else ""
    return (f'<svg{wh} viewBox="{x0:g} {y0:g} {w:g} {h:g}" xmlns="http://www.w3.org/2000/svg">'
            f'<defs>{hatch_def(uid, ink, paper)}</defs>'
            f'{wordmark(uid, ink, paper, hatched)}<g>{dims}</g></svg>')

def plain(uid, ink, paper, width=None, hatched=True):
    W = K.WORD_W
    x0, y0, w, h = -6, -6, W + 12, 112
    wh = f' width="{width}" height="{width*h/w:.2f}"' if width else ""
    return (f'<svg{wh} viewBox="{x0:g} {y0:g} {w:g} {h:g}" xmlns="http://www.w3.org/2000/svg">'
            f'<defs>{hatch_def(uid, ink, paper)}</defs>{wordmark(uid, ink, paper, hatched)}</svg>')


def mark(uid, plate, knock, acc, size=None, style="square", tick=True):
    """Square mark: the wordmark's own M on a plate, hatched the same way.

    The M is the one letter that carries the whole wordmark's character, so the
    icon is literally the first glyph rather than a new drawing - at 16px the
    hatch closes up and it reads as a solid M, which is the point.
    """
    import sketch as K
    w, rings = K.LETTERS["M"]
    sc = 0.56
    tx = 50 - (w * sc) / 2.0
    ty = 50 - (100 * sc) / 2.0 - (3 if tick else 0)
    if style == "circle":
        shape = '<circle cx="50" cy="50" r="48"/>'
    else:
        shape = '<rect x="2" y="2" width="96" height="96" rx="20"/>'
    t = (f'<path d="M34 86 H66" stroke="{acc}" stroke-width="6" stroke-linecap="round"/>'
         if tick else "")
    wh = f' width="{size}" height="{size}"' if size else ""
    return (f'<svg{wh} viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
            f'<defs>{hatch_def(uid, knock, plate, 11.0, 3.0)}</defs>'
            f'<g fill="{plate}">{shape}</g>'
            f'<g transform="translate({tx:.2f} {ty:.2f}) scale({sc:g})">'
            f'<path d="{_poly_m()}" fill="url(#h{uid})" stroke="{knock}" '
            f'stroke-width="{5.0/sc:.2f}" stroke-linejoin="round"/></g>{t}</svg>')


def _poly_m():
    import sketch as K
    return "M" + " L".join(f"{x:g} {y:g}" for x, y in K.LETTERS["M"][1][0]) + " Z"
