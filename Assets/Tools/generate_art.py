#!/usr/bin/env python3
"""
Teer Puzzle — art assets generator (v1.1)
=========================================
Saare PNG assets dobara banata hai (Pillow only — CI mein `pip install pillow`
ke baad chalta hai). Original assets ki reverse-engineered geometry istemal
ki gayi hai: cube-style arrow tiles, hearts, stars, background, app icon.

Output do jagah likhta hai (dono copies identical):
    Assets/Art/            (Unity import)
    Assets/Resources/Art/  (Resources.Load ke liye)

Usage:
    python3 Assets/Tools/generate_art.py [--out-dir DIR]
    --out-dir dene par sirf us dir mein likhta hai (testing ke liye).
"""
import argparse
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

# ---------------------------------------------------------------- paths
TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(os.path.dirname(TOOLS_DIR))
ART_DIRS = [
    os.path.join(PROJECT_ROOT, "Assets", "Art"),
    os.path.join(PROJECT_ROOT, "Assets", "Resources", "Art"),
]

# ---------------------------------------------------------------- colours
TEAL = (15, 118, 110)
TEAL_TOP = (17, 133, 124)
TEAL_EDGE = (13, 110, 102)
TEAL_DARK = (11, 98, 92)
TEAL_DARK_TOP = (9, 77, 72)
ARROW_DARK = (63, 63, 70)
HEART_RED = (230, 57, 70)
GREY_OUTLINE = (154, 160, 166)
STAR_GOLD = (255, 201, 60)
STAR_GOLD_DARK = (242, 178, 32)
TILE_TOP = (255, 255, 255)
TILE_BOTTOM = (232, 233, 236)

# ---------------------------------------------------------------- helpers

def vgrad(w, h, top, bottom):
    """Vertical gradient (pure PIL): 1xh strip, phir resize."""
    strip = Image.new("RGB", (1, h))
    strip.putdata([
        tuple(int(top[c] + (bottom[c] - top[c]) * y / max(1, h - 1))
              for c in range(3))
        for y in range(h)
    ])
    return strip.resize((w, h))


def save_both(im, name, out_dirs):
    """PNG dono (ya diye gaye) dirs mein likho."""
    for d in out_dirs:
        os.makedirs(d, exist_ok=True)
        im.save(os.path.join(d, name), "PNG")
    print(f"  wrote {name} ({im.size[0]}x{im.size[1]} {im.mode})")


def rounded_top_mask(size, radius, bottom):
    """Mask: upar ke corners rounded (radius), neeche square, y=bottom tak.
    Sahi construction: center column + body rect + 2 top corner circles."""
    w, h = size
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    d.rectangle([radius, 0, w - radius - 1, bottom], fill=255)
    d.rectangle([0, radius, w - 1, bottom], fill=255)
    d.ellipse([0, 0, 2 * radius, 2 * radius], fill=255)
    d.ellipse([w - 2 * radius, 0, w, 2 * radius], fill=255)
    return m


# ---------------------------------------------------------------- arrow tile
# Original tile (256x256): white rounded-top face, dark arrow, neeche soft
# shadow, side "legs" (3D cube effect) aur aik light bottom lip.
# Arrow polygon (measured): apex (128,51), head base y=131 (x 72-184),
# shaft x 105-150, neeche y=205 tak. Head ki slope ~0.70 hai.

ARROW_POLY = [(128, 51), (184, 131), (150, 131), (150, 205),
              (105, 205), (105, 131), (72, 131)]


