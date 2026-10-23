"""Rebuild W2.2l registered frames from the unchanged uploaded ZIP.

Requires Pillow/numpy. Registration landmarks are measured in SOURCE pixels
from top-left; scaling is uniform, never independent X/Y. See the manifest.
"""
import hashlib
import io
import json
from pathlib import Path
import uuid
import zipfile

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "SourceAssets~/W22l/sprites_archer.zip"
OUTPUT = ROOT / "Assets/Resources/CultivationSprites"
# Source sole midpoint, floor row, crown row and desired posed body height.
# Hit is deliberately crouched (~340px), not inflated to standing height.
# Bow/arrow/effect extents do NOT determine character scale.
REGISTRATION = [
    ("Archer_Normal.png", "Normal", 1, 300, 586, 210, 376),
    ("Archer_Normal (2).png", "Normal", 2, 780, 1029, 197, 376),
    ("Archer_Normal (3).png", "Normal", 3, 800, 1004, 188, 376),
    ("atk1.png", "Attack", 1, 238, 406, 2, 376),
    ("atk2.png", "Attack", 2, 347, 588, 222, 366),
    ("atk3.png", "Attack", 3, 251, 394, 8, 376),
    ("Archer_Hit.png", "Hit", 1, 230, 586, 248, 338),
    ("Archer_Hit (2).png", "Hit", 2, 672, 995, 179, 340),
    ("Archer_Hit (3).png", "Hit", 3, 660, 1000, 211, 340),
]


def meta(path):
    metadata = Path(str(path) + ".meta")
    if metadata.exists():
        return
    metadata.write_text("""fileFormatVersion: 2
guid: GUID
TextureImporter:
  serializedVersion: 13
  externalObjects: {}
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  maxTextureSize: 2048
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  spriteMode: 1
  spritePixelsToUnits: 100
  spritePivot: {x: 0.33333333, y: 0.01666667}
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureCompression: 0
    overridden: 0
""".replace("GUID", uuid.uuid4().hex))


def main():
    records = []
    with zipfile.ZipFile(SOURCE) as archive:
        for filename, pose, number, fx, fy, crown, height in REGISTRATION:
            raw = archive.read("anim/" + filename)
            im = Image.open(io.BytesIO(raw)).convert("RGBA")
            a = np.array(im)
            # Only discard distant, nearly invisible alpha noise. Expand the
            # >10/255 support by 8px, retaining original antialias/glow inside.
            ys, xs = np.where(a[:, :, 3] > 10)
            box = (max(0, int(xs.min()) - 8), max(0, int(ys.min()) - 8),
                   min(im.width, int(xs.max()) + 9), min(im.height, int(ys.max()) + 9))
            scale = height / (fy - crown)
            crop = im.crop(box)
            w, h = round(crop.width * scale), round(crop.height * scale)
            crop = crop.resize((w, h), Image.Resampling.LANCZOS)
            # Allow <= .5 pixel registration error from integer raster placement.
            x = round(300 - (fx - box[0]) * scale)
            y = round(590 - (fy - box[1]) * scale)
            assert x >= 0 and y >= 0 and x+w <= 900 and y+h <= 600, filename
            canvas = Image.new("RGBA", (900, 600))
            canvas.alpha_composite(crop, (x, y))
            # atk1/atk3 contain checkerboard RGB under ZERO alpha. Do not key
            # gray pixels from the body: keep alpha and clear invisible RGB.
            pixels = np.array(canvas)
            pixels[pixels[:, :, 3] == 0] = 0
            dest = OUTPUT / f"Archer_{pose}_{number}.png"
            Image.fromarray(pixels).save(dest)
            meta(dest)
            records.append(dict(source=filename, source_size=im.size,
                source_sha256=hashlib.sha256(raw).hexdigest(), crop=box,
                source_foot=[fx, fy], crown=crown, uniform_scale=scale,
                posed_height=height, output=str(dest.relative_to(ROOT)),
                canvas=[900,600], foot_bottom_left=[300,10],
                output_sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
    (SOURCE.parent / "archer-registration.json").write_text(json.dumps(records, indent=2) + "\n")
    print("Registered", len(records), "frames; original archive preserved.")


if __name__ == "__main__":
    main()
