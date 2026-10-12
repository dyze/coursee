# Regenerates src/coursee.App/Assets/coursee.ico (needs Python 3 and Pillow):
#   python build/make_icon.py src/coursee.App/Assets/coursee.ico preview.png
"""Draws coursee.ico: a white magnifier on a rounded blue square. Each size is drawn
8x larger and scaled down; small sizes get thicker strokes so they stay readable."""
import sys
from PIL import Image, ImageDraw

def frame(size):
    s = size * 8
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pad = s * 0.04
    d.rounded_rectangle([pad, pad, s - pad, s - pad], radius=s * 0.22, fill=(37, 99, 235, 255))
    stroke = s * (0.13 if size <= 20 else 0.10)
    cx, cy, r = s * 0.44, s * 0.44, s * 0.21
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(255, 255, 255, 255), width=round(stroke))
    off = r * 0.66
    d.line([cx + off, cy + off, s * 0.76, s * 0.76], fill=(255, 255, 255, 255), width=round(stroke * 1.15))
    end = s * 0.76
    rr = stroke * 1.15 / 2
    d.ellipse([end - rr, end - rr, end + rr, end + rr], fill=(255, 255, 255, 255))
    return img.resize((size, size), Image.LANCZOS)

sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
frames = [frame(n) for n in sizes]
frames[-1].save(sys.argv[1], format="ICO", sizes=[(n, n) for n in sizes], append_images=frames[:-1])
frame(256).save(sys.argv[2])