def draw_arrow_tile_up():
    S = 256
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # 1. Soft shadow (gap ke andar, alpha 18)
    shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ds = ImageDraw.Draw(shadow)
    ds.ellipse([10, 182, 246, 242], fill=(0, 0, 0, 18))
    shadow = shadow.filter(ImageFilter.GaussianBlur(4))
    img.alpha_composite(shadow)

    # 2. Bottom lip (trapezoid — upar chaura, neeche tang, image edge tak)
    lip = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    dl = ImageDraw.Draw(lip)
    dl.polygon([(17, 242), (239, 242), (215, 256), (41, 256)],
               fill=(233, 234, 237, 255))
    # crease line (fold) — halki dark line lip ke upar
    dl.rounded_rectangle([20, 245, 236, 247], radius=1, fill=(197, 200, 207, 255))
    img.alpha_composite(lip)

    # 3. Side legs (cube ki side faces) — vertical gradient, polygon mask.
    #    Inner edge measured: 38@184, 27@188, 22@192, 17@200, 14@216;
    #    outer edge neeche andar ki taraf murta hai (1@216, 7@232, 13@240).
    leg_grad = vgrad(S, S, (200, 203, 209), (226, 228, 232)).convert("RGBA")
    leg_mask = Image.new("L", (S, S), 0)
    dm = ImageDraw.Draw(leg_mask)
    dm.polygon([(0, 184), (38, 184), (27, 188), (22, 192), (17, 200),
                (14, 216), (16, 226), (23, 232), (30, 238), (39, 240),
                (13, 240), (7, 232), (3, 222), (1, 216), (0, 205)], fill=255)
    dm.polygon([(256, 184), (218, 184), (229, 188), (234, 192), (239, 200),
                (242, 216), (240, 226), (233, 232), (226, 238), (217, 240),
                (243, 240), (249, 232), (253, 222), (255, 216), (256, 205)],
               fill=255)
    img.paste(leg_grad, (0, 0), leg_mask)

    # 4. Front face — rounded-top rect (top corners R=42, bottom square),
    #    vertical gradient (poori 256 height ka gradient, face sirf
    #    upar ke 184 px istemal karta hai)
    grad = vgrad(S, S, TILE_TOP, TILE_BOTTOM).convert("RGBA")
    face_mask = rounded_top_mask((S, S), 42, 183)
    face = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    face.paste(grad, (0, 0), face_mask)
    img.alpha_composite(face)

    # 5. Arrow (dark)
    d = ImageDraw.Draw(img)
    d.polygon(ARROW_POLY, fill=ARROW_DARK + (255,))
    return img


def draw_arrow_tiles(out_dirs):
    up = draw_arrow_tile_up()
    save_both(up, "arrow_up.png", out_dirs)
    # Baaki 3 directions original mein up ki EXACT rotations hain
    # (mean abs diff 0.00) — is liye transpose (interpolation nahi).
    save_both(up.transpose(Image.ROTATE_180), "arrow_down.png", out_dirs)
    save_both(up.transpose(Image.ROTATE_90), "arrow_left.png", out_dirs)
    save_both(up.transpose(Image.ROTATE_270), "arrow_right.png", out_dirs)


# ---------------------------------------------------------------- plain tile

def draw_tile(out_dirs):
    S = 128
    grad = vgrad(S, S, (255, 255, 255), (238, 239, 242)).convert("RGBA")
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=25, fill=255)
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    im.paste(grad, (0, 0), mask)
    save_both(im, "tile.png", out_dirs)


# ---------------------------------------------------------------- heart
# Heart contour — original mask se measured 72 radii, centre (128,140),
# har 5 degree par (angle -90 se shuru). Parametric curve se behtar
# match deta hai (fill IoU ~0.95, outline IoU ~0.88).
HEART_RADII_72 = [
    48, 64, 76, 87, 97, 105, 112, 117, 122, 125, 128, 129,
    129, 129, 127, 123, 119, 113, 107, 101, 95, 91, 87, 84,
    81, 79, 77, 77, 77, 77, 77, 79, 83, 86, 91, 99,
    115, 101, 93, 87, 83, 81, 79, 77, 77, 77, 78, 79,
    82, 84, 88, 92, 96, 102, 108, 114, 120, 124, 128, 130,
    131, 130, 128, 126, 123, 117, 112, 105, 96, 87, 75, 60,
]
HEART_CX, HEART_CY = 128, 140


