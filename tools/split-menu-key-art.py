"""Splits Assets/_Project/Art/UI/MenuKeyArt.png into a panel-less backdrop and a transparent panel cut-out.

The menu key art has the title panel painted into it. To shrink the panel without shrinking the scenery,
the panel is lifted out as its own sprite and the area it covered is filled in from its surroundings.
Re-run after replacing MenuKeyArt.png:  python tools/split-menu-key-art.py
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

ART = Path(__file__).resolve().parent.parent / "Assets/_Project/Art/UI"
# Pixel box of the painted panel (including its offset shadow) in the 1672 x 941 art.
BOX = (50, 40, 646, 892)
# Output is upscaled so ultra-wide screens (21:9), which stretch the art ~1.5x, stay crisp. The scene only uses
# fractions of the art size, so the scale factor is free to change.
SCALE = 2


def upscale(img: Image.Image) -> Image.Image:
    big = img.resize((img.width * SCALE, img.height * SCALE), Image.LANCZOS)
    rgb = big.convert("RGB").filter(ImageFilter.UnsharpMask(radius=1.4, percent=70, threshold=2))
    if big.mode == "RGBA":
        rgb.putalpha(big.getchannel("A"))
    return rgb


def panel_mask(rgb: np.ndarray) -> np.ndarray:
    x0, y0, x1, y1 = BOX
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    cream = (r > 225) & (g > 215) & (b > 190) & (r - b < 60)
    navy = (r < 70) & (g < 80) & (b < 120) & (b >= r)
    ink = cream | navy
    box = np.zeros(ink.shape, bool)
    box[y0:y1, x0:x1] = True
    ink &= box
    # Close pinholes, keep the component through the panel centre, then fill the title/button interiors.
    ink = ndimage.binary_closing(ink, iterations=3)
    labels, _ = ndimage.label(ink)
    centre = labels[(y0 + y1) // 2, (x0 + x1) // 2]
    mask = ndimage.binary_fill_holes(labels == centre)
    return ndimage.binary_opening(mask, iterations=2)


def fill_backdrop(rgb: np.ndarray, mask: np.ndarray) -> np.ndarray:
    """Replaces the panel with a per-row blend of the scenery on either side of it."""
    x0, y0, x1, y1 = BOX
    out = rgb.astype(float).copy()
    left_x, right_x = max(x0 - 6, 0), min(x1 + 10, rgb.shape[1] - 1)
    rows = np.arange(y0 - 4, y1 + 4)
    # Median (not mean) keeps the flat cartoon bands crisp instead of smearing them.
    left = ndimage.median_filter(np.median(out[:, max(left_x - 6, 0):left_x + 1], axis=1), size=(9, 1))
    right = ndimage.median_filter(np.median(out[:, right_x:right_x + 8], axis=1), size=(9, 1))
    cols = np.arange(x0 - 4, x1 + 4)
    t = ((cols - cols[0]) / (cols[-1] - cols[0]))[None, :, None]
    patch = left[rows][:, None, :] * (1 - t) + right[rows][:, None, :] * t
    region = np.zeros(mask.shape, bool)
    region[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1] = True
    full = np.zeros_like(out)
    full[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1] = patch
    out[region] = full[region]
    return np.clip(out, 0, 255).astype(np.uint8)


def main() -> None:
    src = Image.open(ART / "MenuKeyArt.png").convert("RGB")
    rgb = np.array(src)
    mask = panel_mask(rgb)

    upscale(Image.fromarray(fill_backdrop(rgb, mask))).save(ART / "MenuKeyArtBackdrop.png", optimize=True)

    x0, y0, x1, y1 = BOX
    alpha = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))
    panel = src.copy()
    panel.putalpha(alpha)
    upscale(panel.crop(BOX)).save(ART / "MenuKeyArtPanel.png", optimize=True)
    print("panel box", BOX, "size", (x1 - x0, y1 - y0))


if __name__ == "__main__":
    main()
