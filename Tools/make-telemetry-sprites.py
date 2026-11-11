"""Builds the Telemetry UI sprites into Assets/_UI/Telemetry/Sprites.

Everything is drawn at 2x (the importer sets 200 pixels per unit, so one UI unit is two pixels) and
supersampled 4x for smooth edges. Colours are baked where a sprite is always the same (glass panels,
touch buttons); plain white sprites are tinted by the Image colour.

Run from the project root:  python Tools/make-telemetry-sprites.py
Needs Pillow; the medal digits use the IBM Plex Mono font from Assets/_UI/Telemetry/Fonts.
"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_UI", "Telemetry", "Sprites")
FONTS = os.path.join(ROOT, "Assets", "_UI", "Telemetry", "Fonts")
SS = 4  # supersampling

BG = (9, 13, 18)
GLASS = (12, 17, 24, 199)
LINE = (234, 240, 246, 41)
LIME = (198, 244, 50)
AMB = (255, 181, 71)
TX = (234, 240, 246)


def save(img, name):
    img.save(os.path.join(OUT, name), optimize=True)


def rounded(size, radius, fill, border=None, width=0):
    """A rounded rectangle with an optional inside border, drawn supersampled."""
    w, h = size
    big = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    box = (0, 0, w * SS - 1, h * SS - 1)
    if fill is not None:
        d.rounded_rectangle(box, radius * SS, fill=fill)
    img = big.resize((w, h), Image.LANCZOS)
    if border is not None and width > 0:
        ring = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        r = ImageDraw.Draw(ring)
        r.rounded_rectangle(box, radius * SS, fill=border)
        inner = (width * SS, width * SS, w * SS - 1 - width * SS, h * SS - 1 - width * SS)
        r.rounded_rectangle(inner, max(0, (radius - width)) * SS, fill=(0, 0, 0, 0))
        img = Image.alpha_composite(img, ring.resize((w, h), Image.LANCZOS))
    return img


def circle(d, fill, border=None, width=0):
    big = Image.new("RGBA", (d * SS, d * SS), (0, 0, 0, 0))
    g = ImageDraw.Draw(big)
    g.ellipse((0, 0, d * SS - 1, d * SS - 1), fill=fill)
    img = big.resize((d, d), Image.LANCZOS)
    if border is not None:
        ring = Image.new("RGBA", (d * SS, d * SS), (0, 0, 0, 0))
        r = ImageDraw.Draw(ring)
        r.ellipse((0, 0, d * SS - 1, d * SS - 1), fill=border)
        r.ellipse((width * SS, width * SS, d * SS - 1 - width * SS, d * SS - 1 - width * SS), fill=(0, 0, 0, 0))
        img = Image.alpha_composite(img, ring.resize((d, d), Image.LANCZOS))
    return img


def panels():
    save(rounded((48, 48), 8, GLASS, LINE, 2), "glass.png")
    save(rounded((48, 48), 8, (255, 255, 255, 15), LINE, 2), "row.png")
    save(rounded((48, 48), 8, LIME + (22,), LIME + (255,), 2), "row_sel.png")
    save(rounded((48, 48), 6, (255, 255, 255, 255)), "solid.png")
    save(rounded((48, 48), 6, None, (255, 255, 255, 255), 2), "outline.png")
    save(rounded((48, 48), 6, AMB + (22,), AMB + (255,), 2), "amber.png")
    save(rounded((48, 48), 6, None, (234, 240, 246, 170), 2), "keycap.png")


def touch():
    save(circle(160, BG + (95,), TX + (140,), 3), "touch.png")
    save(circle(160, LIME + (32,), LIME + (255,), 3), "touch_gas.png")
    save(circle(160, AMB + (28,), AMB + (255,), 3), "touch_amber.png")
    save(circle(128, (255, 255, 255, 255)), "circle.png")
    save(circle(128, None, (255, 255, 255, 255), 4), "ring.png")


def bracket():
    s = 48 * SS
    big = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    t = 4 * SS
    d.rectangle((0, 0, s - 1, t - 1), fill=LIME + (255,))
    d.rectangle((0, 0, t - 1, s - 1), fill=LIME + (255,))
    save(big.resize((48, 48), Image.LANCZOS), "bracket.png")


def gradient(name, size, stops, horizontal):
    """stops: list of (position 0..1, alpha 0..1) in BG colour."""
    w, h = size
    img = Image.new("RGBA", size)
    px = img.load()
    n = w if horizontal else h
    for i in range(n):
        t = i / (n - 1)
        a = stops[0][1]
        for (p0, a0), (p1, a1) in zip(stops, stops[1:]):
            if p0 <= t <= p1:
                a = a0 + (a1 - a0) * ((t - p0) / (p1 - p0) if p1 > p0 else 0)
                break
        else:
            a = stops[-1][1]
        for j in range(h if horizontal else w):
            if horizontal:
                px[i, j] = BG + (round(a * 255),)
            else:
                px[j, h - 1 - i] = BG + (round(a * 255),)  # bottom-up, like Unity's UV
    save(img, name)


def scrims():
    # Garage, landscape: dark behind the left rail, clear over the car, a little on the right.
    gradient("scrim_garage_h.png", (256, 4), [(0, .92), (.30, .55), (.55, 0), (.82, 0), (1, .35)], True)
    # Garage, portrait (bottom-up): menu at the bottom, car in the middle, logo at the top.
    gradient("scrim_garage_v.png", (4, 256), [(0, .94), (.32, .94), (.44, 0), (.78, 0), (1, .65)], False)
    # Race HUD (bottom-up): a little dark at the bottom and top edges for the readouts.
    gradient("scrim_hud_v.png", (4, 256), [(0, .5), (.28, 0), (.78, 0), (1, .35)], False)
    # Results, landscape: dark on the left (the big place number), lighter in the middle.
    gradient("scrim_results_h.png", (256, 4), [(0, .94), (.5, .75), (1, .9)], True)


# --- icons: 24-unit grid, strokes 2 units, drawn on a 256 canvas and scaled to 64 ---------------
G = 256 / 24.0


def P(x, y):
    return (x * G, y * G)


def stroke(d, pts, w=2.2, closed=False):
    pts = [P(*p) for p in pts]
    if closed:
        pts = pts + [pts[0]]
    width = round(w * G)
    d.line(pts, fill=(255, 255, 255, 255), width=width, joint="curve")
    r = width / 2
    for x, y in pts:
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, 255))


def icon(name, draw):
    big = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    draw(d)
    save(big.resize((64, 64), Image.LANCZOS), "icon_" + name + ".png")


def arc_pts(cx, cy, r, a0, a1, n=40):
    import math
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def icons():
    icon("chev_l", lambda d: stroke(d, [(15, 18), (9, 12), (15, 6)], 2.4))
    icon("chev_r", lambda d: stroke(d, [(9, 18), (15, 12), (9, 6)], 2.4))
    icon("chev_u", lambda d: stroke(d, [(6, 15), (12, 9), (18, 15)], 2.4))
    icon("chev_d", lambda d: stroke(d, [(6, 9), (12, 15), (18, 9)], 2.4))

    def pause(d):
        stroke(d, [(9, 5), (9, 19)], 2.6)
        stroke(d, [(15, 5), (15, 19)], 2.6)
    icon("pause", pause)

    def camera(d):
        stroke(d, [(3, 7.5), (15, 7.5), (15, 16.5), (3, 16.5)], 2, closed=True)
        stroke(d, [(15, 10.5), (21, 7.5), (21, 16.5), (15, 13.5)], 2)
    icon("camera", camera)

    def bolt(d):
        d.polygon([P(13, 2), P(4.5, 13.5), P(11, 13.5), P(10, 22), P(18.5, 10.5), P(12, 10.5)], fill=(255, 255, 255, 255))
    icon("bolt", bolt)

    def play(d):
        d.polygon([P(7, 4.5), P(7, 19.5), P(19.5, 12)], fill=(255, 255, 255, 255))
    icon("play", play)

    def restart(d):
        stroke(d, arc_pts(12, 12, 8.5, 200, 520), 2)
        stroke(d, [(3.5, 3.5), (3.5, 8.5), (8.5, 8.5)], 2)
    icon("restart", restart)

    def home(d):
        stroke(d, [(3, 11.5), (12, 4), (21, 11.5)], 2)
        stroke(d, [(5.5, 10), (5.5, 20), (18.5, 20), (18.5, 10)], 2)
        stroke(d, [(9.5, 20), (9.5, 15), (14.5, 15), (14.5, 20)], 2)
    icon("home", home)

    def sliders(d):
        for y, a, b in ((6, 13, 17), (12, 7, 11), (18, 15, 19)):
            stroke(d, [(4, y), (a, y)], 2)
            stroke(d, [(b, y), (20, y)], 2)
            cx = (a + b) / 2
            r = 2.2 * G
            x, yy = P(cx, y)
            d.ellipse((x - r, yy - r, x + r, yy + r), outline=(255, 255, 255, 255), width=round(1.9 * G))
    icon("sliders", sliders)

    def lock(d):
        stroke(d, [(5, 11), (19, 11), (19, 21), (5, 21)], 2, closed=True)
        stroke(d, [(8, 11)] + arc_pts(12, 7.5, 4, 180, 360) + [(16, 11)], 2)
    icon("lock", lock)

    icon("check", lambda d: stroke(d, [(5, 12.5), (9.5, 17), (19, 7)], 2.6))

    def tv(d):
        stroke(d, [(3, 5), (21, 5), (21, 18), (3, 18)], 2, closed=True)
        d.polygon([P(10, 8.5), P(15.5, 11.5), P(10, 14.5)], fill=(255, 255, 255, 255))
    icon("tv", tv)

    def keyboard(d):
        stroke(d, [(2.5, 6), (21.5, 6), (21.5, 18), (2.5, 18)], 2, closed=True)
        stroke(d, [(7, 14), (17, 14)], 2)
        for x in (6.5, 10.5, 14.5, 18):
            stroke(d, [(x, 10), (x, 10)], 2.2)
    icon("keyboard", keyboard)


def medals():
    font = ImageFont.truetype(os.path.join(FONTS, "IBMPlexMono-Bold.ttf"), 40 * SS)
    for name, col, digit in (("gold", (232, 194, 74), "1"), ("silver", (201, 209, 220), "2"),
                             ("bronze", (208, 138, 78), "3")):
        s = 64 * SS
        big = Image.new("RGBA", (s, s), (0, 0, 0, 0))
        d = ImageDraw.Draw(big)
        d.ellipse((0, 0, s - 1, s - 1), fill=col + (255,))
        d.text((s / 2, s / 2 + 2 * SS), digit, font=font, fill=(11, 15, 20, 255), anchor="mm")
        save(big.resize((64, 64), Image.LANCZOS), "medal_" + name + ".png")
    save(circle(64, None, (152, 164, 181, 160), 3), "medal_none.png")


def art():
    logo = Image.open(os.path.join(ROOT, "Art", "Covers", "logo.png")).convert("RGBA")
    w = 640
    logo = logo.resize((w, round(logo.height * w / logo.width)), Image.LANCZOS)
    save(logo, "logo.png")
    raw = os.path.join(ROOT, "Builds", "covers", "raw", "wide2.png")
    if os.path.exists(raw):
        track = Image.open(raw).convert("RGB").resize((960, 540), Image.LANCZOS)
        track.save(os.path.join(OUT, "track_village.jpg"), quality=82, optimize=True, progressive=False)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    panels()
    touch()
    bracket()
    scrims()
    icons()
    medals()
    art()
    print("sprites in", OUT)