def heart_points():
    """72-point heart polygon (radial table se)."""
    pts = []
    for k, r in enumerate(HEART_RADII_72):
        th = math.radians(k * 5 - 90)
        pts.append((HEART_CX + r * math.cos(th), HEART_CY + r * math.sin(th)))
    return pts


def draw_hearts(out_dirs):
    S = 256
    pts = heart_points()
    # full (red)
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im).polygon(pts, fill=HEART_RED + (255,))
    save_both(im, "heart_full.png", out_dirs)
    # empty (grey outline, stroke ~12 px — measured outline width)
    im2 = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im2).line(pts + [pts[0]], fill=GREY_OUTLINE + (255,),
                             width=12, joint="curve")
    save_both(im2, "heart_empty.png", out_dirs)


# ---------------------------------------------------------------- star
# 5-point star (measured): centre (128,136), outer R=112, inner r=48.
# Gold radial gradient: centre dark (242,178,32) -> edge light (255,201,60).

def star_points(cx=128, cy=136, r_outer=112, r_inner=48):
    pts = []
    for k in range(10):
        ang = math.radians(-90 + k * 36)
        r = r_outer if k % 2 == 0 else r_inner
        pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    return pts


def radial_gold(size=256, cx=128, cy=136, r_max=112):
    """Radial gradient — concentric circles (pure PIL)."""
    im = Image.new("RGB", (size, size), STAR_GOLD)
    d = ImageDraw.Draw(im)
    steps = 52
    for i in range(steps, 0, -1):
        t = i / steps  # 1 = edge, ~0 = centre
        r = r_max * t
        c = tuple(int(STAR_GOLD_DARK[c] + (STAR_GOLD[c] - STAR_GOLD_DARK[c]) * t)
                  for c in range(3))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c)
    return im


def draw_stars(out_dirs):
    S = 256
    pts = star_points()
    # full (gold radial)
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    gold = radial_gold(S).convert("RGBA")
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    im.paste(gold, (0, 0), mask)
    save_both(im, "star_full.png", out_dirs)
    # empty (grey outline, stroke ~10 px — measured)
    im2 = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im2).line(pts + [pts[0]], fill=GREY_OUTLINE + (255,),
                             width=10, joint="curve")
    save_both(im2, "star_empty.png", out_dirs)


# ---------------------------------------------------------------- background

def draw_bg(out_dirs):
    im = vgrad(1080, 1920, (248, 250, 252), (220, 227, 236)).convert("RGB")
    save_both(im, "bg_gradient.png", out_dirs)


# ---------------------------------------------------------------- app icon
# 1024x1024 mini-scene: teal bg, white/light panel (upar), grey panel
# (neeche), bara teal up-arrow, dark teal bottom band, rounded corners R=223.

