r"""Builds the McPlay / YouTube Playables covers and the loading art for MicroCar Racing.

Input:  in-game renders in Builds/covers/raw (made in the Unity editor with a camera clone of the
        race camera, post-processing on, at 2x size): wide3.png (3840x2160), sq1.png (2160x2160),
        tall4.png (2160x3024).
Output: Art/Covers/cover_16x9.jpg (1920x1080), cover_1x1.jpg (1080x1080), cover_5x7.jpg (1080x1512),
        logo.png (transparent), and Assets/WebGLTemplates/YouTube/loading.jpg (640x640).

The picture is the game itself (no paint-over), only graded a little; the title is drawn here.
Fonts: Impact and Arial Black from Windows.

    python Tools/make-covers.py
"""

import os
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, 'Builds', 'covers', 'raw')
OUT = os.path.join(ROOT, 'Art', 'Covers')
LOADING = os.path.join(ROOT, 'Assets', 'WebGLTemplates', 'YouTube', 'loading.jpg')
FONTS = r'C:\Windows\Fonts'

NAVY = (14, 27, 46)


def gradient(size, stops):
    """Vertical gradient through (position 0..1, rgb) stops."""
    w, h = size
    img = Image.new('RGB', (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        for (p0, c0), (p1, c1) in zip(stops, stops[1:]):
            if p0 <= t <= p1:
                k = (t - p0) / max(1e-6, p1 - p0)
                img.putpixel((0, y), tuple(int(c0[i] + (c1[i] - c0[i]) * k) for i in range(3)))
                break
    return img.resize((w, h))


def text_mask(text, font, pad):
    l, t, r, b = font.getbbox(text)
    mask = Image.new('L', (r - l + 2 * pad, b - t + 2 * pad), 0)
    ImageDraw.Draw(mask).text((pad - l, pad - t), text, font=font, fill=255)
    return mask


def grow(mask, px):
    # MaxFilter needs an odd size; apply in steps for big outlines.
    out = mask
    while px > 0:
        step = min(px, 6)
        out = out.filter(ImageFilter.MaxFilter(2 * step + 1))
        px -= step
    return out


def styled(text, font, fill_stops, outline, outline_px, pad):
    mask = text_mask(text, font, pad)
    fill = gradient(mask.size, fill_stops)
    ring = grow(mask, outline_px)
    out = Image.new('RGBA', mask.size, (0, 0, 0, 0))
    out.paste(Image.new('RGBA', mask.size, outline + (255,)), (0, 0), ring)
    # A thin highlight line along the top inner edge gives the letters some volume.
    out.paste(fill.convert('RGBA'), (0, 0), mask)
    return out


def make_logo(height=420):
    """MICROCAR on top, RACING on a slanted banner under it; the whole thing sheared for speed."""
    big = ImageFont.truetype(os.path.join(FONTS, 'impact.ttf'), height)
    small = ImageFont.truetype(os.path.join(FONTS, 'ariblk.ttf'), int(height * 0.36))
    outline = int(height * 0.075)
    pad = outline + 20

    micro = styled('MICROCAR', big,
                   [(0.0, (255, 250, 228)), (0.45, (255, 214, 64)), (1.0, (255, 120, 0))],
                   NAVY, outline, pad)

    racing = styled('RACING', small, [(0.0, (255, 255, 255)), (1.0, (230, 238, 255))],
                    NAVY, int(outline * 0.55), int(pad * 0.6))

    # Banner behind RACING: a red parallelogram with the same navy edge.
    bw = int(racing.width * 1.25)
    bh = int(racing.height * 0.92)
    slant = int(bh * 0.35)
    banner = Image.new('RGBA', (bw + slant, bh), (0, 0, 0, 0))
    d = ImageDraw.Draw(banner)
    edge = int(outline * 0.5)
    d.polygon([(slant, 0), (bw + slant, 0), (bw, bh), (0, bh)], fill=NAVY + (255,))
    inner = gradient((bw + slant, bh), [(0.0, (255, 70, 50)), (1.0, (200, 20, 30))]).convert('RGBA')
    m = Image.new('L', banner.size, 0)
    ImageDraw.Draw(m).polygon([(slant + edge, edge), (bw + slant - edge, edge), (bw - edge, bh - edge), (edge, bh - edge)], fill=255)
    banner.paste(inner, (0, 0), m)
    banner.alpha_composite(racing, ((banner.width - racing.width) // 2, (banner.height - racing.height) // 2 - int(bh * 0.02)))

    gap = -int(height * 0.08)
    w = max(micro.width, banner.width)
    logo = Image.new('RGBA', (w, micro.height + banner.height + gap), (0, 0, 0, 0))
    logo.alpha_composite(banner, ((w - banner.width) // 2 + int(height * 0.12), micro.height + gap))
    logo.alpha_composite(micro, ((w - micro.width) // 2, 0))

    # Italic shear for speed, then a soft drop shadow.
    k = 0.16
    sheared_w = logo.width + int(k * logo.height)
    logo = logo.transform((sheared_w, logo.height), Image.AFFINE, (1, k, -k * logo.height, 0, 1, 0), resample=Image.BICUBIC)
    shadow = Image.new('RGBA', logo.size, (0, 0, 0, 0))
    shadow.paste(Image.new('RGBA', logo.size, (0, 0, 0, 150)), (0, 0), logo.getchannel('A'))
    shadow = shadow.filter(ImageFilter.GaussianBlur(height * 0.04))
    margin = int(height * 0.12)
    out = Image.new('RGBA', (logo.width + margin * 2, logo.height + margin * 2), (0, 0, 0, 0))
    out.alpha_composite(shadow, (margin + int(height * 0.03), margin + int(height * 0.05)))
    out.alpha_composite(logo, (margin, margin))
    return out.crop(out.getbbox())


def grade(img):
    """A little more punch than the raw render: contrast, colour, a vignette."""
    img = ImageEnhance.Contrast(img).enhance(1.12)
    img = ImageEnhance.Color(img).enhance(1.18)
    w, h = img.size
    vignette = Image.radial_gradient('L').resize((w, h))  # 0 centre .. 255 edge
    vignette = vignette.point(lambda v: int(max(0, v - 90) * 0.55))
    dark = Image.new('RGB', (w, h), (0, 0, 0))
    return Image.composite(dark, img, vignette)


def cover(raw_name, size, logo, logo_width_share, logo_anchor):
    raw = Image.open(os.path.join(RAW, raw_name)).convert('RGB')
    img = raw.resize(size, Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
    img = grade(img).convert('RGBA')
    lw = int(size[0] * logo_width_share)
    lg = logo.resize((lw, int(logo.height * lw / logo.width)), Image.LANCZOS)
    ax, ay = logo_anchor
    x = int((size[0] - lg.width) * ax)
    y = int(size[1] * ay)
    img.alpha_composite(lg, (x, y))
    return img.convert('RGB')


def main():
    os.makedirs(OUT, exist_ok=True)
    logo = make_logo()
    logo.save(os.path.join(OUT, 'logo.png'))

    c169 = cover('wide3.png', (1920, 1080), logo, 0.50, (0.04, 0.035))
    c11 = cover('sq1.png', (1080, 1080), logo, 0.84, (0.5, 0.035))
    c57 = cover('tall4.png', (1080, 1512), logo, 0.88, (0.5, 0.04))

    c169.save(os.path.join(OUT, 'cover_16x9.jpg'), quality=92)
    c11.save(os.path.join(OUT, 'cover_1x1.jpg'), quality=92)
    c57.save(os.path.join(OUT, 'cover_5x7.jpg'), quality=92)
    c11.resize((640, 640), Image.LANCZOS).save(LOADING, quality=86)

    for name in ('cover_16x9.jpg', 'cover_1x1.jpg', 'cover_5x7.jpg', 'logo.png'):
        p = os.path.join(OUT, name)
        print('%-16s %s  %.0f KB' % (name, Image.open(p).size, os.path.getsize(p) / 1024))
    print('loading.jpg     ', Image.open(LOADING).size, '%.0f KB' % (os.path.getsize(LOADING) / 1024))


if __name__ == '__main__':
    main()
