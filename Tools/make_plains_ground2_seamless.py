#!/usr/bin/env python3
"""Rebuild ONLY the approved runtime Plains/Ground2.png (Pillow + NumPy).

Usage, from any directory:
  python3 Tools/make_plains_ground2_seamless.py --write --proof-dir /tmp/ground2-proof
  python3 Tools/make_plains_ground2_seamless.py --check

Input is the pre-repair RUNTIME image in Git commit 115ff428..., not the
unprocessed ArtSource PNG. Both inputs are hash-guarded. A shallow checkout can
supply --input /path/to/the/pre-repair/runtime/Ground2.png instead; this must have
exactly the approved input hash. The script never fetches or installs anything.

Technique (no blur, mirrored/copied scenery or horizontal resampling):
* Register the two alpha horizons at their half-opacity crossing. Shift only the
  outer 96 columns per side, by at most 8 vertical pixels. A quintic envelope
  returns the displacement to zero at the inner border and the bottom row.
* In linear-light RGB, extend the common boundary correction into each strip by
  solving discrete Laplace's equation, with zero correction at the inner border
  and reflected vertical boundaries. High-frequency corrections decay rapidly;
  this ADDS a smooth color field instead of low-pass filtering image detail.
* Match the already registered alpha profiles, preserving the feathered outline.
  Boundary RGBA columns are equal. All 1980 middle columns remain byte-identical.

The input is 2172x724 RGBA; the output has the same canvas, mode and scale. No
Unity code, metadata, other artwork or ArtSource file is written. There are no
random choices. Quantitative/visual checks do not replace native Unity rendering.
Validated with Python 3, NumPy 2.3.5 and Pillow 12.3.0. --check verifies decoded
pixels as PNG encoding details can vary between Pillow/zlib versions.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFont, __version__ as PILLOW_VERSION

ROOT = Path(__file__).resolve().parents[1]
ASSET = Path("Assets/Resources/Environment/Plains/Ground2.png")
SOURCE = Path("Assets/StreamingAssets/ArtSource/Plains/Ground2.png")
BASE_REVISION = "115ff428b9a2380fb4a71e08f3ccba07ca517dc7"
INPUT_SHA256 = "339620ce72d385f971420e4439cbdef930383f92f77ece791777fbd9a85af153"
SOURCE_SHA256 = "0d2478ffca635e45f1776d98e06c062be911836592d63529afad658fa60c78e0"
BAND = 96


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def baseline(input_path):
    if input_path is None:
        result = subprocess.run(
            ["git", "show", f"{BASE_REVISION}:{ASSET.as_posix()}"], cwd=ROOT,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
        require(result.returncode == 0,
                "Baseline commit is unavailable. Supply --input with the original runtime PNG. "
                "No file has been written.")
        data = result.stdout
    else:
        data = input_path.read_bytes()
    require(sha256(data) == INPUT_SHA256,
            "Unrecognized baseline PNG: refusing to replace newer/different artwork.")
    image = Image.open(io.BytesIO(data))
    require(image.mode == "RGBA" and image.size == (2172, 724), "Unexpected input canvas/mode.")
    return np.array(image), image.info.get("dpi")


def smootherstep(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0)


def to_linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)


def to_srgb(rgb):
    return np.where(rgb <= .0031308, 12.92 * rgb,
                    1.055 * np.maximum(rgb, 0.0) ** (1.0 / 2.4) - .055)


def harmonic_extension(boundary, width):
    """Laplace correction: given x=0 boundary, zero at x=width-1, Neumann in y.

    Mirror y to diagonalize the discrete vertical Laplacian with a real FFT.
    For each frequency, the horizontal solution is sinh(lambda*(d-x))/sinh(lambda*d),
    where cosh(lambda)=2-cos(omega). Its exponential form avoids overflow; the
    zero-frequency solution is linear. This is not a blur of the source pixels.
    """
    height = len(boundary)
    spectrum = np.fft.rfft(np.concatenate((boundary, boundary[::-1])), axis=0)
    k = np.arange(height + 1)
    lam = np.arccosh(2.0 - np.cos(np.pi * k / height))
    x = np.arange(width)[None, :]
    d = width - 1
    ratio = np.empty((height + 1, width))
    ratio[0] = 1.0 - x / d
    frequency = lam[1:, None]
    ratio[1:] = (np.exp(-frequency * x) * -np.expm1(-2.0 * frequency * (d - x)) /
                 -np.expm1(-2.0 * frequency * d))
    return np.fft.irfft(spectrum[:, None, :] * ratio[:, :, None],
                        n=2 * height, axis=0)[:height]


def register_strip(rgba, columns, shift, target_horizon):
    """Vertical-only pull resampling in premultiplied linear RGB + alpha."""
    height = rgba.shape[0]
    weight = smootherstep(1.0 - np.arange(BAND) / (BAND - 1))
    # Keep full registration around the feathered horizon and keep y=bottom fixed.
    vertical = 1.0 - smootherstep(
        (np.arange(height) - (target_horizon + 32)) / (height - 1 - target_horizon - 32))
    sample_y = np.arange(height)[:, None] - shift * vertical[:, None] * weight[None, :]
    low = np.floor(sample_y).astype(int)
    fraction = (sample_y - low)[:, :, None]
    low = np.clip(low, 0, height - 1)
    high = np.minimum(low + 1, height - 1)
    original = rgba[:, columns]
    premultiplied = np.dstack((to_linear(original[:, :, :3]) * original[:, :, 3:4],
                               original[:, :, 3]))
    warped = (premultiplied[low, np.arange(BAND)[None, :]] * (1.0 - fraction) +
              premultiplied[high, np.arange(BAND)[None, :]] * fraction)
    alpha = warped[:, :, 3:4]
    rgb = np.divide(warped[:, :, :3], alpha, out=np.zeros_like(warped[:, :, :3]), where=alpha > 0)
    return rgb, alpha, columns, weight


def reconstruct(raw):
    height, width, _ = raw.shape
    horizons = [int(np.flatnonzero(raw[:, edge, 3] >= 128)[0]) for edge in (0, width - 1)]
    target = (sum(horizons) + 1) // 2
    shifts = [target - horizon for horizon in horizons]
    require(max(abs(shift) for shift in shifts) <= 8, "Horizon mismatch exceeds the approved small warp.")
    rgba = raw.astype(np.float64) / 255.0
    strips = [register_strip(rgba, np.arange(BAND), shifts[0], target),
              register_strip(rgba, np.arange(width - 1, width - BAND - 1, -1), shifts[1], target)]
    left_rgb, left_alpha, _, _ = strips[0]
    right_rgb, right_alpha, _, _ = strips[1]
    common_rgb = (left_rgb[:, 0] + right_rgb[:, 0]) / 2.0
    common_alpha = (left_alpha[:, 0] + right_alpha[:, 0]) / 2.0
    output = raw.copy()
    clipped_visible_components = 0
    for rgb, alpha, columns, weight in strips:
        corrected = rgb + harmonic_extension(common_rgb - rgb[:, 0], BAND)
        clipped_visible_components += int(np.count_nonzero(((corrected < 0) | (corrected > 1)) & (alpha > 0)))
        colors = to_srgb(np.clip(corrected, 0, 1))
        opacity = np.clip(alpha + (common_alpha - alpha[:, 0])[:, None, :] * weight[None, :, None], 0, 1)
        output[:, columns] = np.rint(np.dstack((colors, opacity)) * 255).clip(0, 255).astype(np.uint8)
    common = np.rint(np.column_stack((to_srgb(np.clip(common_rgb, 0, 1)), common_alpha)) * 255).clip(0, 255).astype(np.uint8)
    output[:, 0] = common
    output[:, -1] = common
    # Fixed Dirichlet borders also preserve otherwise invisible source RGB.
    output[:, BAND - 1] = raw[:, BAND - 1]
    output[:, -BAND] = raw[:, -BAND]
    return output, {"band_columns_per_side": BAND, "horizon_half_alpha_before": horizons,
                    "horizon_shift_pixels_left_right": shifts, "horizon_half_alpha_target": target,
                    "clipped_visible_rgb_components": clipped_visible_components}


def measure(raw):
    values = raw.astype(np.float64)
    premultiplied = values[:, :, :3] * values[:, :, 3:4] / 255.0
    rgb = np.abs(values[:, 0, :3] - values[:, -1, :3])
    alpha = np.abs(values[:, 0, 3] - values[:, -1, 3])
    column_jump = np.abs(premultiplied[:, 1:] - premultiplied[:, :-1]).mean(axis=(0, 2))
    edge_alpha = []
    for x in (0, raw.shape[1] - 1):
        edge_alpha.append({str(level): int(np.flatnonzero(raw[:, x, 3] >= level)[0])
                           for level in (1, 128, 255)})
    return {"edge_rgb_mae": float(rgb.mean()), "edge_rgb_mae_per_channel": rgb.mean(axis=0).tolist(),
            "edge_rgb_max_difference": int(rgb.max()), "edge_alpha_mae": float(alpha.mean()),
            "edge_alpha_max_difference": int(alpha.max()), "edge_alpha_mismatched_rows": int(np.count_nonzero(alpha)),
            "edge_premultiplied_rgb_mae": float(np.abs(premultiplied[:, 0] - premultiplied[:, -1]).mean()),
            "max_interior_column_jump": float(column_jump.max()),
            "neighbor_column_jump_left_right": [float(column_jump[0]), float(column_jump[-1])],
            "alpha_crossing_rows_left_right": edge_alpha}


def validate(before, after, processing):
    require(after.shape == before.shape and after.dtype == np.uint8, "Canvas/dtype changed.")
    require(np.array_equal(after[:, 0], after[:, -1]), "RGBA edges do not match.")
    require(np.array_equal(before[:, BAND:-BAND], after[:, BAND:-BAND]), "Protected middle artwork changed.")
    require(np.array_equal(before[:, BAND - 1], after[:, BAND - 1]) and
            np.array_equal(before[:, -BAND], after[:, -BAND]), "Inner strip boundaries changed.")
    require(processing["clipped_visible_rgb_components"] == 0, "Visible colors would clip; reject instead of hiding detail.")
    old, new = measure(before), measure(after)
    require(new["max_interior_column_jump"] <= old["max_interior_column_jump"],
            "Processing introduced a stronger interior vertical step than the original artwork.")
    require(new["edge_alpha_mismatched_rows"] == 0 and new["edge_rgb_max_difference"] == 0,
            "Seam statistics did not converge.")
    return old, new


def encode(raw, dpi):
    stream = io.BytesIO()
    options = {"dpi": dpi} if dpi else {}
    Image.fromarray(raw).save(stream, format="PNG", compress_level=9, **options)
    return stream.getvalue()


def proof_directory(folder):
    folder = folder.resolve()
    # Proofs belong outside Unity's import tree. Never risk an ArtSource output.
    for directory in (ROOT / "Assets", ROOT / "Packages", ROOT / "ProjectSettings"):
        require(folder != directory.resolve() and directory.resolve() not in folder.parents,
                "Choose a proof directory outside Assets/Packages/ProjectSettings.")
    return folder


def proof_images(folder, before, after, report):
    folder = proof_directory(folder)
    folder.mkdir(parents=True, exist_ok=True)
    height, width, _ = before.shape
    panels = []
    for name, raw in (("before", before), ("after", after)):
        repeated = Image.new("RGBA", (width * 3, height))
        image = Image.fromarray(raw)
        for index in range(3):
            repeated.paste(image, (index * width, 0))
        repeated.save(folder / (name + "-3tiles.png"))
        underlay = Image.new("RGBA", repeated.size, (96, 112, 128, 255))
        underlay.alpha_composite(repeated)
        panels.append(underlay.convert("RGB"))
    comparison = Image.new("RGB", (width * 3, 2 * height + 96), (32, 40, 48))
    draw = ImageDraw.Draw(comparison)
    font = ImageFont.load_default(size=28)
    for index, (name, panel) in enumerate(zip(("BEFORE - 3 tiles", "AFTER - 3 tiles"), panels)):
        y = index * (height + 48)
        draw.text((12, y + 8), name, fill="white", font=font)
        for join in (width, width * 2):
            draw.text((join - 32, y + 8), "JOIN", fill=(255, 220, 100), font=font)
        comparison.paste(panel, (0, y + 48))
    comparison.save(folder / "comparison-3tiles.png")
    previews = []
    for panel in panels:
        panel = panel.copy()
        panel.thumbnail((1800, 500))
        previews.append(panel)
    preview_height = previews[0].height
    preview = Image.new("RGB", (1800, 2 * (preview_height + 32)), (32, 40, 48))
    draw = ImageDraw.Draw(preview)
    for index, panel in enumerate(previews):
        y = index * (preview_height + 32)
        draw.text((10, y + 5), ("BEFORE - 3 tiles", "AFTER - 3 tiles")[index],
                  fill="white", font=ImageFont.load_default(size=18))
        for join in (600, 1200):
            draw.text((join - 22, y + 5), "JOIN", fill=(255, 220, 100), font=ImageFont.load_default(size=18))
        preview.paste(panel, (0, y + 32))
    preview.save(folder / "comparison-overview.png")
    extent, top = 224, 420
    detail = Image.new("RGB", (extent * 4, height - top + 32), (32, 40, 48))
    draw = ImageDraw.Draw(detail)
    for index, panel in enumerate(panels):
        draw.text((index * extent * 2 + 8, 8), ("BEFORE", "AFTER")[index] + " - native pixels",
                  fill="white", font=ImageFont.load_default(size=16))
        detail.paste(panel.crop((width - extent, top, width + extent, height)), (index * extent * 2, 32))
    detail.save(folder / "comparison-detail.png")
    (folder / "metrics.json").write_text(json.dumps(report, indent=2) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--write", action="store_true", help="Write only the approved runtime PNG, after all quality gates.")
    mode.add_argument("--check", action="store_true", help="Rebuild in memory and compare current runtime pixels; no asset writes.")
    parser.add_argument("--input", type=Path, help="Optional original runtime PNG (exact baseline hash required).")
    parser.add_argument("--proof-dir", type=Path, help="Write native-resolution three-tile comparisons and metrics here.")
    args = parser.parse_args()
    if args.proof_dir:
        args.proof_dir = proof_directory(args.proof_dir)
    source = ROOT / SOURCE
    require(source.is_file() and sha256(source.read_bytes()) == SOURCE_SHA256,
            "ArtSource is missing or changed. Refuse to process until its provenance is reviewed.")
    target = ROOT / ASSET
    require(not target.is_symlink() and target.resolve() != source.resolve(), "Unsafe runtime destination.")
    before, dpi = baseline(args.input)
    after, processing = reconstruct(before)
    old_metrics, new_metrics = validate(before, after, processing)
    if args.write:
        current_hash = sha256(target.read_bytes())
        current_pixels = np.array(Image.open(target).convert("RGBA"))
        require(current_hash == INPUT_SHA256 or np.array_equal(current_pixels, after),
                "Runtime differs from both the approved baseline and reconstruction; refusing overwrite.")
        target.write_bytes(encode(after, dpi))
    elif args.check:
        image = Image.open(target)
        require(image.mode == "RGBA" and image.size == (2172, 724), "Runtime canvas/mode changed.")
        require(np.array_equal(np.array(image), after), "Runtime pixels differ from the deterministic reconstruction.")
    require(sha256(source.read_bytes()) == SOURCE_SHA256, "ArtSource integrity check failed.")
    report = {"input_revision": BASE_REVISION, "input_runtime_sha256": INPUT_SHA256,
              "source_unchanged_sha256": SOURCE_SHA256, "dimensions": [after.shape[1], after.shape[0]],
              "processing": processing, "before": old_metrics, "after": new_metrics,
              "protected_middle_columns": after.shape[1] - 2 * BAND,
              "protected_middle_percent": (after.shape[1] - 2 * BAND) * 100.0 / after.shape[1],
              "changed_visible_pixels": int(np.count_nonzero((before != after).any(axis=2) & (before[:, :, 3] > 0))),
              "output_pixel_sha256": sha256(after.tobytes()),
              "numpy_version": np.__version__, "pillow_version": PILLOW_VERSION,
              "action": "wrote runtime" if args.write else "checked runtime" if args.check else "preview only"}
    if args.proof_dir:
        proof_images(args.proof_dir, before, after, report)
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