def draw_icon(out_dirs):
    S = 1024
    # teal bg: vertical gradient + radial vignette (edges ~12% darker)
    bg = vgrad(S, S, TEAL_TOP, (14, 114, 106)).convert("RGB")
    dark = bg.point(lambda v: int(v * 0.88))
    # radial mask (64x64, phir resize): 255 centre -> ~25 corners
    small = Image.new("L", (64, 64), 0)
    sp = small.load()
    for yy in range(64):
        for xx in range(64):
            dx = (xx - 31.5) / 31.5
            dy = (yy - 31.5) / 31.5
            dist = min(1.0, math.sqrt(dx * dx + dy * dy) / math.sqrt(2))
            sp[xx, yy] = int(255 * (1 - 0.9 * dist * dist))
    mask = small.resize((S, S), Image.BICUBIC)
    bg = Image.composite(bg, dark, mask).convert("RGBA")

    # dark teal bottom band (y 830-1023): upar 30 px mein gradient,
    # phir flat (11,98,92)
    bg.paste(Image.new("RGBA", (S, S - 830), TEAL_DARK + (255,)), (0, 830))
    band_top = vgrad(S, 30, TEAL_DARK_TOP, TEAL_DARK).convert("RGBA")
    bg.paste(band_top, (0, 830))

    # grey panel (x 194-829, y 500-829), gradient, bottom corners R=40
    grey = vgrad(S, 330, (239, 240, 243), (234, 236, 239)).convert("RGBA")
    grey_full = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    grey_full.paste(grey, (0, 500))
    gm = Image.new("L", (S, S), 0)
    dg = ImageDraw.Draw(gm)
    dg.rectangle([234, 500, 789, 829], fill=255)
    dg.rectangle([194, 500, 829, 789], fill=255)
    dg.ellipse([194, 749, 274, 829], fill=255)
    dg.ellipse([749, 749, 829, 829], fill=255)
    panel = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    panel.paste(grey_full, (0, 0), gm)
    bg.alpha_composite(panel)

    # dark side triangles (grey panel ke 3D side faces)
    d = ImageDraw.Draw(bg)
    d.polygon([(160, 672), (192, 672), (192, 736), (256, 832), (160, 832)],
              fill=TEAL_DARK + (255,))
    d.polygon([(864, 672), (832, 672), (832, 736), (768, 832), (864, 832)],
              fill=TEAL_DARK + (255,))

    # white/light panel (x 194-829, y 194-505), top corners R=111
    light = vgrad(S, 312, (249, 250, 251), (243, 244, 246)).convert("RGBA")
    light_full = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    light_full.paste(light, (0, 194))
    lm = Image.new("L", (S, S), 0)
    dl = ImageDraw.Draw(lm)
    dl.rectangle([305, 194, 718, 505], fill=255)
    dl.rectangle([194, 305, 829, 505], fill=255)
    dl.ellipse([194, 194, 416, 416], fill=255)
    dl.ellipse([613, 194, 829, 416], fill=255)
    panel2 = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    panel2.paste(light_full, (0, 0), lm)
    bg.alpha_composite(panel2)

    # bara teal up-arrow (apex 512,306 — head base y=540, shaft y=712 tak)
    d = ImageDraw.Draw(bg)
    d.polygon([(512, 306), (665, 540), (573, 540), (573, 712),
               (450, 712), (450, 540), (359, 540)], fill=TEAL + (255,))

    # rounded corners R=223, black bahar
    corner = Image.new("L", (S, S), 0)
    ImageDraw.Draw(corner).rounded_rectangle([0, 0, S - 1, S - 1], radius=223, fill=255)
    out = Image.new("RGB", (S, S), (0, 0, 0))
    out.paste(bg.convert("RGB"), (0, 0), corner)
    save_both(out, "icon_1024.png", out_dirs)


# ---------------------------------------------------------------- adaptive icons

def draw_adaptive(out_dirs):
    S = 432
    bg = Image.new("RGB", (S, S), TEAL)
    save_both(bg, "adaptive_bg.png", out_dirs)
    # white arrow — tile arrow ko 432 par scale kiya (factor 1.6875)
    fg = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    poly = [(216, 86), (311, 221), (253, 221), (253, 346),
            (177, 346), (177, 221), (121, 221)]
    ImageDraw.Draw(fg).polygon(poly, fill=(255, 255, 255, 255))
    save_both(fg, "adaptive_fg.png", out_dirs)


# ---------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser(description="Teer Puzzle art generator")
    ap.add_argument("--out-dir", default=None,
                    help="Sirf is dir mein likho (testing ke liye)")
    args = ap.parse_args()
    out_dirs = [args.out_dir] if args.out_dir else ART_DIRS

    print("Teer Puzzle art generator — output:", ", ".join(out_dirs))
    draw_arrow_tiles(out_dirs)
    draw_tile(out_dirs)
    draw_hearts(out_dirs)
    draw_stars(out_dirs)
    draw_bg(out_dirs)
    draw_icon(out_dirs)
    draw_adaptive(out_dirs)
    print("Done — 13 PNGs x %d dir(s)." % len(out_dirs))


if __name__ == "__main__":
    main()
